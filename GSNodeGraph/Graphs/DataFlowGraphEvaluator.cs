using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
    public class DataFlowGraphEvaluator
    {
        DataFlowGraph Graph;

        public DataFlowGraphEvaluator(DataFlowGraph graph)
        {
            Graph = graph;
        }


        public bool EnableDebugPrinting { get; set; } = false;



        public void EvaluateAllOutputs(
            Dictionary<INodeInfo, object?>? OutputValues)
        {
            List<INodeInfo> SinkNodes = new List<INodeInfo>();

            foreach (INodeInfo nodeInfo in Graph.EnumerateNodes())
            {
                Type nodeType = nodeInfo.Node!.GetType();

                if ( nodeType.IsSubclassOf( typeof(SinkNode) ) )
                {
                    SinkNodes.Add(nodeInfo);
                }
            }

            foreach (INodeInfo nodeInfo in SinkNodes)
            {
                object? Result = Graph.ComputeNodeOutputData(nodeInfo, "");

                if (EnableDebugPrinting)
                {
                    if (Result == null)
                        System.Console.WriteLine("Node " + nodeInfo.Node!.GetNodeName() + " : value is null");
                    else
                        System.Console.WriteLine("Node " + nodeInfo.Node!.GetNodeName() + " : value is type " + Result.GetType().Name + " with value " + Result.ToString());
                }
                    

                if ( Result != null && OutputValues != null)
                {
                    OutputValues.Add(nodeInfo, Result);
                }
            }
        }

    }
}
