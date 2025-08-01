using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Gradientspace.NodeGraph
{
	public static class GraphTraversalUtils
	{
		/**
		 * Find all the sequence output connections at a node, ie the paths the graph evaluation
		 * might conceivably take at that node
		 **/
		public static void FindAllOutgoingSequenceConnections(
			BaseGraph Graph, 
			NodeHandle nodeHandle, 
			ref List<IConnectionInfo> Connections, 
			NodeBase? HaveNode = null)
		{
			if (HaveNode == null)
				HaveNode = Graph.FindNodeFromHandle(nodeHandle);
			if (HaveNode == null)
				return;
			Graph.FindConnectionsFrom(nodeHandle.Identifier, "", ref Connections, EConnectionType.Sequence);
			// some nodes might have more outputs
			// (is it always the case that additional outputs mean no default output? if so we can skip this in those cases...)
			foreach (INodeOutputInfo output in HaveNode.EnumerateOutputs())
			{
				Graph.FindConnectionsFrom(nodeHandle.Identifier, output.OutputName, ref Connections, EConnectionType.Sequence);
			}
		}
		/**
		 * Find all the sequence output connections at a node, ie the paths the graph evaluation
		 * might conceivably take at that node
		 **/
		public static List<IConnectionInfo> FindAllOutgoingSequenceConnections(
			BaseGraph Graph,
			NodeHandle nodeHandle,
			NodeBase? HaveNode = null)
		{
			List<IConnectionInfo> SeqConnections = new List<IConnectionInfo>();
			FindAllOutgoingSequenceConnections(Graph, nodeHandle, ref SeqConnections, HaveNode);
			return SeqConnections;
		}

        public static List<IConnectionInfo> FindAllOutgoingSequenceConnections(
            BaseGraph Graph,
            NodeBase Node)
        {
            NodeHandle nodeHandle = new NodeHandle(Node.GraphIdentifier);
            List<IConnectionInfo> SeqConnections = new List<IConnectionInfo>();
            FindAllOutgoingSequenceConnections(Graph, nodeHandle, ref SeqConnections, Node);
            return SeqConnections;
        }


        /**
		 * Traverse all sequence paths in the entire graph. This traversal will split at
		 * any node that has multiple sequence outputs. Will touch all nodes with a sequence
		 * path connected to SequenceStartNode or FunctionDefinitionNode node types
         *
         * (but currently not disconnected paths or non-sequence/pure nodes!)
		 */
        public enum ScopeType
        {
            GraphStart = 0,
            GraphFunction = 1,
            Local = 2
        }

        public static void TraverseAllSequencePaths(
            BaseGraph Graph,
            Action<NodeBase> NodeFunction,
            Action<NodeBase, IConnectionInfo, ScopeType, bool>? ScopeFunction = null)
        {
            // process all function definitions
            foreach (FunctionDefinitionNode funcDefNode in Graph.EnumerateNodesOfType<FunctionDefinitionNode>()) 
            {
                List<IConnectionInfo> FuncSeqConnections = FindAllOutgoingSequenceConnections(Graph, funcDefNode);
                Debug.Assert(FuncSeqConnections.Count == 1);
                ScopeFunction?.Invoke(funcDefNode, FuncSeqConnections[0], ScopeType.GraphFunction, true);
                traverse_paths(Graph, new(funcDefNode.GraphIdentifier), FuncSeqConnections[0], NodeFunction, ScopeFunction);
                ScopeFunction?.Invoke(funcDefNode, FuncSeqConnections[0], ScopeType.GraphFunction, false);
            }

            // should only ever be one...
            foreach (SequenceStartNode startNode in Graph.EnumerateNodesOfType<SequenceStartNode>()) 
            {
                List<IConnectionInfo> SeqConnections = FindAllOutgoingSequenceConnections(Graph, startNode);
                Debug.Assert(SeqConnections.Count == 1);
                ScopeFunction?.Invoke(startNode, SeqConnections[0], ScopeType.GraphStart, true);
                traverse_paths(Graph, new(startNode.GraphIdentifier), SeqConnections[0], NodeFunction, ScopeFunction);
                ScopeFunction?.Invoke(startNode, SeqConnections[0], ScopeType.GraphStart, false);
            }
        }


        private static void traverse_paths(BaseGraph Graph, NodeHandle nodeHandle, IConnectionInfo outgoingConnection,
            Action<NodeBase> NodeFunction, Action<NodeBase, IConnectionInfo, ScopeType, bool>? ScopeFunction)
        {
            List<IConnectionInfo> SeqConnections = new List<IConnectionInfo>();
            NodeHandle CurrentNodeHandle = nodeHandle;
            IConnectionInfo NextSequenceConnection = outgoingConnection;

            bool bDone = false;
            while (!bDone) 
            {
                NodeHandle NextNodeHandle = new(NextSequenceConnection.ToNodeIdentifier);
                NodeBase? NextNode = Graph.FindNodeFromHandle(NextNodeHandle);

                if (NextNode == null) {
                    bDone = true;
                    break;
                }

                NodeFunction(NextNode);

                SeqConnections.Clear();
                FindAllOutgoingSequenceConnections(Graph, NextNodeHandle, ref SeqConnections, NextNode);
                if (SeqConnections.Count == 1) 
                {
                    CurrentNodeHandle = NextNodeHandle;
                    NextSequenceConnection = SeqConnections[0];
                } else {
                    foreach (IConnectionInfo outgoingSeqConn in SeqConnections) 
                    {
                        ScopeFunction?.Invoke(NextNode, outgoingSeqConn, ScopeType.Local, true);
                        traverse_paths(Graph, NextNodeHandle, outgoingSeqConn, NodeFunction, ScopeFunction);
                        ScopeFunction?.Invoke(NextNode, outgoingSeqConn, ScopeType.Local, false);
                    }
                    bDone = true;
                }
            }
        }


    }
}
