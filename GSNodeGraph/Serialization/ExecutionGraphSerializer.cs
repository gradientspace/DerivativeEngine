using System.Diagnostics;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gradientspace.NodeGraph
{
    public class ExecutionGraphSerializer
    {
        public struct AssemblyDependency
        {
            string AssemblyName { get; set; }
        }

        public struct GraphInputConstant
        {
            public string InputName { get; set; }
            public string DataType { get; set; }
            public object Value { get; set; }
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

            public List<GraphInputConstant> InputConstants { get; set; } = new List<GraphInputConstant>();

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
            public List<AssemblyDependency> Assemblies { get; set; } = new List<AssemblyDependency>();
            public List<GraphNode> Nodes { get; set; } = new List<GraphNode>();
            public List<Connection> DataConnections { get; set; } = new List<Connection>();
            public List<Connection> SequenceConnections { get; set; } = new List<Connection>();

            public SerializedGraph()
            {
            }
        }


        public static void Save(ExecutionGraph graph, Stream utf8Stream, INodeGraphLayoutProvider? LayoutProvider = null)
        {
            SerializedGraph Serialized = new SerializedGraph();

            foreach (INodeInfo nodeInfo in graph.EnumerateNodes())
            {
                NodeHandle handle = new NodeHandle(nodeInfo.Identifier);
                if (graph.FindNodeInfoFromHandle(handle, out var InternalInfo) == false)
                    continue;

                NodeBase node = InternalInfo.Node;
                NodeType nodeType = InternalInfo.nodeType;

                GraphNode sni = new();
                sni.Identifier = InternalInfo.Identifier;
                sni.NodeName = node.GetNodeName();
                sni.NodeClassType = nodeType.ClassType.FullName!;
                sni.NodeClassVariant = nodeType.Variant;

                if (LayoutProvider != null)
                    sni.Location = LayoutProvider.GetLocationStringForNode(InternalInfo.Identifier);

                node.CollectCustomDataItems(out List<Tuple<string,object>> ? DataItemsList);
                if ( DataItemsList != null ) {
                    foreach (var item in DataItemsList)
                        sni.CustomData.Add(new GraphNodeCustomData() { Identifier = item.Item1, Value = item.Item2 });
                }

                foreach ( NodeBase.NodeInputInfo inputInfo in node.Inputs )
                {
                    INodeInput input = inputInfo.Input;
                    (object? constantValue, bool bIsDefined) = input.GetConstantValue();
                    if (bIsDefined == false || constantValue == null)
                        continue;

                    GraphDataType inputDataType = input.GetDataType();
                    Type inputType = inputDataType.DataType;

                    GraphInputConstant constant = new GraphInputConstant();
                    constant.InputName = inputInfo.Name;
                    constant.DataType = inputType.FullName ?? inputType.Name;

                    if (constantValue.GetType().IsSubclassOf(typeof(Type)))
                        constant.Value = (constantValue as Type)!.AssemblyQualifiedName!;
                    else if (inputType.IsEnum)
                        constant.Value = constantValue.ToString()!;
                    else
                        constant.Value = constantValue;

                    sni.InputConstants.Add(constant);
                }

                Serialized.Nodes.Add(sni);
            }

            foreach (IConnectionInfo connectionInfo in graph.EnumerateConnections(EConnectionType.Data))
            {
                Serialized.DataConnections.Add(new Connection(connectionInfo));
            }
            foreach (IConnectionInfo connectionInfo in graph.EnumerateConnections(EConnectionType.Sequence))
            {
                Serialized.SequenceConnections.Add(new Connection(connectionInfo));
            }

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;

            JsonSerializer.Serialize<SerializedGraph>(utf8Stream, Serialized, options);
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


        public static bool Restore(Stream utf8Stream, ExecutionGraph appendToGraph, INodeGraphLayoutProvider? LayoutProvider = null)
        {
            SerializedGraph? Serialized = JsonSerializer.Deserialize<SerializedGraph>(utf8Stream);
            if (Serialized == null)
                return false;

            bool bNodeErrors = false;
            bool bConnectionErrors = false;

            Dictionary<int, int> NodeIdentifierMap = new Dictionary<int, int>();
            int MaxIdentifier = -1;
            foreach (GraphNode node in Serialized.Nodes)
            {
                NodeType? FoundNodeType = DefaultNodeLibrary.Instance.FindNodeType(node.NodeClassType, node.NodeClassVariant);
                if (FoundNodeType == null)
                {
                    NodeIdentifierMap.Add(node.Identifier, -1);
                    bNodeErrors = true;
                    continue;
                }

                // ExecutionGraph comes with a start node already....will reuse for now...maybe should replace it?
                // (TODO HACK do something about this...)
                if (FoundNodeType.ClassType == typeof(SequenceStartNode) )
                {
                    NodeIdentifierMap.Add(node.Identifier, appendToGraph.StartNodeHandle.Identifier);
                    if (LayoutProvider != null)
                        LayoutProvider.SetNodeLocationFromString(appendToGraph.StartNodeHandle.Identifier, node.Location.ToString());
                    continue;
                }

                NodeHandle newNodeHandle = appendToGraph.AddNodeOfType(FoundNodeType);
                if (newNodeHandle == NodeHandle.Invalid)
                {
                    NodeIdentifierMap.Add(node.Identifier, -1);
                    bNodeErrors = true;
                    continue;
                }
                NodeBase? FoundNode = appendToGraph.FindNodeFromHandle(newNodeHandle);
                Debug.Assert(FoundNode != null);

                NodeIdentifierMap.Add(node.Identifier, newNodeHandle.Identifier);
                MaxIdentifier = Math.Max(MaxIdentifier, newNodeHandle.Identifier);

				if (LayoutProvider != null)
                    LayoutProvider.SetNodeLocationFromString(newNodeHandle.Identifier, node.Location.ToString());

                // restore custom data because for dynamic nodes this may modify the inputs, 
                // and they need to be correct for saved constants to be restored
                if (node.CustomData.Count > 0 ) {
                    List<Tuple<string, object>> Items = new List<Tuple<string, object>>();
                    foreach (GraphNodeCustomData data in node.CustomData)
                    {
                        if (data.Value is JsonElement)
                        {
                            JsonElement element = (JsonElement)data.Value;
                            if (element.ValueKind == JsonValueKind.String)
                                Items.Add(new(data.Identifier, element.ToString()));
                            else
                                Items.Add(new(data.Identifier, element));
                        }

                    }
                    FoundNode.RestoreCustomDataItems(Items);
                }

                foreach (GraphInputConstant constant in node.InputConstants) 
                {
                    foreach (NodeBase.NodeInputInfo inputInfo in FoundNode.Inputs) 
                    {
                        if (inputInfo.Name == constant.InputName) {
                            RestoreInputConstant(constant, inputInfo);
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
                if (NodeIdentifierMap.TryGetValue(c.FromNode, out int FromNodeIdentifier) == false
                     || NodeIdentifierMap.TryGetValue(c.ToNode, out int ToNodeIdentifier) == false)
                    continue;

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
                        bConnectionErrors = true;
                }
			}

            // restore sequence connections (currently do not need to care about ordering like we
            // did for data connections)
            foreach (Connection c in Serialized.SequenceConnections)
            {
                if (NodeIdentifierMap.TryGetValue(c.FromNode, out int FromNodeIdentifier) == false
                     || NodeIdentifierMap.TryGetValue(c.ToNode, out int ToNodeIdentifier) == false)
                    continue;

                bool bOK = appendToGraph.AddSequenceConnection(
                    new NodeHandle(FromNodeIdentifier), c.OutputName,
                    new NodeHandle(ToNodeIdentifier), c.InputName);
                if (!bOK)
                    bConnectionErrors = true;
            }

            return (bNodeErrors == false) && (bConnectionErrors == false);
        }




        internal static void RestoreInputConstant(in GraphInputConstant constant, in NodeBase.NodeInputInfo inputInfo)
        {
            INodeInput input = inputInfo.Input;
            GraphDataType dataType = input.GetDataType();
            Type inputType = dataType.DataType;

            string useTypeName = inputType.FullName ?? inputType.Name;
            (object? constantValue, bool bIsDefined) = input.GetConstantValue();
            if (bIsDefined != false && constantValue != null && useTypeName == constant.DataType)
            {
                string? stringConstant = constant.Value.ToString();
                Debug.Assert(stringConstant != null);

                if (useTypeName == "System.Type")
                {
                    Type? FoundType = TypeUtils.FindTypeInLoadedAssemblies(stringConstant);
                    if (FoundType != null)
                        input.SetConstantValue(FoundType);
                    return;
                }

                if ( useTypeName.StartsWith("System.") == false )
                {
                    // if it's not a system type, we need to find it. First check for enums...
                    Type? EnumType = TypeUtils.FindEnumTypeFromFullName(useTypeName);
                    if ( EnumType != null && TypeUtils.GetEnumInfo(EnumType, out var EnumInfo) )
                    {
                        object? EnumValue = EnumInfo.FindEnumValueFromString(stringConstant);
                        if (EnumValue != null)
                            input.SetConstantValue(EnumValue);

                        // not using by-integer...
                        //if (int.TryParse(stringConstant, out int EnumID))
                        //{
                        //    object? EnumValue = EnumInfo.FindEnumValueFromID(EnumID);
                        //    if (EnumValue != null)
                        //        input.SetConstantValue(EnumValue);
                        //}
                    }

                    return;
                }

                // for int types can we cast?
                // for float/double do we need to consider writing precision?
                if (useTypeName == typeof(float).FullName)
                {
                    if (float.TryParse(stringConstant, out float f))
                        input.SetConstantValue(f);
                }
                else if (useTypeName == typeof(double).FullName)
                {
                    if (double.TryParse(stringConstant, out double f))
                        input.SetConstantValue(f);
                }
                else if (useTypeName == typeof(int).FullName)
                {
                    if (int.TryParse(stringConstant, out int f))
                        input.SetConstantValue(f);
                }
                else if (useTypeName == typeof(short).FullName)
                {
                    if (short.TryParse(stringConstant, out short f))
                        input.SetConstantValue(f);
                }
                else if (useTypeName == typeof(long).FullName)
                {
                    if (long.TryParse(stringConstant, out long f))
                        input.SetConstantValue(f);
                }
                else if (useTypeName == typeof(bool).FullName)
                {
                    if (bool.TryParse(stringConstant, out bool bValue))
                        input.SetConstantValue(bValue);
                }
                else if (useTypeName == typeof(string).FullName)
                {
                    input.SetConstantValue(stringConstant);
                }

            }
        }


    }
}
