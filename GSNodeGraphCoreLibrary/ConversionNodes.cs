// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Nodes
{
    [GraphNodeNamespace("Gradientspace.String")]
    [GraphNodeUIName("To String")]
    public class ToStringNode : StandardNode
    {
        public static string InputName { get { return "object"; } }
        public static string OutputName { get { return "string"; } }

        public override string GetDefaultNodeName() {
            return "To String";
        }

        public ToStringNode()
        {
            AddInput(InputName, new StandardNodeInput<object>());
            AddOutput(OutputName, new StandardNodeOutput<string>());
        }

        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(OutputName);
            if (OutputIndex == -1)
                throw new Exception("ToStringNode: output not found");

            // argh cannot distinguish null from not found
            object? Input = DataIn.FindItemValue(InputName);

            if (Input == null)
            {
                RequestedDataOut.SetItemValue(OutputIndex, "(null)");
            }
            else
            {
                string result = Input.ToString() ?? "(empty)";
                RequestedDataOut.SetItemValue(OutputIndex, result);
            }
        }
    }



 

}
