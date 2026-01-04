// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph.Util;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace Gradientspace.NodeGraph
{
    public class ExecutionGraphSerializer
    {

        public struct VersionHeader
        {
            public const string GraphType_Execution = "ExecutionGraph";

            public string Version { get; set; }
            public string GraphType { get; set; }
            public Dictionary<string, string> MetaTags { get; set; } = new();

            public VersionHeader(string version, string graphType)
            {
                Version = version;
                GraphType = graphType;
            }

            public void AddOrUpdateTags(IEnumerable<(string,string)> tags) {
                foreach( var tag in tags )
                    MetaTags[tag.Item1] = tag.Item2;
            }
            public void AddOrUpdateTags(Dictionary<string, string> tags) {
                foreach (var tag in tags)
                    MetaTags[tag.Key] = tag.Value;
            }

            public static VersionHeader Current { get { return Version_1p0; } }

            public static readonly VersionHeader Version_1p0 = new("1.0", GraphType_Execution);
        }


        public struct AssemblyDependency
        {
            public string AssemblyName { get; set; }
            public string QualifiedName { get; set; }
            public string DLLPath { get; set; }
        }

        public struct GraphNodeCustomData
        {
            public string Identifier { get; set; }
            public object Value { get; set; }
        }


        public struct GraphNode
        {
            public int Identifier { get; set; } = -1;
            public string NodeName { get; set; } = "UnknownNode";
            public string NodeClassType { get; set; } = "";
            public string NodeClassVariant { get; set; } = "";
            public string Location { get; set; } = "";
            public string Version { get; set; } = "1.0";

            public List<SerializationUtil.InputConstant> InputConstants { get; set; } = new List<SerializationUtil.InputConstant>();

            public List<GraphNodeCustomData> CustomData { get; set; } = new List<GraphNodeCustomData>();

            public GraphNode() { }
        }

        public struct Connection
        {
            public int FromNode { get; set; }
            public string OutputName { get; set; }
            public int ToNode { get; set; }
            public string InputName { get; set; }

            public Connection(IConnectionInfo info) {
                FromNode = info.FromNodeIdentifier; OutputName = info.FromNodeOutputName;
                ToNode = info.ToNodeIdentifier; InputName = info.ToNodeInputName;
            }
        }

        public class SerializedGraph
        {
            public VersionHeader Header { get; set; } = VersionHeader.Current;

            public List<AssemblyDependency> Assemblies { get; set; } = new List<AssemblyDependency>();
            public List<GraphNode> Nodes { get; set; } = new List<GraphNode>();
            public List<Connection> DataConnections { get; set; } = new List<Connection>();
            public List<Connection> SequenceConnections { get; set; } = new List<Connection>();

            public SerializedGraph()
            {
            }
        }


        public struct SaveGraphOptions
        {
            public INodeGraphLayoutProvider? LayoutProvider = null;

            public Func<NodeBase, bool>? IncludeNodeFunc = null;

            public Dictionary<string, string>? AdditionalTags = null;

            public SaveGraphOptions() { }
        }


        public static void Save(
            ExecutionGraph graph, 
            Stream utf8Stream,
            SaveGraphOptions options)
        {
            SerializedGraph Serialized = new SerializedGraph();

            Serialized.Header.AddOrUpdateTags(graph.EnumerateMetaTags());
            if (options.AdditionalTags != null)
                Serialized.Header.AddOrUpdateTags(options.AdditionalTags);

            HashSet<Assembly> ReferencedAssemblies = new();
            Action<Assembly> CollectAssembly = (Assembly assembly) => {
                ReferencedAssemblies.Add(assembly);
            };

            HashSet<int> SerializedNodeIDs = new();
            foreach (INodeInfo nodeInfo in graph.EnumerateNodes())
            {
                NodeHandle handle = new NodeHandle(nodeInfo.Identifier);
                if (graph.FindNodeInfoFromHandle(handle, out var InternalInfo) == false)
                    continue;

                // skip filtered nodes
                if (options.IncludeNodeFunc != null && options.IncludeNodeFunc(InternalInfo.Node) == false)
                    continue;

                NodeBase node = InternalInfo.Node;
                NodeType nodeType = InternalInfo.nodeType;

                node.CollectDependencies(CollectAssembly);

                GraphNode sni = new();
                sni.Identifier = InternalInfo.Identifier;
                sni.NodeName = node.GetNodeName();
                sni.NodeClassType = nodeType.ClassType.FullName!;
                sni.NodeClassVariant = nodeType.Variant;
                sni.Version = nodeType.Version.ToString();

                if (options.LayoutProvider != null)
                    sni.Location = options.LayoutProvider.GetLocationStringForNode(InternalInfo.Identifier);

                node.CollectCustomDataItems(out NodeCustomData? CustomData);
                if (CustomData != null ) {
                    foreach (var item in CustomData.DataItems)
                        sni.CustomData.Add(new GraphNodeCustomData() { Identifier = item.Item1, Value = item.Item2 });
                }

                SerializationUtil.SaveInputConstants(node, out var SavedInputConstants);
                if (SavedInputConstants != null)
                    sni.InputConstants.AddRange(SavedInputConstants);

                Serialized.Nodes.Add(sni);
                SerializedNodeIDs.Add(node.GraphIdentifier);
            }

            foreach (IConnectionInfo connectionInfo in graph.EnumerateConnections(EConnectionType.Data))
            {
                if (SerializedNodeIDs.Contains(connectionInfo.FromNodeIdentifier) == false ||
                     SerializedNodeIDs.Contains(connectionInfo.ToNodeIdentifier) == false)
                    continue;

                Serialized.DataConnections.Add(new Connection(connectionInfo));
            }
            foreach (IConnectionInfo connectionInfo in graph.EnumerateConnections(EConnectionType.Sequence))
            {
                if (SerializedNodeIDs.Contains(connectionInfo.FromNodeIdentifier) == false ||
                     SerializedNodeIDs.Contains(connectionInfo.ToNodeIdentifier) == false)
                    continue;

                Serialized.SequenceConnections.Add(new Connection(connectionInfo));
            }


            string ExecutablePath = AppDomain.CurrentDomain.BaseDirectory;
            foreach (Assembly assembly in ReferencedAssemblies) {
                if (assembly.Location.Length == 0)
                    continue;       // skip assemblies that don't have a DLL file - eg dynamically-generated assemblies

                string AssemblyPath = assembly.Location;
                if (AssemblyPath.StartsWith(ExecutablePath, StringComparison.OrdinalIgnoreCase))
                    AssemblyPath = Path.GetRelativePath(ExecutablePath, AssemblyPath);

                // if assembly is in known-assembly folder, then can even strip off relative path...

                Serialized.Assemblies.Add(new AssemblyDependency() {
                    AssemblyName = assembly.GetName().Name!,
                    QualifiedName = assembly.FullName!,
                    DLLPath = AssemblyPath
                });
            }

            JsonSerializerOptions jsonOptions = new JsonSerializerOptions();
            jsonOptions.WriteIndented = true;

            // adding this to avoid trying to serialize properties like Vector3d.Normalized
            jsonOptions.IgnoreReadOnlyProperties = true;

            JsonSerializer.Serialize<SerializedGraph>(utf8Stream, Serialized, jsonOptions);
        }


        // helper used to sort nodes by connectivity, so that connections can be restored in-order
        internal class NodeConnections
        {
            public int NodeIdentifier = -1;
            public INode? Node = null;
            public List<Connection> ConnectionsOut = new List<Connection>();
            public bool HasConnectionTo(int identifier)
            {
                foreach (Connection c in ConnectionsOut)
                    if (c.ToNode == identifier)
                        return true;
                return false;
            }
			public override string ToString()
			{
                return (Node == null) ? "null" : $"{NodeIdentifier}:{Node.GetType().ToString()}";
            }
		}


        public struct RestoreGraphOptions
        {
            public bool TryAutoRepairRenamedConnections = true;

            public INodeGraphLayoutProvider? LayoutProvider = null;

            public Func<NodeType, int, string, bool>? IncludeNodeFunc = null;

            public Dictionary<int, int>? NodeIDMapIn = null;

            public Dictionary<int, int>? NodeIDMapOut = null;

            public Dictionary<string, string>? AllRestoredTags = null;

            public RestoreGraphOptions() { }
        }

        public static bool Restore(Stream utf8Stream, ExecutionGraph appendToGraph)
        {
            return Restore(utf8Stream, appendToGraph, new RestoreGraphOptions());
        }
        public static bool Restore(
            Stream utf8Stream, 
            ExecutionGraph appendToGraph, 
            RestoreGraphOptions options )
        {
            Dictionary<int, int> nodeIdentifierMap = 
                (options.NodeIDMapOut != null) ? options.NodeIDMapOut : new Dictionary<int, int>();

            SerializedGraph? Serialized = JsonSerializer.Deserialize<SerializedGraph>(utf8Stream);
            if (Serialized == null)
                return false;

            if (options.AllRestoredTags != null) {
                foreach (var kvp in Serialized.Header.MetaTags ?? [] )
                    options.AllRestoredTags.Add(kvp.Key, kvp.Value);
            }

            // try to load assemblies referenced by graph file
            bool bNewAssembliesLoaded = false;
            foreach (AssemblyDependency assemblyDep in Serialized.Assemblies)
            {
                var checkResult = NodeLibraryUtils.CheckIfNodeLibraryAlreadyLoaded(assemblyDep.DLLPath, out string? loadedPath);
                if (checkResult == NodeLibraryUtils.ELibraryLoadCheckResult.AlreadyLoaded_ExactPath)
                    continue;       // library is already loaded and we can skip it
                if (checkResult == NodeLibraryUtils.ELibraryLoadCheckResult.AlreadyLoaded_DifferentPath) {
                    GlobalGraphOutput.AppendLog($"[Serializer] Graph Loader requested Assembly {assemblyDep.DLLPath} but a NodeLibrary with the same DLL name has already been loaded from {loadedPath!}. ignoring.");
                    continue;
                }
                Debug.Assert(checkResult == NodeLibraryUtils.ELibraryLoadCheckResult.NotLoaded);    // in case more options added later

                AssemblyUtils.EAssemblyLoadResult LoadResult
                    = AssemblyUtils.TryFindLoadAssembly(assemblyDep.QualifiedName, assemblyDep.DLLPath, out Assembly? LoadedAssembly);
                if (LoadResult == AssemblyUtils.EAssemblyLoadResult.LoadedSuccessfully) {
                    if (LoadedAssembly != null) {
                        GlobalGraphOutput.AppendLog($"[Serializer] Loaded referenced Assembly {LoadedAssembly.GetName().Name!} from {LoadedAssembly.Location} ");
                        NodeLibraryUtils.NotifyNodeLibraryLoadedExternally(LoadedAssembly);
                    }
                    bNewAssembliesLoaded = true;
                }
            }
            if ( bNewAssembliesLoaded ) {
                // if we loaded new assemblies, we need to refresh the node library
                DefaultNodeLibrary.ForceFullRebuild();
            }

            bool bNodeErrors = false;
            bool bConnectionErrors = false;

            int MaxIdentifier = -1;
            foreach (GraphNode node in Serialized.Nodes)
            {
                NodeVersion UseVersion = NodeVersion.Parse(node.Version);
                NodeType? FoundNodeType = DefaultNodeLibrary.Instance.FindNodeType(node.NodeClassType, node.NodeClassVariant, UseVersion);

                // if node type could not be found, graph is broken. We will swap in a 
                // MissingNodeErrorNode type node, which still won't work, but allows the node
                // to remain in the graph, so it can be handled at the UI level
                bool bNodeTypeMissing = false;
                if (FoundNodeType == null) {
                    FoundNodeType = DefaultNodeLibrary.Instance.FindNodeType(typeof(MissingNodeErrorNode));
                    bNodeTypeMissing = true;
                    bNodeErrors = true;
                }

                // if we still have no node type, abort this node
                if (FoundNodeType == null) {
                    nodeIdentifierMap.Add(node.Identifier, -1);
                    bNodeErrors = true;
                    continue;
                }

                if (options.IncludeNodeFunc != null && options.IncludeNodeFunc(FoundNodeType, node.Identifier, node.NodeName) == false)
                    continue;

                // ExecutionGraph comes with a start node already....will reuse for now...maybe should replace it?
                // (TODO HACK do something about this...)
                if (FoundNodeType.ClassType == typeof(SequenceStartNode) )
                {
                    nodeIdentifierMap.Add(node.Identifier, appendToGraph.StartNodeHandle.Identifier);
                    if (options.LayoutProvider != null)
                        options.LayoutProvider.SetNodeLocationFromString(appendToGraph.StartNodeHandle.Identifier, node.Location.ToString());
                    continue;
                }

                int UseSpecifiedNodeID = -1;
                if (options.NodeIDMapIn != null && options.NodeIDMapIn.TryGetValue(node.Identifier, out int SpecifiedID))
                    UseSpecifiedNodeID = SpecifiedID;

                NodeHandle newNodeHandle = appendToGraph.AddNodeOfType(FoundNodeType, UseSpecifiedNodeID);
                if (newNodeHandle == NodeHandle.Invalid)
                {
                    nodeIdentifierMap.Add(node.Identifier, -1);
                    bNodeErrors = true;
                    continue;
                }
                NodeBase? FoundNode = appendToGraph.FindNodeFromHandle(newNodeHandle);
                Debug.Assert(FoundNode != null);

                // configure the MissingNodeErrorNode if we swapped to it due to missing node type
                if ( bNodeTypeMissing) {
                    MissingNodeErrorNode ErrorNode = (FoundNode as MissingNodeErrorNode)!;
                    ErrorNode.NodeName = node.NodeName;
                    ErrorNode.NodeClassType = node.NodeClassType;
                    ErrorNode.NodeClassVariant = node.NodeClassVariant;
                }

                nodeIdentifierMap.Add(node.Identifier, newNodeHandle.Identifier);
                MaxIdentifier = Math.Max(MaxIdentifier, newNodeHandle.Identifier);

				if (options.LayoutProvider != null)
                    options.LayoutProvider.SetNodeLocationFromString(newNodeHandle.Identifier, node.Location.ToString());

                // restore custom data because for dynamic nodes this may modify the inputs, 
                // and they need to be correct for saved constants to be restored
                if (node.CustomData.Count > 0 ) {
                    NodeCustomData CustomData = new NodeCustomData();
                    foreach (GraphNodeCustomData data in node.CustomData)
                    {
                        if (data.Value is JsonElement)
                        {
                            JsonElement element = (JsonElement)data.Value;
                            if (element.ValueKind == JsonValueKind.String)
                                CustomData.AddStringItem(data.Identifier, element.ToString());
                            else
                                CustomData.AddItem(data.Identifier, element);
                        }

                    }
                    FoundNode.RestoreCustomDataItems(CustomData);
                }

                foreach (SerializationUtil.InputConstant constant in node.InputConstants) 
                {
                    foreach (NodeBase.NodeInputInfo inputInfo in FoundNode.Inputs) 
                    {
                        if (inputInfo.Name == constant.InputName) {
							SerializationUtil.RestoreInputConstant(FoundNode, inputInfo.AsInterface, constant);
                            break;
                        }
                    }
                }
			}

			// we need to rebuild the data connections in-order because of the existence of nodes
			// implementing INode_DynamicOutputs - these nodes may change their output pin types
			// based on their input pins. So before we touch those outputs we need to ensure all the
			// inputs have been wired up, then we can call INode_DynamicOutputs.UpdateDynamicOutputs()


            // make array of (node, list_of_outgoing_connections)
			NodeConnections[] SortedConnections = new NodeConnections[MaxIdentifier+1];
            foreach (INodeInfo nodeInfo in appendToGraph.EnumerateNodes())
                SortedConnections[nodeInfo.Identifier] = new NodeConnections() { NodeIdentifier = nodeInfo.Identifier, Node = nodeInfo.Node };
            foreach (Connection c in Serialized.DataConnections)
            {
                if (nodeIdentifierMap.TryGetValue(c.FromNode, out int FromNodeIdentifier) == false
                     || nodeIdentifierMap.TryGetValue(c.ToNode, out int ToNodeIdentifier) == false)
                {
                    bConnectionErrors = true;
                    continue;
                }

                if (FromNodeIdentifier < 0) {
                    bConnectionErrors = true;
                    continue;
                }

                Connection mappedC = new Connection() { 
                    FromNode = FromNodeIdentifier, OutputName = c.OutputName, 
                    ToNode = ToNodeIdentifier, InputName = c.InputName };
                SortedConnections[FromNodeIdentifier].ConnectionsOut.Add(mappedC);
            }

            // filter out nodes w/ no outgoing connections, unless they are dynamic-output,
            // in which case we need to process them in-order below
            List<NodeConnections> filtered = new List<NodeConnections>();
            foreach (NodeConnections c in SortedConnections) {
                if (c == null) continue;
                if ( (c.ConnectionsOut.Count > 0) || (c.Node is INode_DynamicOutputs) )
					filtered.Add(c);
            }
            SortedConnections = filtered.ToArray();

            // sort nodes by connectivity, so that a node should end up before any
            // of it's downstream connections
            SortedConnections = SortNodeConnections(SortedConnections);

            // now we can process nodes in-order
            foreach (NodeConnections nodeConnections in SortedConnections)
            {
				// at this point a dynamic node should have all its inputs wired up
				if (nodeConnections.Node is INode_DynamicOutputs)
					(nodeConnections.Node as INode_DynamicOutputs)?.UpdateDynamicOutputs(appendToGraph);

                // when we add connections on load, we ignore type checks, otherwise any errors
                // that might have been introduced will result in wires silently disappearing
                bool bDoTypeChecking = false;

				foreach (Connection c in nodeConnections.ConnectionsOut) {
                    bool bOK = appendToGraph.AddConnection(
                        new NodeHandle(c.FromNode), c.OutputName,
                        new NodeHandle(c.ToNode), c.InputName, bDoTypeChecking);

                    // if connection fails, try to autorepair it
                    // (todo: should defer this until after we try all connections as we
                    //  could be more flexible in heuristics at that point...
                    if (bOK == false && options.TryAutoRepairRenamedConnections) {
                        if (TryAutoRepairConnection(appendToGraph, c, out Connection newC)) {
                            // try again with repaired connection
                            bOK = appendToGraph.AddConnection(
                                new NodeHandle(newC.FromNode), newC.OutputName,
                                new NodeHandle(newC.ToNode), newC.InputName, bDoTypeChecking);
                        }
                    }

                    if (bOK == false) 
                    {
                        // If connection fails, it's either because nodes or pins are missing.
                        // In the pins case we can try to add 'error' pins/connections
                        appendToGraph.TryAddErrorConnection( new NodeHandle(c.FromNode), c.OutputName, new NodeHandle(c.ToNode), c.InputName);
                        bConnectionErrors = true;
                    }
                }
			}

            // restore sequence connections (currently do not need to care about ordering like we
            // did for data connections)
            foreach (Connection c in Serialized.SequenceConnections)
            {
                if (nodeIdentifierMap.TryGetValue(c.FromNode, out int FromNodeIdentifier) == false
                     || nodeIdentifierMap.TryGetValue(c.ToNode, out int ToNodeIdentifier) == false)
                    continue;

                bool bOK = appendToGraph.AddSequenceConnection(
                    new NodeHandle(FromNodeIdentifier), c.OutputName,
                    new NodeHandle(ToNodeIdentifier), c.InputName);
                if (!bOK)
                    bConnectionErrors = true;
            }

            return (bNodeErrors == false) && (bConnectionErrors == false);
        }


        protected static bool TryAutoRepairConnection(ExecutionGraph graph, Connection connection, out Connection newConnection)
        {
            newConnection = connection;

            NodeHandle FromNodeHandle = new NodeHandle(connection.FromNode);
            NodeHandle ToNodeHandle = new NodeHandle(connection.ToNode);
            NodeBase? FromNode = graph.FindNodeFromHandle(FromNodeHandle);
            NodeBase? ToNode = graph.FindNodeFromHandle(ToNodeHandle);
            if (FromNode == null || ToNode == null)
                return false;

            INodeOutput? Output = FromNode.FindOutput(connection.OutputName);
            INodeInput? Input = ToNode.FindInput(connection.InputName);
            if (Output == null && Input == null)
                return false;

            // if we could not find an output by name, try matching by type
            // only unique matches are allowed for now
            // (could relax this if we know other outputs/inputs are already used...)
            string? newOutputName = null;
            if (Output == null) {
                GraphDataType dataType = Input!.GetDataType();
                int Count = 0;
                foreach( var output in FromNode.EnumerateOutputs() ) {
                    if ( output.Output.GetDataType().IsSameType(in dataType) ) {
                        newOutputName = output.OutputName;
                        Count++;
                    }
                }
                if (Count != 1)
                    newOutputName = null;
            }

            // if we could not find an input by name, try matching by type
            // only unique matches are allowed for now
            string? newInputName = null;
            if (Input == null) {
                GraphDataType fromDataType = Output!.GetDataType();
                int Count = 0;
                foreach (var input in ToNode.EnumerateInputs()) {
                    GraphDataType toDataType = input.Input.GetDataType();
                    bool bTypeMatches = toDataType.IsSameType(in fromDataType);

                    // TODO: this is maybe too relaxed as it will allow (eg) a double to an int...
                    // (now that matchInfo is available we could be more strict here...)
                    if (!bTypeMatches)
                        bTypeMatches = TypeUtils.CanConnectFromTo(fromDataType, toDataType, out TypeUtils.TypeMatchInfo matchInfo);
                   
                    if (bTypeMatches) {
                        newInputName = input.InputName;
                        Count++;
                    }
                }
                if (Count != 1)
                    newInputName = null;
            }

            if (newInputName != null && newConnection.InputName != newInputName) {
                newConnection.InputName = newInputName;
                return true;
            }
            if (newOutputName != null && newConnection.OutputName != newOutputName) {
                newConnection.OutputName = newOutputName;
                return true;
            }
            return false;
        }


        public static bool IsSerializedGraphJSon(string json)
        {
            //bool bHasAssemblies = json.Contains("\"Assemblies\"");
            bool bHasNodes = json.Contains("\"Nodes\"");
            bool bHasDataConnections = json.Contains("\"DataConnections\"");
            bool bHasSequenceConnections = json.Contains("\"SequenceConnections\"");
            bool bHasNodeClassType = json.Contains("\"NodeClassType\"");
            return (bHasNodes && bHasDataConnections && bHasSequenceConnections && bHasNodeClassType);
        }



        // topological sort of an array of NodeConnections. This is Kahn's algorithm, implemented inefficiently...
        // The right thing to do would be to redesign NodeConnections...we need things 
        // structured in a graph to be able to do the topo-sort. We flattened the (known)
        // graph to create NodeConnections...
        internal static NodeConnections[] SortNodeConnections(NodeConnections[] sortedConnections)
        {
            // todo we already kinda have adjacencyList's in NodeConnections.ConnectionsOut...

            int n = sortedConnections.Length;
            var adjacencyList = new Dictionary<NodeConnections, List<NodeConnections>>();
            var inDegree = new Dictionary<NodeConnections, int>();

            // 1. Initialize dictionaries
            foreach (var node in sortedConnections) {
                adjacencyList[node] = new List<NodeConnections>();
                inDegree[node] = 0;
            }

            // 2. Build the graph by checking pairs (O(N^2))
            // We check if 'a' must come before 'b'
            for (int i = 0; i < n; i++) {
                for (int j = 0; j < n; j++) {
                    if (i == j) continue;

                    var a = sortedConnections[i];
                    var b = sortedConnections[j];

                    if (a.HasConnectionTo(b.NodeIdentifier)) {
                        // If a has a connection to b, it means a is a prerequisite for b
                        adjacencyList[a].Add(b);
                        inDegree[b]++;
                    }
                }
            }

            // 3. Standard Kahn's Logic
            var queue = new Queue<NodeConnections>();
            foreach (var node in sortedConnections) {
                if (inDegree[node] == 0)
                    queue.Enqueue(node);
            }

            var result = new List<NodeConnections>();

            while (queue.Count > 0) {
                var current = queue.Dequeue();
                result.Add(current);

                if (adjacencyList.ContainsKey(current)) {
                    foreach (var neighbor in adjacencyList[current]) {
                        inDegree[neighbor]--;
                        if (inDegree[neighbor] == 0)
                            queue.Enqueue(neighbor);
                    }
                }
            }

            // 4. Verification
            if (result.Count != n)
                throw new InvalidOperationException("Cycle detected or missing nodes in graph.");

            return result.ToArray();
        }
    }
}
