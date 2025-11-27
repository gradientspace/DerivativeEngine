// Copyright Gradientspace Corp. All Rights Reserved.
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
        public ENodeOutputFlags Flags { get; set; }

        public StandardNodeOutputBase(Type valueType)
        {
            ValueType = valueType;
        }

        public virtual GraphDataType GetDataType()
        {
            return new GraphDataType(ValueType);
        }

        public virtual ENodeOutputFlags GetOutputFlags()
        {
            return Flags;
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

        public virtual ENodeOutputFlags GetOutputFlags()
        {
            return ENodeOutputFlags.None;
        }
    }

}
