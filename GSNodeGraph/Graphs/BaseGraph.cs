// Copyright Gradientspace Corp. All Rights Reserved.
using System.Diagnostics;

namespace Gradientspace.NodeGraph
{
    public class BaseGraph : INodeGraph
    {
        public struct Connection
        {
            public NodeHandle FromNode = NodeHandle.Invalid;
            public string FromOutput = "";
            public NodeHandle ToNode = NodeHandle.Invalid;
            public string ToInput = "";

            // connection state is dynamically updated...should it live here??
            public EConnectionState State = EConnectionState.OK;

			public Connection() { }

            public static bool operator ==(Connection A, Connection B)
            {
                return A.FromNode == B.FromNode && A.FromOutput == B.FromOutput && A.ToNode == B.ToNode && A.ToInput == B.ToInput;
            }
            public static bool operator !=(Connection A, Connection B) { return !(A == B); }

            readonly public override bool Equals(object? obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;
                return this == ((Connection)obj);
            }
            public override int GetHashCode()
            {
                return HashCode.Combine(FromNode.Identifier, FromOutput, ToNode.Identifier, ToInput);
            }
            public static readonly Connection Invalid = new();

            public static bool operator ==(Connection A, IConnectionInfo connectionInfo)
            {
                return (A.ToConnectionInfo(connectionInfo.ConnectionType) == connectionInfo);
            }
            public static bool operator !=(Connection A, IConnectionInfo connectionInfo) { return !(A == connectionInfo); }

            public static explicit operator IConnectionInfo(Connection connection)
            {
                return new IConnectionInfo()
                {
                    FromNodeIdentifier = connection.FromNode.Identifier,
                    FromNodeOutputName = connection.FromOutput,
                    ToNodeIdentifier = connection.ToNode.Identifier,
                    ToNodeInputName = connection.ToInput
                };
            }

            public IConnectionInfo ToConnectionInfo(EConnectionType AsType)
            {
                IConnectionInfo info = (IConnectionInfo)this;
                info.ConnectionType = AsType;
                return info;
            }

        }
        public List<Connection> Connections;
        public List<Connection> SequenceConnections;

        public struct NodeInfo
        {
            public NodeBase Node;
            public int Identifier;
            public NodeType nodeType;
        }
        public List<NodeInfo> Nodes;
        protected int NodeIdentifierCounter = 0;



        public BaseGraph()
        {
            Nodes = new List<NodeInfo>();
            NodeIdentifierCounter = 0;

            Connections = new List<Connection>();
            SequenceConnections = new List<Connection>();
        }


        public virtual NodeHandle AddNodeOfType<T>() where T : NodeBase, new()
        {
            T NewNode = new T();
            if (NewNode == null)
                throw new Exception("DataFlowGraph.AddNodeOfType: failed to create new node");

            NodeType nodeType = new NodeType(typeof(T));

            return internalAddNewNode(NewNode, nodeType);
        }
        public virtual NodeHandle AddNodeOfType(NodeType NodeType, int UseSpecifiedNodeID = -1)
        {
            Type NodeClassType = NodeType.ClassType;

            // todo support variant somehow? and pass to NodeBase?

            if (NodeClassType.IsSubclassOf(typeof(NodeBase)) == false)
                throw new Exception("DataFlowGraph.AddNodeOfType: provided NodeType is not a valid DataFlowGraph Node Type");

            object? NewNodeObj = null;
            if (NodeType.NodeArchetype != null && NodeType.NodeArchetype.GetType() == typeof(LibraryFunctionNode))
            {
                NewNodeObj = (NodeType.NodeArchetype as LibraryFunctionNode)!.MakeInstance();
            }
            else if (NodeType.NodeArchetype != null && NodeType.NodeArchetype.GetType() == typeof(LibraryFunctionSinkNode))
            {
                NewNodeObj = (NodeType.NodeArchetype as LibraryFunctionSinkNode)!.MakeInstance();
            }
            else
            {
                NewNodeObj = Activator.CreateInstance(NodeClassType);
            }

            if (NewNodeObj == null)
                throw new Exception("DataFlowGraph.AddNodeOfType: failed to create new node");

            return internalAddNewNode((NodeBase)NewNodeObj, NodeType, UseSpecifiedNodeID);
        }
        protected virtual NodeHandle internalAddNewNode(NodeBase newNode, NodeType ofNodeType, int UseSpecifiedNodeID = -1)
        {
            if ( UseSpecifiedNodeID >= 0 ) {
                foreach (NodeInfo info in Nodes)
                    if (info.Identifier == UseSpecifiedNodeID)
                        throw new Exception($"BaseGraph.internalAddNewNode() : UseSpecifiedNodeID {UseSpecifiedNodeID} is already in use");
            }

            NodeInfo Info = new();
            Info.Node = newNode;
            Info.Identifier = (UseSpecifiedNodeID >= 0) ? UseSpecifiedNodeID : NodeIdentifierCounter;
            NodeIdentifierCounter = Math.Max(NodeIdentifierCounter + 1, Info.Identifier + 1);
			Info.nodeType = ofNodeType;
            Nodes.Add(Info);

            newNode.SetOwningGraphInfo(Info.Identifier, Info.nodeType);

            return new NodeHandle(Info.Identifier);
        }

        // todo this should return a struct that includes (eg) info about whether there will be a conversion, etc
        public virtual bool CanConnectTypes(GraphDataType FromOutputDataType, GraphDataType ToInputDataType)
        {
            return TypeUtils.CanConnectFromTo(FromOutputDataType, ToInputDataType);
        }


		// todo return a more complex result structure here... (similar to CanConnectTypes...)
		public virtual bool AddConnection(
            NodeHandle FromNode, string FromOutput, 
            NodeHandle ToNode, string ToInput,
            bool bRequireMatchingDataTypes = true)
        {
            bool bFoundFrom = GetOutputTypeForNode(FromNode, FromOutput, out GraphDataType FromDataType);
            bool bFoundTo = GetInputTypeForNode(ToNode, ToInput, out GraphDataType ToDataType);
            if (bFoundFrom == false || bFoundTo == false)
                return false;

            if (bRequireMatchingDataTypes && CanConnectTypes(FromDataType, ToDataType) == false)
                return false;

            Connection NewConnection = new();
            NewConnection.FromNode = FromNode; NewConnection.FromOutput = FromOutput;
            NewConnection.ToNode = ToNode; NewConnection.ToInput = ToInput;
            if (Connections.Exists(x => x == NewConnection))
                return false;

            Connections.Add(NewConnection);
            return true;
        }


        // if AddConnection() fails because one of the nodes is missing the required input/output,
        // call this to try to dynamically spawn 'error' inputs/outputs, which then allows
        // for a valid (bad) connection to be added
        public virtual bool TryAddErrorConnection(
            NodeHandle FromNode, string FromOutput,
            NodeHandle ToNode, string ToInput)
        {
            bool bFoundFrom = GetOutputTypeForNode(FromNode, FromOutput, out GraphDataType FromDataType);
            bool bFoundTo = GetInputTypeForNode(ToNode, ToInput, out GraphDataType ToDataType);
            Debug.Assert(bFoundFrom == false || bFoundTo == false);

            if ( bFoundFrom == false ) {
                NodeBase? Found = FindNodeFromHandle(FromNode);
                if ( Found is MissingNodeErrorNode node ) 
                    node.AddOutput(FromOutput, new MissingNodeOutput());
            }
            if (bFoundTo == false) {
                NodeBase? Found = FindNodeFromHandle(ToNode);
                if ( Found is MissingNodeErrorNode node )
                    node.AddInput(ToInput, new MissingNodeInput());
            }

            Connection NewConnection = new();
            NewConnection.FromNode = FromNode; NewConnection.FromOutput = FromOutput;
            NewConnection.ToNode = ToNode; NewConnection.ToInput = ToInput;
            if (Connections.Exists(x => x == NewConnection))
                return false;

            Connections.Add(NewConnection);
            return true;

        }



        public virtual bool RemoveConnection(Connection connection)
        {
            int FoundIndex = Connections.FindIndex(x => x == connection);
            if (FoundIndex != -1)
            {
                Connections.RemoveAt(FoundIndex);
                return true;
            }
            return false;
        }



        // todo return a more complex result structure here...
        public virtual bool AddSequenceConnection(NodeHandle FromNode, string FromOutput, NodeHandle ToNode, string ToInput)
        {
            NodeBase? fromNode = FindNodeFromHandle(FromNode);
            NodeBase? toNode = FindNodeFromHandle(ToNode);
            if (fromNode == null || toNode == null)
                return false;

            Connection NewConnection = new();
            NewConnection.FromNode = FromNode; NewConnection.FromOutput = FromOutput;
            NewConnection.ToNode = ToNode; NewConnection.ToInput = ToInput;
            if (SequenceConnections.Exists(x => x == NewConnection))
                return false;

            SequenceConnections.Add(NewConnection);
            return true;
        }


        public virtual bool RemoveSequenceConnection(Connection connection)
        {
            int FoundIndex = SequenceConnections.FindIndex(x => x == connection);
            if (FoundIndex != -1)
            {
                SequenceConnections.RemoveAt(FoundIndex);
                return true;
            }
            return false;
        }


		/**
         * validate the state of all DataConnections in the current graph, to detect any type mismatches or missing inputs/outputs
         * that may have resulted from dynamic node changes
         */
		public virtual void ValidateDataConnections()
        {
            int N = Connections.Count;
            for (int i = 0; i < N; ++i )
            {
                Connection c = Connections[i];

				bool bFoundFrom = GetOutputTypeForNode(c.FromNode, c.FromOutput, out GraphDataType FromDataType);
				bool bFoundTo = GetInputTypeForNode(c.ToNode, c.ToInput, out GraphDataType ToDataType);
                if (bFoundFrom == false)
                    c.State = EConnectionState.OutputMissing;
                else if (bFoundTo == false)
                    c.State = EConnectionState.InputMissing;
                else if (CanConnectTypes(FromDataType, ToDataType) == false)
                    c.State = EConnectionState.TypeMismatch;
                else
                    c.State = EConnectionState.OK;

                Connections[i] = c;
			}
        }



        public NodeBase? FindNodeFromHandle(NodeHandle Handle)
        {
            foreach (NodeInfo ni in Nodes) {
                if (ni.Identifier == Handle.Identifier)
                    return ni.Node;
            }
            return null;
        }
        public bool FindNodeInfoFromHandle(NodeHandle Handle, out NodeInfo nodeInfo)
        {
            nodeInfo = new();
            int FoundIndex = Nodes.FindIndex(x => x.Identifier == Handle.Identifier);
            if (FoundIndex >= 0) {
                nodeInfo = Nodes[FoundIndex]; return true;
            }
            return false;            
        }

        public bool GetInputTypeForNode(NodeHandle NodeHandle, string InputName, out GraphDataType DataType)
        {
            DataType = new GraphDataType();
            NodeBase? Node = FindNodeFromHandle(NodeHandle);
            if (Node == null) return false;
            INodeInput? Input = Node.FindInput(InputName);
            if (Input == null) return false;
            DataType = Input.GetDataType();
            return true;
        }
        public bool GetOutputTypeForNode(NodeHandle NodeHandle, string OutputName, out GraphDataType DataType)
        {
            DataType = new GraphDataType();
            NodeBase? Node = FindNodeFromHandle(NodeHandle);
            if (Node == null) return false;
            INodeOutput? Output = Node.FindOutput(OutputName);
            if (Output == null) return false;
            DataType = Output.GetDataType();
            return true;
        }


        public (object?, bool) GetConstantValueForInput(NodeHandle NodeHandle, string InputName)
        {
            NodeBase? Node = FindNodeFromHandle(NodeHandle);
            if (Node == null) return (null, false);
            INodeInput? Input = Node.FindInput(InputName);
            if (Input == null) return (null, false);
            return Input.GetConstantValue();
        }



        public Connection FindDataConnectionForInput(NodeHandle ToNodeHandle, string InputName)
        {
            foreach (Connection connection in Connections) {
                if (connection.ToNode == ToNodeHandle && connection.ToInput == InputName)
                    return connection;
            }
            return Connection.Invalid;
        }

        public Connection FindSequenceConnectionForInput(NodeHandle ToNodeHandle, string InputName = "")
        {
            foreach (Connection connection in SequenceConnections) {
                if (connection.ToNode == ToNodeHandle && connection.ToInput == InputName)
                    return connection;
            }
            return Connection.Invalid;
        }
        public Connection FindSequenceConnectionForOutput(NodeHandle FromNodeHandle, string OutputName = "")
        {
            foreach (Connection connection in SequenceConnections) {
                if (connection.FromNode == FromNodeHandle && connection.FromOutput == OutputName)
                    return connection;
            }
            return Connection.Invalid;
        }


        protected bool find_connection(IConnectionInfo connectionInfo, out Connection foundConnection)
        {
            foundConnection = new();
			List<Connection> UseList = (connectionInfo.ConnectionType == EConnectionType.Data) ? Connections : SequenceConnections;
			foreach (Connection c in UseList) {
                if (c == connectionInfo) {
                    foundConnection = c;
                    return true;
                }
			}
            return false;
		}


        //
        // INodeGraph
        //
        public virtual IEnumerable<INodeInfo> EnumerateNodes()
        {
            foreach (NodeInfo info in Nodes)
            {
                yield return new INodeInfo() { Identifier = info.Identifier, Node = info.Node };
            }
        }

        public virtual IEnumerable<T> EnumerateNodesOfType<T>() where T : NodeBase
        {
            foreach (NodeInfo info in Nodes) {
                if (info.Node is T typedNode)
                    yield return typedNode;
            }
        }

        public virtual INodeInfo FindNodeFromIdentifier(int NodeIdentifier)
        {
            INode? Found = FindNodeFromHandle(new NodeHandle(NodeIdentifier));
            if ( Found != null )
                return new INodeInfo() { Identifier = NodeIdentifier, Node = Found };
            return new INodeInfo();
		}

        public virtual T? FindTypedNodeFromIdentifier<T>(int NodeIdentifier) where T : NodeBase
        {
            INode? Found = FindNodeFromHandle(new NodeHandle(NodeIdentifier));
            if ( Found == null ) return null;
            T? TypedNode = Found as T;
            return TypedNode;
        }

        public virtual IEnumerable<IConnectionInfo> EnumerateConnections(EConnectionType connectionType)
        {
            List<Connection> UseList = (connectionType == EConnectionType.Data) ? Connections : SequenceConnections;
            foreach (Connection connection in UseList)
                yield return connection.ToConnectionInfo(connectionType);
        }

        public virtual IConnectionInfo FindConnectionTo(int NodeIdentifier, string InputName, EConnectionType connectionType)
        {
            List<Connection> UseList = (connectionType == EConnectionType.Data) ? Connections : SequenceConnections;
            foreach (Connection connection in UseList) {
                if (connection.ToNode.Identifier == NodeIdentifier && connection.ToInput == InputName)
                    return connection.ToConnectionInfo(connectionType);
            }
            return IConnectionInfo.Invalid;
        }

        public virtual void FindAllNodeConnections(int NodeIdentifier, ref List<IConnectionInfo> nodeConnections, EConnectionType connectionType)
        {
            List<Connection> UseList = (connectionType == EConnectionType.Data) ? Connections : SequenceConnections;
            foreach (Connection c in UseList) {
                if (c.ToNode == NodeIdentifier || c.FromNode == NodeIdentifier)
                    nodeConnections.Add(c.ToConnectionInfo(connectionType));
            }
        }

        public virtual void FindConnectionsFrom(int NodeIdentifier, string OutputName, ref List<IConnectionInfo> nodeConnections, EConnectionType connectionType)
        {
            List<Connection> UseList = (connectionType == EConnectionType.Data) ? Connections : SequenceConnections;
            foreach (Connection c in UseList) {
                if (c.FromNode == NodeIdentifier && c.FromOutput == OutputName)
                    nodeConnections.Add(c.ToConnectionInfo(connectionType));
            }
        }

		public virtual EConnectionState GetConnectionState(IConnectionInfo connectionInfo)
        {
            if (find_connection(connectionInfo, out Connection c) == false)
                return EConnectionState.NotFound;
            return c.State;
        }

		public virtual bool GetNodeInputType(int NodeIdentifier, string InputName, out GraphDataType DataType)
        {
            DataType = new GraphDataType();
            foreach (NodeInfo info in Nodes) {
                if (info.Identifier == NodeIdentifier) {
                    INodeInput? FoundInput = info.Node.FindInput(InputName);
                    if (FoundInput != null) {
                        DataType = FoundInput.GetDataType();
                        return true;
                    }
                }
            }
            return false;
        }

        public virtual bool GetNodeOutputType(int NodeIdentifier, string OutputName, out GraphDataType DataType)
        {
            DataType = new GraphDataType();
            foreach (NodeInfo info in Nodes) {
                if (info.Identifier == NodeIdentifier) {
                    INodeOutput? FoundOutput = info.Node.FindOutput(OutputName);
                    if (FoundOutput != null) { 
                        DataType = FoundOutput.GetDataType();
                        return true;
                    }
                }
            }
            return false;
        }

        public virtual (object?, bool) GetNodeConstantValue(int NodeIdentifier, string InputName)
        {
            foreach (NodeInfo info in Nodes) {
                if (info.Identifier == NodeIdentifier) {
                    INodeInput? FoundInput = info.Node.FindInput(InputName);
                    if (FoundInput != null)
                        return FoundInput.GetConstantValue();
                }
            }
            return (null, false);
        }

        public virtual bool SetNodeConstantValue(int NodeIdentifier, string InputName, object NewValue)
        {
            const bool bPublishEvent = true;
            foreach (NodeInfo info in Nodes) 
            {
                if (info.Identifier == NodeIdentifier) 
                {
                    bool bSetValue = false;
                    INodeInput? FoundInput = info.Node.FindInput(InputName);
                    if (FoundInput != null)
                    {
                        GraphDataType InputDataType = FoundInput.GetDataType();
                        Type inputType = InputDataType.CSType;
                        Type NewValueType = NewValue.GetType();
                        if ( NewValueType == inputType ) 
                        {
                            FoundInput.SetConstantValue(NewValue);
                            info.Node.PublishNodeModifiedNotification();
                            bSetValue = true;
                        }
                        else if ( NewValueType.IsAssignableTo(inputType) ) 
                        {
                            FoundInput.SetConstantValue(NewValue);
                            bSetValue = true;
                        }
                        else if (NewValue is IConvertible) 
                        { 
                            object? CastType = Convert.ChangeType(NewValue, inputType);
                            if (CastType != null) {
                                FoundInput.SetConstantValue(CastType);
                                bSetValue = true;
                            }
                        }
                    }

                    if (bSetValue && bPublishEvent) {
                        // note: this is the nuclear option, that will force-rebuild the entire node,
                        // but we do not have a more precise way to do it yet...
                        info.Node.PublishNodeModifiedNotification();
                    }
                }
            }
            return false;
        }

        public virtual INodeInfo CreateNewNodeOfType(NodeType nodeType, int UseSpecifiedNodeIdentifier = -1)
        {
            NodeHandle NewNodeHandle = AddNodeOfType(nodeType, UseSpecifiedNodeIdentifier);
            NodeBase? Node = FindNodeFromHandle(NewNodeHandle);
            if (Node == null)
                throw new Exception("CreateNewNodeOfType: cannot find new Node!");

            return new INodeInfo() { Identifier = NewNodeHandle.Identifier, Node = Node };
        }


        public virtual bool RemoveNode(int NodeIdentifier)
        {
            List<IConnectionInfo> nodeConnections = new List<IConnectionInfo>();
            foreach (EConnectionType connectionType in Enum.GetValues<EConnectionType>())
            {
                FindAllNodeConnections(NodeIdentifier, ref nodeConnections, connectionType);
                foreach (IConnectionInfo connection in nodeConnections) {
                    bool bOK = RemoveConnection(connection);
                    if (!bOK)
                        throw new Exception("DataFlowGraph.RemoveNode: failed to remove a connection...");
                }
            }


            int FoundIndex = Nodes.FindIndex(n => n.Identifier == NodeIdentifier);
            if (FoundIndex >= 0)
            {
                Nodes.RemoveAt(FoundIndex);
                return true;
            }
            return false;
        }


        public virtual bool TryAddNewConnection(IConnectionInfo NewConnectionInfo)
        {
            NodeHandle FromNodeHandle = new NodeHandle(NewConnectionInfo.FromNodeIdentifier);
            NodeBase? FromNode = FindNodeFromHandle(FromNodeHandle);
            NodeHandle ToNodeHandle = new NodeHandle(NewConnectionInfo.ToNodeIdentifier);
            NodeBase? ToNode = FindNodeFromHandle(ToNodeHandle);
            if (FromNode == null || ToNode == null || FromNode == ToNode)
                return false;

            bool bOK = false;
            if (NewConnectionInfo.ConnectionType == EConnectionType.Data)
            {
                INodeOutput? FromOutput = FromNode.FindOutput(NewConnectionInfo.FromNodeOutputName);
                INodeInput? ToInput = ToNode.FindInput(NewConnectionInfo.ToNodeInputName);
                if (FromOutput == null || ToInput == null)
                    return false;

                // could allow replacement?
                Connection ExistingConnection = FindDataConnectionForInput(ToNodeHandle, NewConnectionInfo.ToNodeInputName);
                if (ExistingConnection != Connection.Invalid)
                    return false;
                bOK = AddConnection(FromNodeHandle, NewConnectionInfo.FromNodeOutputName, ToNodeHandle, NewConnectionInfo.ToNodeInputName);
            }
            else if (NewConnectionInfo.ConnectionType == EConnectionType.Sequence)
            {
                // what validation do we need to do here??
                Connection ExistingConnection = FindSequenceConnectionForOutput(FromNodeHandle);
                if (ExistingConnection != Connection.Invalid)
                    return false;

                bOK = AddSequenceConnection(FromNodeHandle, NewConnectionInfo.FromNodeOutputName, ToNodeHandle, NewConnectionInfo.ToNodeInputName);
            }
            else
                throw new NotImplementedException();

            return bOK;
        }


        public virtual bool RemoveConnection(IConnectionInfo ConnectionInfo)
        {
            List<Connection> UseList = (ConnectionInfo.ConnectionType == EConnectionType.Data) ? Connections : SequenceConnections;
            int FoundIndex = UseList.FindIndex(c => c == ConnectionInfo);
            if (FoundIndex >= 0)
            {
                UseList.RemoveAt(FoundIndex);
                return true;
            }
            return false;
        }

    }
}
