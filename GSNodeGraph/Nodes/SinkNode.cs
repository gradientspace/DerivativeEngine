// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
    public abstract class SinkNode : StandardNode
    {
    }



    public abstract class PrintValueSinkNode<T> : SinkNode
    {
        public static string InputName { get { return "Value"; } }
        public static string FormatInputName { get { return "Format"; } }

        public PrintValueSinkNode()
        {
            AddInput(InputName, new StandardNodeInput<T>() );
            AddInput(FormatInputName, new StandardNodeInputBaseWithConstant(typeof(string), "{0}"));
        }

        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            object? FoundValue = DataIn.FindItemValueAsType(InputName, typeof(T));

            object? FormatStringObj = DataIn.FindItemValue(FormatInputName);
            string UseFormatString = (FormatStringObj is string) ? (string)FormatStringObj : "{0}";

            if ( FoundValue != null )
            {
                T Value = (T)FoundValue;
                GlobalGraphOutput.AppendLine(String.Format(UseFormatString, Value), EGraphOutputType.User);
			}
            else
                throw new Exception("PrintValueSinkNode: invalid type conversion");
        }
    }


    [GraphNodeNamespace("Gradientspace.Math")]
    [GraphNodeUIName("Print Float")]
    public class PrintFloatSinkNode : PrintValueSinkNode<float>
    {
        public override string GetDefaultNodeName()
        {
            return "Print Float";
        }
    }


    [GraphNodeNamespace("Gradientspace.Math")]
    [GraphNodeUIName("Print Int")]
    public class PrintIntSinkNode : PrintValueSinkNode<int>
    {
        public override string GetDefaultNodeName()
        {
            return "Print Int";
        }
    }


    [GraphNodeNamespace("Gradientspace.Math")]
    [GraphNodeUIName("Print String")]
    public class PrintStringSinkNode : PrintValueSinkNode<string>
    {
        public override string GetDefaultNodeName()
        {
            return "Print String";
        }
    }
}
