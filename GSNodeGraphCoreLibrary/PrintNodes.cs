// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public class PrintStringNode : StandardNode
    {
        public override string? GetNodeNamespace() { return "Core.IO"; }
        public override string GetDefaultNodeName() { return "PrintStr"; }

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



    [GraphNodeNamespace("Core.IO")]
    [GraphNodeUIName("PrintF")]
    public class PrintWithCustomFormatNode : PrintWithFormatNode
    {
        public override string GetDefaultNodeName() { return "PrintF"; }
        protected override void BuildStandardInputsOutputs()
        {
            base.BuildStandardInputsOutputs();
            (Inputs[0].Input as StandardStringNodeInput)!.Flags &= ~ENodeInputFlags.IsNodeConstant;
        }
    }




    [GraphNodeNamespace("Core.String")]
    public class StringViewNode : NodeBase
    {
        public override string GetDefaultNodeName() { return "ViewString"; }

        public const string StringInputName = "String";
        public const string StringOutputName = "String";

        public StringViewNode()
        {
            StandardNodeInputBase ImageInput = new StandardNodeInputBase(typeof(string));
            AddInput(StringInputName, ImageInput);
            ImageInput.Flags |= ENodeInputFlags.IsInOut;
            ImageInput.Flags |= ENodeInputFlags.HiddenLabel;

            StandardNodeOutputBase ImageOutput = new StandardNodeOutputBase(typeof(string));
            AddOutput(StringOutputName, ImageOutput);
            ImageOutput.Flags |= ENodeOutputFlags.HiddenLabel;
        }

        public delegate void StringViewUpdateEvent(string String);
        public event StringViewUpdateEvent? OnStringUpdate;

        public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(StringOutputName);
            if (OutputIndex == -1)
                throw new Exception($"{GetDefaultNodeName()}: output not found");

            object? foundData = DataIn.FindItemValue(StringInputName);
            if (foundData is string str) {
                OnStringUpdate?.Invoke(str);
                RequestedDataOut.SetItem(OutputIndex, StringOutputName, str);
            } else
                throw new Exception($"{GetDefaultNodeName()}: input string not found");
        }

    }




    /**
     * Object View Node - allows viewing any object as a string in the UI
     * This is DynamicOutput node - the output type will be updated to match
     * the type of object wired into the input.
     * (currently the input type does not change, it is always 'object')
     */
    [GraphNodeNamespace("Core.IO")]
    public class ObjectViewNode : NodeBase, INode_DynamicOutputs
    {
        public override string GetDefaultNodeName() { return "ViewObject"; }

        public const string ObjectInputName = "object";
        public const string ObjectOutputName = "object";

        StandardNodeInputBase ObjInput;
        StandardNodeOutputBase ObjOutput;

        public ObjectViewNode()
        {
            ObjInput = new StandardNodeInputBase(typeof(object));
            AddInput(ObjectInputName, ObjInput);
            ObjInput.Flags |= ENodeInputFlags.IsInOut;
            ObjInput.Flags |= ENodeInputFlags.HiddenLabel;

            ObjOutput = new StandardNodeOutputBase(typeof(object));
            AddOutput(ObjectOutputName, ObjOutput);
            ObjOutput.Flags |= ENodeOutputFlags.HiddenLabel;
        }

        public delegate void StringViewUpdateEvent(string String);
        public event StringViewUpdateEvent? OnStringUpdate;

        public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ObjectOutputName);
            if (OutputIndex == -1)
                throw new Exception($"{GetDefaultNodeName()}: output not found");

            object? foundData = DataIn.FindItemValue(ObjectInputName);
            string stringValue = "null";
            if ( foundData != null ) {
                stringValue = $"[{foundData.GetType().ToString()}]\n{foundData.ToString()}";
            }
            OnStringUpdate?.Invoke(stringValue);
            RequestedDataOut.SetItem(OutputIndex, ObjectOutputName, foundData!);
        }

        public void UpdateDynamicOutputs(INodeGraph graph)
        {
            IConnectionInfo connection = graph.FindConnectionTo(this.GraphIdentifier, ObjectInputName, EConnectionType.Data);
            if (connection.IsValid == false)
                return;

            INodeInfo fromNodeInfo = graph.FindNodeFromIdentifier(connection.FromNodeIdentifier);
            bool bFoundType = graph.GetNodeOutputType(connection.FromNodeIdentifier, connection.FromNodeOutputName, out GraphDataType incomingDataType);
            if (bFoundType == false)
                return;

            if (ObjOutput.GetDataType().IsSameType(incomingDataType) == false) {
                Outputs.Clear();
                ObjOutput = new StandardNodeOutputBase(incomingDataType.CSType);
                ObjOutput.Flags |= ENodeOutputFlags.HiddenLabel;
                AddOutput(ObjectOutputName, ObjOutput);

                // publish node change, this will rebuild it at the UI level
                PublishNodeModifiedNotification();
            }
        }
    }


}
