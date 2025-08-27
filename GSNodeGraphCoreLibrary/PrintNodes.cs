// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Nodes
{

	[NodeFunctionLibrary("Core.IO")]
	public static class GradientspaceIOFunctionLibrary
	{

		[NodeFunction]
		public static object? PrintValue(object? Value, string Format = "{0}")
		{
			object showValue = (Value != null) ? Value : "null";
			GlobalGraphOutput.AppendLine(String.Format(Format, showValue), EGraphOutputType.User);
			return Value;
		}

	}



    [GraphNodeNamespace("Core.IO")]
    [GraphNodeUIName("Print")]
    public class PrintWithFormatNode : VariableObjectsInputNode
    {
        public static string InputName { get { return "Format"; } }

        public override string GetDefaultNodeName() { return "Print"; }
        protected override string ElementBaseName { get { return "Value"; } }

        protected override void BuildStandardInputsOutputs()
        {
            StandardStringNodeInput newInput = new StandardStringNodeInput("{0}");
            AddInput(InputName, newInput);
        }

        public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            object[] formatValues = ConstructObjectArray(in DataIn);
            string FormatString = DataIn.FindStringValueOrDefault(InputName, "");
            string result = String.Format(FormatString, formatValues);

            GlobalGraphOutput.AppendLine(result, EGraphOutputType.User);
        }
    }


}
