// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Nodes
{

    public enum EBinaryComparison
    {
        Equal = 0,
        NotEqual = 1,
        LessThan = 2, 
        LessThanOrEqual = 3,
        GreaterThan = 4, 
        GreaterThanOrEqual = 5
    }


    //! this only works for POD types...
    [ClassHierarchyNode]
    public abstract class BinaryTestNode<T> : StandardNode, ICodeGen
        where T : struct
    {
        public BinaryTestNode()
        {
            this.Flags = ENodeFlags.IsPure;

            AddInput(FirstInputName, new StandardNodeInputWithConstant<T>());
            AddInput(SecondInputName, new StandardNodeInputWithConstant<T>());

            AddOutput(ValueOutputName, new StandardNodeOutput<bool>());
        }

        public override string? GetNodeNamespace()
        {
            return "Core.Comparison";
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


        public static string GetToken(EBinaryComparison Comparison)
        {
            switch (Comparison) {
                case EBinaryComparison.Equal: return "==";
                case EBinaryComparison.NotEqual: return "!=";
                case EBinaryComparison.LessThan: return "<";
                case EBinaryComparison.LessThanOrEqual: return "<=";
                case EBinaryComparison.GreaterThan: return ">";
                case EBinaryComparison.GreaterThanOrEqual:  return ">=";
            }
            return "nop";
        }
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



    [ClassHierarchyNode]
    public class IntCompareNode<T> : BinaryTestNode<T> where T : struct, IBinaryInteger<T>
    {
        public EBinaryComparison Comparison = EBinaryComparison.Equal;

        public override string GetDefaultNodeName() {
            return "CompareInt";
        }
        public override string GetCustomNodeName() {
            return "A " + GetToken(Comparison) + " B";
        }
        protected override bool apply_op(ref T a, ref T b) { 
            switch (Comparison) {
                case EBinaryComparison.Equal: return a == b;
                case EBinaryComparison.NotEqual: return a != b;
                case EBinaryComparison.LessThan: return a < b;
                case EBinaryComparison.GreaterThan: return a > b;
                case EBinaryComparison.LessThanOrEqual: return a <= b;
                case EBinaryComparison.GreaterThanOrEqual: return a >= b;
            }
            return false; 
        }
        protected override string op_code(string argA, string argB) { return "(" + argA + " " + GetToken(Comparison) + " " + argB + ")"; }
    }

    public class Int32EqualNode : IntCompareNode<int> { public Int32EqualNode() { Comparison = EBinaryComparison.Equal; } }
    public class Int32NotEqualNode : IntCompareNode<int> { public Int32NotEqualNode() { Comparison = EBinaryComparison.NotEqual; } }
    public class Int32LessThanNode : IntCompareNode<int> { public Int32LessThanNode() { Comparison = EBinaryComparison.LessThan; } }
    public class Int32LessThanOrEqualNode : IntCompareNode<int> { public Int32LessThanOrEqualNode() { Comparison = EBinaryComparison.LessThanOrEqual; } }
    public class Int32GreaterThanNode : IntCompareNode<int> { public Int32GreaterThanNode() { Comparison = EBinaryComparison.GreaterThan; } }
    public class Int32GreaterThanOrEqualNode : IntCompareNode<int> { public Int32GreaterThanOrEqualNode() { Comparison = EBinaryComparison.GreaterThanOrEqual; } }

    public class Int64EqualNode : IntCompareNode<long> { public Int64EqualNode() { Comparison = EBinaryComparison.Equal; } }
    public class Int64NotEqualNode : IntCompareNode<long> { public Int64NotEqualNode() { Comparison = EBinaryComparison.NotEqual; } }
    public class Int64LessThanNode : IntCompareNode<long> { public Int64LessThanNode() { Comparison = EBinaryComparison.LessThan; } }
    public class Int64LessThanOrEqualNode : IntCompareNode<long> { public Int64LessThanOrEqualNode() { Comparison = EBinaryComparison.LessThanOrEqual; } }
    public class Int64GreaterThanNode : IntCompareNode<long> { public Int64GreaterThanNode() { Comparison = EBinaryComparison.GreaterThan; } }
    public class Int64GreaterThanOrEqualNode : IntCompareNode<long> { public Int64GreaterThanOrEqualNode() { Comparison = EBinaryComparison.GreaterThanOrEqual; } }





    [ClassHierarchyNode]
    public class RealCompareNode<T> : BinaryTestNode<T> where T : struct, IFloatingPoint<T>
    {
        public EBinaryComparison Comparison = EBinaryComparison.Equal;

        public override string GetDefaultNodeName() {
            return "CompareReal";
        }
        public override string GetCustomNodeName() {
            return "A " + GetToken(Comparison) + " B";
        }
        protected override bool apply_op(ref T a, ref T b) { 
            switch (Comparison) {
                case EBinaryComparison.Equal: return a == b;
                case EBinaryComparison.NotEqual: return a != b;
                case EBinaryComparison.LessThan: return a < b;
                case EBinaryComparison.GreaterThan: return a > b;
                case EBinaryComparison.LessThanOrEqual: return a <= b;
                case EBinaryComparison.GreaterThanOrEqual: return a >= b;
            }
            return false; 
        }
        protected override string op_code(string argA, string argB) { return "(" + argA + " " + GetToken(Comparison) + " " + argB + ")"; }
    }

    public class DoubleEqualNode : RealCompareNode<double> { public DoubleEqualNode() { Comparison = EBinaryComparison.Equal; } }
    public class DoubleNotEqualNode : RealCompareNode<double> { public DoubleNotEqualNode() { Comparison = EBinaryComparison.NotEqual; } }
    public class DoubleLessThanNode : RealCompareNode<double> { public DoubleLessThanNode() { Comparison = EBinaryComparison.LessThan; } }
    public class DoubleLessThanOrEqualNode : RealCompareNode<double> { public DoubleLessThanOrEqualNode() { Comparison = EBinaryComparison.LessThanOrEqual; } }
    public class DoubleGreaterThanNode : RealCompareNode<double> { public DoubleGreaterThanNode() { Comparison = EBinaryComparison.GreaterThan; } }
    public class DoubleGreaterThanOrEqualNode : RealCompareNode<double> { public DoubleGreaterThanOrEqualNode() { Comparison = EBinaryComparison.GreaterThanOrEqual; } }


}
