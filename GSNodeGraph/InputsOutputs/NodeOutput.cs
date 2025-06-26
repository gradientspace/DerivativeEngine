using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
    public class StandardNodeOutputBase : INodeOutput
    {
        Type ValueType { get; set; }

        public StandardNodeOutputBase(Type valueType)
        {
            ValueType = valueType;
        }

        public virtual GraphDataType GetDataType()
        {
            return new GraphDataType(ValueType);
        }
    }


    public class StandardNodeOutput<T> : StandardNodeOutputBase
    {
        public StandardNodeOutput() : base(typeof(T))
        {
        }
    }



    public struct ControlFlowOutputID
    {
        public string OutputPathName = "";

        public ControlFlowOutputID(string outputPathName)
        {
            OutputPathName = outputPathName;
        }
    }


    public class ControlFlowOutput : INodeOutput
    {
        public string OutputPathName;

        public ControlFlowOutput(string outputPathName)
        {
            OutputPathName = outputPathName;
        }   

        public virtual GraphDataType GetDataType()
        {
            return new GraphDataType(typeof(ControlFlowOutputID));
        }
    }

}
