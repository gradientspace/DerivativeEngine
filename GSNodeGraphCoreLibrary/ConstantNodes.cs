// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using System.Diagnostics;
using System.Reflection;

namespace Gradientspace.NodeGraph.Nodes
{
    //! this only works for POD types...
    [ClassHierarchyNode]
    public class GenericPODConstantNode<T> : StandardNode, ICodeGen 
        where T : struct
    {
        public GenericPODConstantNode()
        {
            AddOutput(ValueOutputName, new StandardNodeOutput<T>());

			StandardNodeInputBase input = new StandardNodeInputWithConstant<T>(new T());
            input.Flags = ENodeInputFlags.IsNodeConstant | ENodeInputFlags.HiddenLabel;
			AddInput(ValueInputName, input);
        }

        public static string ValueOutputName { get { return "Value"; } }
        public static string ValueInputName { get { return "Value"; } }

        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ValueOutputName);
            if (OutputIndex == -1)
                throw new Exception($"{GetDefaultNodeName()}: output not found");

            T Value = new T();
            if ( DataIn.FindItemValueStrict<T>(ValueInputName, ref Value) == false )
            {
                object? Found = DataIn.FindItemValueAsType(ValueInputName, typeof(T));
                if (Found != null)
                    Value = (T)Found;
                else
                    throw new Exception(this.GetType().Name + ": could not convert input to type " + typeof(T).Name);
            }

            RequestedDataOut.SetItemValue(OutputIndex, Value);
        }

        public void GetCodeOutputNames(out string[]? OutputNames)
        {
            OutputNames = [ValueOutputName];
        }
        public string GenerateCode(string[]? Arguments, string[]? UseOutputNames)
        {
            CodeGenUtils.CheckArgsAndOutputs(Arguments, 1, UseOutputNames, 1, "GenericPODConstantNode");
            string useTypeName = CodeGenUtils.GetCSharpTypeDecl(typeof(T));
            return $"{useTypeName} {UseOutputNames![0]} = {Arguments![0]};";
        }
    }



    [GraphNodeNamespace("Gradientspace.Constants")]
    [GraphNodeUIName("Make Int32")]
    public class Int32ConstantNode : GenericPODConstantNode<int>
    {
        public override string GetDefaultNodeName() { return "Make Int32"; }
    }

    [GraphNodeNamespace("Gradientspace.Constants")]
    [GraphNodeUIName("Make Int64")]
    public class Int64ConstantNode : GenericPODConstantNode<long>
    {
        public override string GetDefaultNodeName() { return "Make Int64"; }
    }

    [GraphNodeNamespace("Gradientspace.Constants")]
    [GraphNodeUIName("Make Float")]
    public class FloatConstantNode : GenericPODConstantNode<float>
    {
        public override string GetDefaultNodeName() { return "Make Float"; }
    }

    [GraphNodeNamespace("Gradientspace.Constants")]
    [GraphNodeUIName("Make Double")]
    public class DoubleConstantNode : GenericPODConstantNode<double>
    {
        public override string GetDefaultNodeName() { return "Make Double"; }
    }

    [GraphNodeNamespace("Gradientspace.Constants")]
    [GraphNodeUIName("Make Boolean")]
    public class BoolConstantNode : GenericPODConstantNode<bool>
    {
        public override string GetDefaultNodeName() { return "Make Boolean"; }
    }


    [GraphNodeNamespace("Gradientspace.Constants")]
    [GraphNodeUIName("Make String")]
    public class StringConstantNode : StandardNode, ICodeGen
    {
        public override string GetDefaultNodeName() { return "Make String"; }

        public StringConstantNode()
        {
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


    //public struct TestVector3
    //{
    //    public float x = 1;
    //    public float y = 2;
    //    public float z = 3;
    //    //int w = 7;
    //    public bool flag = true;
    //    public TestVector3() { }
    //}

    //[GraphNodeFunctionLibrary("Constants")]
    //[GraphNodeUIName("Make TestVector3")]
    //public class MakeTestVector3 : MakeStructNode<TestVector3>
    //{
    //    public override string GetDefaultNodeName() { return "Make TestVector3"; }
    //}


}
