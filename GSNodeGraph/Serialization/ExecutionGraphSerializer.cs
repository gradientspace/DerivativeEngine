// Copyright Gradientspace Corp. All Rights Reserved.
using System.Diagnostics;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gradientspace.NodeGraph
{
    public class ExecutionGraphSerializer
    {

        public struct VersionHeader
        {
            public const string GraphType_Execution = "ExecutionGraph";

            public string Version { get; set; }
            public string GraphType { get; set; }

            public VersionHeader(string version, string graphType)
            {
                Version = version;
                GraphType = graphType;
            }

            public static VersionHeader Current { get { return Version_1p0; } }

            public static readonly VersionHeader Version_1p0 = new("1.0", GraphType_Execution);
        }


        public struct AssemblyDependency
        {
            string AssemblyName { get; set; }
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

            public SaveGraphOptions() { }
        }


        public static void Save(
            ExecutionGraph graph, 
            Stream utf8Stream,
            SaveGraphOptions options)
        {
            SerializedGraph Serialized = new SerializedGraph();

            HashSet<int> SerializedNodeIDs = new HashSet<int>();
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
            public INodeGraphLayoutProvider? LayoutProvider = null;

            public Func<NodeType, int, string, bool>? IncludeNodeFunc = null;

            public Dictionary<int, int>? NodeIDMapIn = null;

            public Dictionary<int, int>? NodeIDMapOut = null;

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
			// of it's downstream connections. 
            // TODO: does this actually always work? possibly incorrect definition of partial order
			Array.Sort(SortedConnections, (first, second) => {
                if (first.HasConnectionTo(second.NodeIdentifier))   return -1;
                if (second.HasConnectionTo(first.NodeIdentifier))   return 1;
                return 0;       // nodes are not connected, no ordering preference
			});

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
                    if (!bOK) 
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




        public static bool IsSerializedGraphJSon(string json)
        {
            //bool bHasAssemblies = json.Contains("\"Assemblies\"");
            bool bHasNodes = json.Contains("\"Nodes\"");
            bool bHasDataConnections = json.Contains("\"DataConnections\"");
            bool bHasSequenceConnections = json.Contains("\"SequenceConnections\"");
            bool bHasNodeClassType = json.Contains("\"NodeClassType\"");
            return (bHasNodes && bHasDataConnections && bHasSequenceConnections && bHasNodeClassType);
        }


    }
}
