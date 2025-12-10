// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using System.Diagnostics;
using System.Reflection;

namespace Gradientspace.NodeGraph.Nodes
{
    [GraphNodeNamespace("Core.Constants")]
    [GraphNodeUIName("int")]
    public class Int32ConstantNode : GenericPODConstantNode<int>
    {
        public override string GetDefaultNodeName() { return "int"; }
    }

    [GraphNodeNamespace("Core.Constants")]
    [GraphNodeUIName("int64")]
    public class Int64ConstantNode : GenericPODConstantNode<long>
    {
        public override string GetDefaultNodeName() { return "int64"; }
    }

    [GraphNodeNamespace("Core.Constants")]
    [GraphNodeUIName("float")]
    public class FloatConstantNode : GenericPODConstantNode<float>
    {
        public override string GetDefaultNodeName() { return "float"; }
    }

    [GraphNodeNamespace("Core.Constants")]
    [GraphNodeUIName("double")]
    public class DoubleConstantNode : GenericPODConstantNode<double>
    {
        public override string GetDefaultNodeName() { return "double"; }
    }

    [GraphNodeNamespace("Core.Constants")]
    [GraphNodeUIName("bool")]
    public class BoolConstantNode : GenericPODConstantNode<bool>
    {
        public override string GetDefaultNodeName() { return "bool"; }
    }


    [GraphNodeNamespace("Core.Constants")]
    [GraphNodeUIName("string")]
    public class StringConstantNode : StandardNode, ICodeGen
    {
        public override string GetDefaultNodeName() { return "string"; }

        public StringConstantNode()
        {
            this.Flags = ENodeFlags.IsPure;
            AddOutput(ValueOutputName, new StandardNodeOutput<string>());
			StandardStringNodeInput input = new StandardStringNodeInput();
            input.Flags = ENodeInputFlags.IsNodeConstant | ENodeInputFlags.HiddenLabel;
			AddInput(ValueInputName, input);
        }

        public static string ValueOutputName { get { return "String"; } }
        public static string ValueInputName { get { return "String"; } }

        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ValueOutputName);
            if (OutputIndex == -1)
                throw new Exception("StringConstantNode: output not found");
            string Result = "";
            DataIn.FindItemValueStrict<string>(ValueInputName, ref Result);
            RequestedDataOut.SetItemValue(OutputIndex, Result);
        }

        public void GetCodeOutputNames(out string[]? OutputNames)
        {
            OutputNames = ["Str"];
        }
        public string GenerateCode(string[]? Arguments, string[]? UseOutputNames)
        {
            CodeGenUtils.CheckArgsAndOutputs(Arguments, 1, UseOutputNames, 1, "StringConstantNode");
            return $"string {UseOutputNames![0]} = {Arguments![0]};";
        }
    }



    // TextBlock node is a variant of StringConstantNode that provides a multiline text block input
    // This probably doesn't strictly need it's own node, but it makes things simpler...
    [GraphNodeNamespace("Core.Constants")]
    [GraphNodeUIName("textblock")]
    public class TextBlockConstantNode : StandardNode, ICodeGen
    {
        public override string GetDefaultNodeName() { return "textblock"; }

        public TextBlockConstantNode()
        {
            this.Flags = ENodeFlags.IsPure;
            AddOutput(ValueOutputName, new StandardNodeOutput<string>());
            TextBlockNodeInput input = new TextBlockNodeInput();
            input.UIWidthHint = 250;
            input.Flags = ENodeInputFlags.IsNodeConstant | ENodeInputFlags.HiddenLabel;
            AddInput(ValueInputName, input);
        }

        public static string ValueOutputName { get { return "String"; } }
        public static string ValueInputName { get { return "String"; } }

        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ValueOutputName);
            if (OutputIndex == -1)
                throw new Exception("TextBlockConstantNode: output not found");
            string Result = "";
            DataIn.FindItemValueStrict<string>(ValueInputName, ref Result);
            RequestedDataOut.SetItemValue(OutputIndex, Result);
        }

        public void GetCodeOutputNames(out string[]? OutputNames)
        {
            OutputNames = ["Str"];
        }
        public string GenerateCode(string[]? Arguments, string[]? UseOutputNames)
        {
            CodeGenUtils.CheckArgsAndOutputs(Arguments, 1, UseOutputNames, 1, "TextBlockConstantNode");
            return $"string {UseOutputNames![0]} = {Arguments![0]};";
        }
    }




    [ClassHierarchyNode]
    public class MakeStructNode<T> : StandardNode, ICodeGen
        where T : struct
    {
        public MakeStructNode()
        {
            // TODO: this should not be repeated for every node instance...somehow need to cache this

            Type structType = typeof(T);
            object? defaultStructInstance = Activator.CreateInstance(structType);
            Debug.Assert(defaultStructInstance != null);

            FieldInfo[] fields = structType.GetFields();
            for ( int j = 0; j < fields.Length; ++j )
            {
                if (fields[j].IsPublic == false) continue;
                Type FieldType = fields[j].FieldType;
                if (FieldType == typeof(int) || FieldType == typeof(float) || FieldType == typeof(double) )
                {
                    object? structDefaultValue = fields[j].GetValue(defaultStructInstance);
                    object initialValue = structDefaultValue ?? Convert.ChangeType(0, FieldType);
                    AddInput(fields[j].Name, new StandardNodeInputBaseWithConstant(FieldType, initialValue));
                }
            }

            AddOutput(ValueOutputName, new StandardNodeOutput<T>());
        }

        public static string ValueOutputName { get { return typeof(T).Name; } }

        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ValueOutputName);
            if (OutputIndex == -1)
                throw new Exception("MakeStructNode: output not found");

            Type structType = typeof(T);
            object? resultStruct = Activator.CreateInstance(structType);
            Debug.Assert(resultStruct != null);

            FieldInfo[] fields = structType.GetFields();
            for (int j = 0; j < fields.Length; ++j)
            {
                if (fields[j].IsPublic)
                {
                    object? fieldValue = DataIn.FindItemValue(fields[j].Name);
                    fields[j].SetValue(resultStruct, fieldValue);
                }
            }
           
            RequestedDataOut.SetItemValue(OutputIndex, resultStruct);
        }


        public void GetCodeOutputNames(out string[]? OutputNames)
        {
            OutputNames = [CodeGenUtils.SanitizeVarName(typeof(T).Name)];
        }
        public string GenerateCode(string[]? Arguments, string[]? UseOutputNames)
        {
            CodeGenUtils.CheckArgsAndOutputs(Arguments, Inputs.Count, UseOutputNames, 1, "MakeStructNode");
            throw new NotImplementedException();
            //return $"string {UseOutputNames![0]} = {Arguments![0]};";
        }
    }


}
