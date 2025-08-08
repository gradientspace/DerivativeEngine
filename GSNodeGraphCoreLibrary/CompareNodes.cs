// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Nodes
{
    //! this only works for POD types...
    [ClassHierarchyNode]
    public abstract class BinaryTestNode<T> : StandardNode, ICodeGen
        where T : struct
    {
        public BinaryTestNode()
        {
            AddInput(FirstInputName, new StandardNodeInputWithConstant<T>());
            AddInput(SecondInputName, new StandardNodeInputWithConstant<T>());

            AddOutput(ValueOutputName, new StandardNodeOutput<bool>());
        }

        public static string ValueOutputName { get { return "Bool"; } }
        public static string FirstInputName { get { return "A"; } }
        public static string SecondInputName { get { return "B"; } }

        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ValueOutputName);
            if (OutputIndex == -1)
                throw new Exception($"{GetDefaultNodeName()}: output not found");

            T A = new T();
            // can we simplify this block somehow?? isn't FindItemValueStrict going to throw??
            if (DataIn.FindItemValueStrict<T>(FirstInputName, ref A) == false) {
                object? Found = DataIn.FindItemValueAsType(FirstInputName, typeof(T));
                if (Found != null)
                    A = (T)Found;
                else
                    throw new Exception(this.GetType().Name + ": could not convert input to type " + typeof(T).Name);
            }

            T B = new T();
            if (DataIn.FindItemValueStrict<T>(SecondInputName, ref B) == false) {
                object? Found = DataIn.FindItemValueAsType(SecondInputName, typeof(T));
                if (Found != null)
                    B = (T)Found;
                else
                    throw new Exception(this.GetType().Name + ": could not convert input to type " + typeof(T).Name);
            }

            bool bTestResult = apply_op(ref A, ref B);
            RequestedDataOut.SetItemValue(OutputIndex, bTestResult);
        }

        protected abstract bool apply_op(ref T a, ref T b);

        public void GetCodeOutputNames(out string[]? OutputNames)
        {
            OutputNames = [ValueOutputName];
        }
        public string GenerateCode(string[]? Arguments, string[]? UseOutputNames)
        {
            CodeGenUtils.CheckArgsAndOutputs(Arguments, 2, UseOutputNames, 1, "GenericPODConstantNode");
            string testcode = op_code(Arguments![0], Arguments![1]);
            return $"bool {UseOutputNames![0]} = {testcode};";
        }
        protected abstract string op_code(string argA, string argB);
    }

    public class BoolEqualTest : BinaryTestNode<bool>
    {
        public override string GetCustomNodeName() { return "A == B"; }
        protected override bool apply_op(ref bool a, ref bool b) { return a == b; }
        protected override string op_code(string argA, string argB) { return "(" + argA + " == " + argB + ")"; }
    }

    public class BoolNotEqualTest : BinaryTestNode<bool>
    {
        public override string GetCustomNodeName() { return "A != B"; }
        protected override bool apply_op(ref bool a, ref bool b) { return a != b; }
        protected override string op_code(string argA, string argB) { return "(" + argA + " != " + argB + ")"; }
    }

}
