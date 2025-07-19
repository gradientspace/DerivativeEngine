using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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



		/**
		 * Traverse all sequence paths in the entire graph. This traversal will split at
		 * any node that has multiple sequence outputs. Will touch all nodes with a sequence
		 * path connected to the startNode (but not disconnected paths or non-sequence/pure nodes!)
		 **/
		public static void TraverseAllSequencePaths(
			BaseGraph Graph,
			NodeHandle startNodeHandle,
			Action<NodeBase> NodeFunction,
			Action<NodeBase, IConnectionInfo>? PushScopeFunction = null,
			Action<NodeBase, IConnectionInfo>? PopScopeFunction = null)
		{
			NodeBase? StartNode = Graph.FindNodeFromHandle(startNodeHandle);
			if (StartNode == null) return;
			List<IConnectionInfo> InitialSeqConnections = FindAllOutgoingSequenceConnections(Graph, startNodeHandle, StartNode);
			foreach (IConnectionInfo outgoingSeqConn in InitialSeqConnections)
			{
				PushScopeFunction?.Invoke(StartNode, outgoingSeqConn);
				traverse_paths(Graph, startNodeHandle, outgoingSeqConn, NodeFunction, PushScopeFunction, PopScopeFunction);
				PopScopeFunction?.Invoke(StartNode, outgoingSeqConn);
			}
		}
		private static void traverse_paths(BaseGraph Graph, NodeHandle nodeHandle, IConnectionInfo outgoingConnection, 
			Action<NodeBase> NodeFunction, Action<NodeBase, IConnectionInfo>? PushScopeFunction, Action<NodeBase, IConnectionInfo>? PopScopeFunction)
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
						PushScopeFunction?.Invoke(NextNode, outgoingSeqConn);
						traverse_paths(Graph, NextNodeHandle, outgoingSeqConn, NodeFunction, PushScopeFunction, PopScopeFunction);
						PopScopeFunction?.Invoke(NextNode, outgoingSeqConn);
					}
					bDone = true;
				}
			}
		}


	}
}
