// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;

namespace Gradientspace.NodeGraph.Nodes
{
    public abstract class StandardMathOpNode : StandardNode
    {
    }


    public abstract class StandardUnaryMathOpNode<T1, T2> : StandardMathOpNode, ICodeGen
        where T1 : struct where T2 : struct
    {
        public static string Operand1Name { get { return "A"; } }
        public static string ValueOutputName { get { return "Value"; } }

        public override string? GetNodeNamespace() { return "Core.Math"; }
        public override string GetDefaultNodeName() { return OpString; }


        public abstract string OpName { get; }
        public abstract string OpString { get; }
        public abstract T2 ComputeOp(ref readonly T1 A);

        public StandardUnaryMathOpNode()
        {
            AddInput(Operand1Name, new StandardNodeInputWithConstant<T1>());
            AddOutput(ValueOutputName, new StandardNodeOutput<T2>());
        }

        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ValueOutputName);
            if (OutputIndex == -1)
                throw new Exception(OpName + ": output not found");

            T1 A = DataIn.FindStructValueOrDefault<T1>(Operand1Name, default(T1), false);
            T2 Result = ComputeOp(ref A);

            RequestedDataOut.SetItemValue(OutputIndex, Result);
        }


        // ICodeGen interface
        public void GetCodeOutputNames(out string[]? OutputNames)
        {
            OutputNames = ["Value"];
        }
        public string GenerateCode(string[]? Arguments, string[]? UseOutputNames)
        {
            CodeGenUtils.CheckArgsAndOutputs(Arguments, 1, UseOutputNames, 1, this.ToString());
            string str = CodeString(Arguments![0], UseOutputNames![0]);
            if (str.EndsWith(';') == false)
                str = str + ";";
            return str;
        }
        protected virtual string CodeString(string A, string Result)
        {
            return $"{Result} = -{A}";
        }
    }



    public abstract class StandardBinaryMathOpNode<T1, T2, T3> : StandardMathOpNode, ICodeGen 
        where T1 : struct where T2 : struct where T3 : struct
    {
        public static string Operand1Name { get { return "A"; } }
        public static string Operand2Name { get { return "B"; } }
        public static string ValueOutputName { get { return "Value"; } }

        public override string? GetNodeNamespace() { return "Core.Math"; }
        public override string GetDefaultNodeName() { return OpString; }


        public abstract string OpName { get; }
        public abstract string OpString { get; }
        public abstract T3 ComputeOp(ref readonly T1 A, ref readonly T2 B);

        public StandardBinaryMathOpNode()
        {
            AddInput(Operand1Name, new StandardNodeInputWithConstant<T1>());
            AddInput(Operand2Name, new StandardNodeInputWithConstant<T2>());
            AddOutput(ValueOutputName, new StandardNodeOutput<T3>());
        }

        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ValueOutputName);
            if (OutputIndex == -1)
                throw new Exception(OpName + ": output not found");

            T1 A = DataIn.FindStructValueOrDefault<T1>(Operand1Name, default(T1), false);
            T2 B = DataIn.FindStructValueOrDefault<T2>(Operand1Name, default(T2), false);
            T3 Result = ComputeOp(ref A, ref B);

            RequestedDataOut.SetItemValue(OutputIndex, Result);
        }


        // ICodeGen interface
        public void GetCodeOutputNames(out string[]? OutputNames)
        {
            OutputNames = ["Value"];
        }
        public string GenerateCode(string[]? Arguments, string[]? UseOutputNames)
        {
            CodeGenUtils.CheckArgsAndOutputs(Arguments, 2, UseOutputNames, 1, this.ToString());
            string str = CodeString(Arguments![0], Arguments![1], UseOutputNames![0]);
            if (str.EndsWith(';') == false)
                str = str + ";";
            return str;
        }
        protected virtual string CodeString(string A, string B, string Result) {
            return $"{Result} = {A} + {B}";
        }
    }



}
