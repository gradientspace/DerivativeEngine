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

		[NodeFunction(Hidden=true)]
		public static object? PrintValue(object? Value, string Format = "{0}")
		{
			object showValue = (Value != null) ? Value : "null";
			GlobalGraphOutput.AppendLine(String.Format(Format, showValue), EGraphOutputType.User);
			return Value;
		}

	}


    [GraphNodeUIName("Print")]
    public class PrintStringNode : StandardNode
    {
        public override string? GetNodeNamespace() { return "Core.IO"; }
        public override string GetDefaultNodeName() { return "Print"; }

        public static string InputName { get { return "String"; } }

        public PrintStringNode()
        {
            AddInput(InputName, new StandardStringNodeInput("(string)") );
        }

        public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            string StringValue = DataIn.FindStringValueOrDefault(InputName, "");
            GlobalGraphOutput.AppendLine(StringValue, EGraphOutputType.User);
        }
    }


    [GraphNodeNamespace("Core.IO")]
    [GraphNodeUIName("Print")]
    public class PrintWithFormatNode : VariableObjectsInputNode
    {
        public static string FormatName { get { return "Format"; } }

        public override string GetDefaultNodeName() { return "Print"; }
        protected override string ElementBaseName { get { return "Value"; } }

        protected override void BuildStandardInputsOutputs()
        {
            StandardStringNodeInput formatInput = new StandardStringNodeInput("{0}");
            formatInput.Flags |= ENodeInputFlags.IsNodeConstant;
            AddInput(FormatName, formatInput);
        }

        public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            object[] formatValues = ConstructObjectArray(in DataIn);
            string FormatString = DataIn.FindStringValueOrDefault(FormatName, "");
            string result = String.Format(FormatString, formatValues);

            GlobalGraphOutput.AppendLine(result, EGraphOutputType.User);
        }
    }


}
