using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
    public class ExecutionGraph : BaseGraph
    {
        public NodeHandle StartNodeHandle { get; private set; }
        public SequenceStartNode StartNode { get; private set; }

        public ExecutionGraph() : base()
        {
            StartNodeHandle = this.AddNodeOfType<SequenceStartNode>(); ;
            StartNode = (SequenceStartNode)this.FindNodeFromHandle(StartNodeHandle)!;
        }


        




    }

}
