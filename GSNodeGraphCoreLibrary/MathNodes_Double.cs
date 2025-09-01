// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;

namespace Gradientspace.NodeGraph.Nodes
{
    // TODO:
    //  (A,B) = SinCos(V)
    //  ILogB
    //  IEEERemainder / FMod
    //  BitIncrement/BitDecrement
    //  multi-add/multiply?


    public class DoublePiNode : StandardMathConstantNode<double>
    {
        public override string OpName => "Pi";
        public override string OpString => "Pi";
        public override double ConstantValue => Math.PI;
        protected override string CodeString(string Result) { return $"{Result} = Math.PI"; }
    }
    public class DoubleHalfPiNode : StandardMathConstantNode<double>
    {
        public override string OpName => "HalfPi";
        public override string OpString => "HalfPi";
        public override double ConstantValue => Math.PI * 0.5;
        protected override string CodeString(string Result) { return $"{Result} = (Math.PI*0.5)"; }
    }
    public class DoubleTwoPiNode : StandardMathConstantNode<double>
    {
        public override string OpName => "TwoPi";
        public override string OpString => "TwoPi";
        public override double ConstantValue => Math.PI * 2.0;
        protected override string CodeString(string Result) { return $"{Result} = (Math.PI*0.2.0)"; }
    }
    public class DoubleZeroToleranceNode : StandardMathConstantNode<double>
    {
        public override string OpName => "ZeroTol";
        public override string OpString => "ZeroTol";
        public override double ConstantValue => 1e-08;
        protected override string CodeString(string Result) { return $"{Result} = (1e-08)"; }
    }
    public class DoubleEpsilonNode : StandardMathConstantNode<double>
    {
        public override string OpName => "Epsilon";
        public override string OpString => "Epsilon";
        public override double ConstantValue => 2.2204460492503131e-016;
        protected override string CodeString(string Result) { return $"{Result} = (2.2204460492503131e-016)"; }
    }


    public class DoubleAddNode : StandardBinaryMathOpNode<double, double, double>
    {
        public override string OpName => "Add";
        public override string OpString => "A + B";
        public override double ComputeOp(ref readonly double A, ref readonly double B) { return A + B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) + ({B})"; }
    }
    public class DoubleSubtractNode : StandardBinaryMathOpNode<double, double, double>
    {
        public override string OpName => "Subtract";
        public override string OpString => "A - B";
        public override double ComputeOp(ref readonly double A, ref readonly double B) { return A - B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) - ({B})"; }
    }
    public class DoubleMultiplyNode : StandardBinaryMathOpNode<double, double, double>
    {
        public override string OpName => "Multiply";
        public override string OpString => "A * B";
        public override double ComputeOp(ref readonly double A, ref readonly double B) { return A * B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) * ({B})"; }
    }
    public class DoubleDivideNode : StandardBinaryMathOpNode<double, double, double>
    {
        public override string OpName => "Divide";
        public override string OpString => "A / B";
        public override double ComputeOp(ref readonly double A, ref readonly double B) { return A / B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) / ({B})"; }
    }
    public class DoubleNegateNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Negate";
        public override string OpString => "-A";
        public override double ComputeOp(ref readonly double A) { return -A; }
        protected override string CodeString(string A, string Result) { return $"{Result} = -({A})"; }
    }
    public class DoubleOneOverNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "OneOver";
        public override string OpString => "1 / A";
        public override double ComputeOp(ref readonly double A) { return 1.0 / A; }
        protected override string CodeString(string A, string Result) { return $"{Result} = 1.0 / ({A})"; }
    }
    public class DoubleOneMinusNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "OneMinus";
        public override string OpString => "1-A";
        public override double ComputeOp(ref readonly double A) { return 1.0-A; }
        protected override string CodeString(string A, string Result) { return $"{Result} = 1.0-({A})"; }
    }
    public class DoubleMulAddNode : StandardTrinaryMathOpNode<double, double, double, double>
    {
        public override string OpName => "A*B+C";
        public override string OpString => "A*B+C";
        public override double ComputeOp(ref readonly double A, ref readonly double B, ref readonly double C) { return Math.FusedMultiplyAdd(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = Math.FusedMultiplyAdd({A},{B},{C})"; }
    }
    public class DoubleCopySignNode : StandardBinaryMathOpNode<double, double, double>
    {
        public override string Operand2Name => "Sign";
        public override string OpName => "CopySign";
        public override string OpString => "CopySign";
        public override double ComputeOp(ref readonly double A, ref readonly double B) { return Math.CopySign(A,B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = Math.CopySign({A},{B})"; }
    }



    public class DoublePowNode : StandardBinaryMathOpNode<double, double, double>
    {
        public override string OpName => "Pow";
        public override string OpString => "A ^ B";
        public override double ComputeOp(ref readonly double A, ref readonly double B) { return Math.Pow(A, B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = Math.Pow({A},{B})"; }
    }
    public class DoubleSqrtNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Sqrt";
        public override string OpString => "Sqrt";
        public override double ComputeOp(ref readonly double A) { return Math.Sqrt(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Sqrt({A})"; }
    }
    public class DoubleInvSqrtNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "InvSqrt";
        public override string OpString => "InvSqrt";
        public override double ComputeOp(ref readonly double A) { return 1.0/Math.Sqrt(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = 1.0/Math.Sqrt({A})"; }
    }
    public class DoubleCbrtNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Cbrt";
        public override string OpString => "Cbrt";
        public override double ComputeOp(ref readonly double A) { return Math.Cbrt(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Cbrt({A})"; }
    }
    public class DoubleExpNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Exp";
        public override string OpString => "Exp";
        public override double ComputeOp(ref readonly double A) { return Math.Exp(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Exp({A})"; }
    }
    public class DoubleLnNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Ln";
        public override string OpString => "Ln";
        public override double ComputeOp(ref readonly double A) { return Math.Log(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Log({A})"; }
    }
    public class DoubleLog10Node : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Log10";
        public override string OpString => "Log10";
        public override double ComputeOp(ref readonly double A) { return Math.Log10(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Log10({A})"; }
    }
    public class DoubleLogNode : StandardBinaryMathOpNode<double, double, double>
    {
        public override string OpName => "Log";
        public override string OpString => "Log(A,B)";
        public override double ComputeOp(ref readonly double A, ref readonly double B) { return Math.Log(A, B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = Math.Log({A},{B})"; }
    }


    public class DoubleAbsNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Abs";
        public override string OpString => "Abs";
        public override double ComputeOp(ref readonly double A) { return Math.Abs(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Abs({A})"; }
    }
    public class DoubleSignNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Sign";
        public override string OpString => "Sign";
        public override double ComputeOp(ref readonly double A) { return Math.Sign(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Sign({A})"; }
    }
    public class DoubleRoundFracNode : StandardBinaryMathOpNode<double, int, double>
    {
        public override string Operand2Name => "Decimals";
        public override string OpName => "Round";
        public override string OpString => "Round";
        public override double ComputeOp(ref readonly double A, ref readonly int B) { return Math.Round(A, B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = Math.Round({A},{B})"; }
    }
    public class DoubleTruncateNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Trunc";
        public override string OpString => "Trunc";
        public override double ComputeOp(ref readonly double A) { return Math.Truncate(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Truncate({A})"; }
    }
    public class DoubleFloorNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Floor";
        public override string OpString => "Floor";
        public override double ComputeOp(ref readonly double A) { return Math.Floor(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Floor({A})"; }
    }
    public class DoubleCeilNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Ceil";
        public override string OpString => "Ceil";
        public override double ComputeOp(ref readonly double A) { return Math.Ceiling(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Ceiling({A})"; }
    }
    public class DoubleMinNode : StandardBinaryMathOpNode<double, double, double>
    {
        public override string OpName => "Min";
        public override string OpString => "Min";
        public override double ComputeOp(ref readonly double A, ref readonly double B) { return Math.Min(A, B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = Math.Min({A},{B})"; }
    }
    public class DoubleMaxNode : StandardBinaryMathOpNode<double, double, double>
    {
        public override string OpName => "Max";
        public override string OpString => "Max";
        public override double ComputeOp(ref readonly double A, ref readonly double B) { return Math.Max(A, B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = Math.Max({A},{B})"; }
    }
    public class DoubleClampNode : StandardTrinaryMathOpNode<double, double, double, double>
    {
        public override string Operand2Name => "Min";
        public override string Operand3Name => "Max";
        public override string OpName => "Clamp";
        public override string OpString => "Clamp";
        public override double ComputeOp(ref readonly double A, ref readonly double B, ref readonly double C) { return Math.Clamp(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = Math.Clamp({A},{B},{C})"; }
    }


    public abstract class DoubleTrigNode : StandardBinaryMathOpNode<double, bool, double>
    {
        public override string Operand1Name => "Angle";
        public override string Operand2Name => "IsRadians";
    }
    public class DoubleSinNode : DoubleTrigNode
    {
        public override string OpName => "Sin";
        public override string OpString => "Sin";
        public override double ComputeOp(ref readonly double A, ref readonly bool B) { return MathHelpers.Sin(A,B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = MathHelpers.Sin({A},{B})"; }
    }
    public class DoubleCosNode : DoubleTrigNode
    {
        public override string OpName => "Cos";
        public override string OpString => "Cos";
        public override double ComputeOp(ref readonly double A, ref readonly bool B) { return MathHelpers.Cos(A,B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = MathHelpers.Cos({A},{B})"; }
    }
    public class DoubleTanNode : DoubleTrigNode
    {
        public override string OpName => "Tan";
        public override string OpString => "Tan";
        public override double ComputeOp(ref readonly double A, ref readonly bool B) { return MathHelpers.Tan(A,B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = MathHelpers.Tan({A},{B})"; }
    }


    public abstract class DoubleATrigNode : StandardBinaryMathOpNode<double, bool, double>
    {
        public override string Operand1Name => "Value";
        public override string Operand2Name => "ToRadians";
        public override string ValueOutputName => "Angle";
    }
    public class DoubleASinNode : DoubleATrigNode
    {
        public override string OpName => "ASin";
        public override string OpString => "ASin";
        public override double ComputeOp(ref readonly double A, ref readonly bool B) { return MathHelpers.ASin(A,B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = MathHelpers.ASin({A},{B})"; }
    }
    public class DoubleACosNode : DoubleATrigNode
    {
        public override string OpName => "ACos";
        public override string OpString => "ACos";
        public override double ComputeOp(ref readonly double A, ref readonly bool B) { return MathHelpers.ACos(A,B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = MathHelpers.ACos({A},{B})"; }
    }
    public class DoubleATanNode : DoubleATrigNode
    {
        public override string OpName => "ATan";
        public override string OpString => "ATan(A)";
        public override double ComputeOp(ref readonly double A, ref readonly bool B) { return MathHelpers.ATan(A,B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = MathHelpers.ATan({A},{B})"; }
    }
    public class DoubleATan2Node : StandardTrinaryMathOpNode<double, double, bool, double>
    {
        public override string Operand1Name => "Y";
        public override string Operand2Name => "X";
        public override string Operand3Name => "ToRadians";
        public override string ValueOutputName => "Angle";
        public override string OpName => "ATan2";
        public override string OpString => "ATan2(Y,X)";
        public override double ComputeOp(ref readonly double A, ref readonly double B, ref readonly bool C) { return MathHelpers.ATan2(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = MathHelpers.ATan2({A},{B},{C})"; }
    }
    public class DoubleRad2DegNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Rad2Deg";
        public override string OpString => "Rad2Deg";
        public override double ComputeOp(ref readonly double A) { return MathHelpers.Rad2Deg * A; }
        protected override string CodeString(string A, string Result) { return $"{Result} = MathHelpers.Rad2Deg*({A})"; }
    }
    public class DoubleDeg2RadNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Deg2Rad";
        public override string OpString => "Deg2Rad";
        public override double ComputeOp(ref readonly double A) { return MathHelpers.Deg2Rad * A; }
        protected override string CodeString(string A, string Result) { return $"{Result} = MathHelpers.Deg2Rad*({A})"; }
    }




    public class DoubleIsFiniteNode : StandardUnaryMathOpNode<double, bool>
    {
        public override string OpName => "IsFinite";
        public override string OpString => "IsFinite";
        public override bool ComputeOp(ref readonly double A) { return double.IsInfinity(A) == false && double.IsNaN(A) == false; }
        protected override string CodeString(string A, string Result) { return $"{Result} = (double.IsInfinity({A}) == false && double.IsNaN({A}) == false)"; }
    }
    public class DoubleEpsEqualNode : StandardTrinaryMathOpNode<double, double, double, bool>
    {
        public override string Operand3Name => "Epsilon";
        public override string OpName => "EpsEqual";
        public override string OpString => "EpsEqual";
        public override bool ComputeOp(ref readonly double A, ref readonly double B, ref readonly double C) { return Math.Abs((A)-(B)) < (C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = Math.Abs(({A})-({B})) < ({C})"; }
    }

}
