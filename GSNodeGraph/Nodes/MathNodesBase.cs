// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;


// todo maybe should move this somewhere else?
namespace Gradientspace
{
    public static class MathHelpers
    {
        public const double Deg2Rad = (Math.PI / 180.0);
        public const double Rad2Deg = (180.0 / Math.PI);
        public const double ZeroTolerance = (1e-08);
        public const double Epsilon = (2.2204460492503131e-016);

        public static double ToRadians(double Angle, bool bIsRadians)
        {
            return (bIsRadians) ? Angle : (Deg2Rad*Angle);
        }

        public static double Sin(double Angle, bool bIsRadians) 
        {
            return (bIsRadians) ? Math.Sin(Angle) : Math.Sin(Deg2Rad*Angle);
        }
        public static double Cos(double Angle, bool bIsRadians)
        {
            return (bIsRadians) ? Math.Cos(Angle) : Math.Cos(Deg2Rad*Angle);
        }
        public static double Tan(double Angle, bool bIsRadians)
        {
            return (bIsRadians) ? Math.Tan(Angle) : Math.Tan(Deg2Rad*Angle);
        }

        public static double ASin(double Value, bool bWantRadians)
        {
            return (bWantRadians) ? Math.Asin(Value) : Rad2Deg*Math.Asin(Deg2Rad*Value);
        }
        public static double ACos(double Value, bool bWantRadians)
        {
            return (bWantRadians) ? Math.Acos(Value) : Rad2Deg*Math.Acos(Deg2Rad*Value);
        }
        public static double ATan(double Value, bool bWantRadians)
        {
            return (bWantRadians) ? Math.Atan(Value) : Rad2Deg*Math.Atan(Value);
        }
        public static double ATan2(double Y, double X, bool bWantRadians)
        {
            return (bWantRadians) ? Math.Atan2(Y,X) : Rad2Deg*Math.Atan2(Y,X);
        }
    }
}



namespace Gradientspace.NodeGraph.Nodes
{
    public abstract class StandardMathOpNode : StandardNode
    {
        protected string ProcessCodeString(string codeString)
        {
            if (codeString.EndsWith('}') == false && codeString.EndsWith(';') == false)
                return codeString + ";";
            return codeString;
        }

    }



    public abstract class StandardMathConstantNode<TReturn> : StandardMathOpNode, ICodeGen
        where TReturn : struct
    {
        public static string ValueOutputName { get { return "Value"; } }

        public override string? GetNodeNamespace() { return OpNamespace; }
        public override string GetDefaultNodeName() { return OpString; }

        public virtual string OpNamespace => "Core.Math";
        public abstract string OpName { get; }
        public abstract string OpString { get; }
        public abstract TReturn ConstantValue { get; }

        public StandardMathConstantNode()
        {
            Flags = ENodeFlags.IsPure;
            AddOutput(ValueOutputName, new StandardNodeOutput<TReturn>());
        }

        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ValueOutputName);
            if (OutputIndex == -1)
                throw new Exception(OpName + ": output not found");
            RequestedDataOut.SetItemValue(OutputIndex, ConstantValue);
        }


        // ICodeGen interface
        public void GetCodeOutputNames(out string[]? OutputNames)
        {
            OutputNames = ["Value"];
        }
        public string GenerateCode(string[]? Arguments, string[]? UseOutputNames)
        {
            CodeGenUtils.CheckArgsAndOutputs(Arguments, 0, UseOutputNames, 1, this.ToString());
            string str = CodeString(UseOutputNames![0]);
            return ProcessCodeString(str);
        }
        protected virtual string CodeString(string Result)
        {
            return $"{Result} = default";
        }
    }




    public abstract class StandardUnaryMathOpNode<T1, TReturn> : StandardMathOpNode, ICodeGen
        where T1 : struct where TReturn : struct
    {
        public static string Operand1Name { get { return "A"; } }
        public static string ValueOutputName { get { return "Value"; } }

        public override string? GetNodeNamespace() { return OpNamespace; }
        public override string GetDefaultNodeName() { return OpString; }

        public virtual string OpNamespace => "Core.Math";
        public abstract string OpName { get; }
        public abstract string OpString { get; }
        public abstract TReturn ComputeOp(ref readonly T1 A);

        public StandardUnaryMathOpNode()
        {
            Flags = ENodeFlags.IsPure;

            AddInput(Operand1Name, new StandardNodeInputWithConstant<T1>());
            AddOutput(ValueOutputName, new StandardNodeOutput<TReturn>());
        }

        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ValueOutputName);
            if (OutputIndex == -1)
                throw new Exception(OpName + ": output not found");

            T1 A = DataIn.FindStructValueOrDefault<T1>(Operand1Name, default(T1), false);
            TReturn Result = ComputeOp(ref A);

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
            return ProcessCodeString(str);
        }
        protected virtual string CodeString(string A, string Result)
        {
            return $"{Result} = -{A}";
        }
    }



    public abstract class StandardBinaryMathOpNode<T1, T2, TReturn> : StandardMathOpNode, ICodeGen 
        where T1 : struct where T2 : struct where TReturn : struct
    {
        public virtual string Operand1Name => "A";
        public virtual string Operand2Name => "B";
        public virtual string ValueOutputName => "Value";

        public override string? GetNodeNamespace() { return OpNamespace; }
        public override string GetDefaultNodeName() { return OpString; }

        public virtual string OpNamespace => "Core.Math";
        public abstract string OpName { get; }
        public abstract string OpString { get; }
        public abstract TReturn ComputeOp(ref readonly T1 A, ref readonly T2 B);

        public StandardBinaryMathOpNode()
        {
            Flags = ENodeFlags.IsPure;

            AddInput(Operand1Name, new StandardNodeInputWithConstant<T1>());
            AddInput(Operand2Name, new StandardNodeInputWithConstant<T2>());
            AddOutput(ValueOutputName, new StandardNodeOutput<TReturn>());
        }

        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ValueOutputName);
            if (OutputIndex == -1)
                throw new Exception(OpName + ": output not found");

            T1 A = DataIn.FindStructValueOrDefault<T1>(Operand1Name, default(T1), false);
            T2 B = DataIn.FindStructValueOrDefault<T2>(Operand2Name, default(T2), false);
            TReturn Result = ComputeOp(ref A, ref B);

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
            return ProcessCodeString(str);
        }
        protected virtual string CodeString(string A, string B, string Result) {
            return $"{Result} = {A} + {B}";
        }
    }




    public abstract class StandardTrinaryMathOpNode<T1, T2, T3, TReturn> : StandardMathOpNode, ICodeGen
        where T1 : struct where T2 : struct where T3 : struct where TReturn : struct
    {
        public virtual string Operand1Name => "A";
        public virtual string Operand2Name => "B";
        public virtual string Operand3Name => "C";
        public virtual string ValueOutputName => "Value";

        public override string? GetNodeNamespace() { return OpNamespace; }
        public override string GetDefaultNodeName() { return OpString; }

        public virtual string OpNamespace => "Core.Math";
        public abstract string OpName { get; }
        public abstract string OpString { get; }
        public abstract TReturn ComputeOp(ref readonly T1 A, ref readonly T2 B, ref readonly T3 C);

        public StandardTrinaryMathOpNode()
        {
            Flags = ENodeFlags.IsPure;

            AddInput(Operand1Name, new StandardNodeInputWithConstant<T1>());
            AddInput(Operand2Name, new StandardNodeInputWithConstant<T2>());
            AddInput(Operand3Name, new StandardNodeInputWithConstant<T3>());
            AddOutput(ValueOutputName, new StandardNodeOutput<TReturn>());
        }

        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ValueOutputName);
            if (OutputIndex == -1)
                throw new Exception(OpName + ": output not found");

            T1 A = DataIn.FindStructValueOrDefault<T1>(Operand1Name, default(T1), false);
            T2 B = DataIn.FindStructValueOrDefault<T2>(Operand2Name, default(T2), false);
            T3 C = DataIn.FindStructValueOrDefault<T3>(Operand3Name, default(T3), false);
            TReturn Result = ComputeOp(ref A, ref B, ref C);

            RequestedDataOut.SetItemValue(OutputIndex, Result);
        }


        // ICodeGen interface
        public void GetCodeOutputNames(out string[]? OutputNames)
        {
            OutputNames = ["Value"];
        }
        public string GenerateCode(string[]? Arguments, string[]? UseOutputNames)
        {
            CodeGenUtils.CheckArgsAndOutputs(Arguments, 3, UseOutputNames, 1, this.ToString());
            string str = CodeString(Arguments![0], Arguments![1], Arguments![2], UseOutputNames![0]);
            return ProcessCodeString(str);
        }
        protected virtual string CodeString(string A, string B, string C, string Result)
        {
            return $"{Result} = {A} + {B}";
        }
    }



}
