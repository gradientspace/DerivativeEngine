// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;

namespace Gradientspace.NodeGraph.Nodes
{
    // TODO:
    //  (A,B) = SinCos(V)
    //  A = Round(B, NumDigits)
    //  FastInvSqrt?
    //  ILogB
    //  IEEERemainder / FMod
    //  FusedMultiplyAdd
    //  Clamp
    //  BitIncrement/BitDecrement
    //  CopySign
    //  OneOver
    //  InRange  (3-arg)
    // EpsilonEqual (3-arg)
    // Min3, Max3
    // Atan2Positive (g3)


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
        public override string OpString => "Sqrt(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Sqrt(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Sqrt({A})"; }
    }
    public class DoubleCbrtNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Cbrt";
        public override string OpString => "Cbrt(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Cbrt(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Cbrt({A})"; }
    }
    public class DoubleExpNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Exp";
        public override string OpString => "Exp(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Exp(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Exp({A})"; }
    }
    public class DoubleLnNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Ln";
        public override string OpString => "Ln(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Log(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Log({A})"; }
    }
    public class DoubleLog10Node : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Log10";
        public override string OpString => "Log10(A)";
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
        public override string OpString => "Abs(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Abs(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Abs({A})"; }
    }
    public class DoubleSignNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Sign";
        public override string OpString => "Sign(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Sign(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Sign({A})"; }
    }
    public class DoubleRoundNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Round";
        public override string OpString => "Round(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Round(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Round({A})"; }
    }
    public class DoubleTruncateNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Trunc";
        public override string OpString => "Trunc(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Truncate(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Truncate({A})"; }
    }
    public class DoubleFloorNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Floor";
        public override string OpString => "Floor(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Floor(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Floor({A})"; }
    }
    public class DoubleCeilNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Ceil";
        public override string OpString => "Ceil(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Ceiling(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Ceiling({A})"; }
    }
    public class DoubleMinNode : StandardBinaryMathOpNode<double, double, double>
    {
        public override string OpName => "Min";
        public override string OpString => "Min(A,B)";
        public override double ComputeOp(ref readonly double A, ref readonly double B) { return Math.Min(A, B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = Math.Min({A},{B})"; }
    }
    public class DoubleMaxNode : StandardBinaryMathOpNode<double, double, double>
    {
        public override string OpName => "Max";
        public override string OpString => "Max(A,B)";
        public override double ComputeOp(ref readonly double A, ref readonly double B) { return Math.Max(A, B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = Math.Max({A},{B})"; }
    }


    public class DoubleSinNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Sin";
        public override string OpString => "Sin(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Sin(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Sin({A})"; }
    }
    public class DoubleCosNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Cos";
        public override string OpString => "Cos(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Cos(A); }
        protected override string CodeString(string A, string Result) { return "{Result} = Math.Cos({A})"; }
    }
    public class DoubleTanNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Tan";
        public override string OpString => "Tan(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Tan(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Tan({A})"; }
    }
    public class DoubleASinNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "ASin";
        public override string OpString => "ASin(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Asin(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Asin({A})"; }
    }
    public class DoubleACosNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "ACos";
        public override string OpString => "ACos(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Acos(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Acos({A})"; }
    }
    public class DoubleATanNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "ATan";
        public override string OpString => "ATan(A)";
        public override double ComputeOp(ref readonly double A) { return Math.Atan(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Atan({A})"; }
    }
    public class DoubleATan2Node : StandardBinaryMathOpNode<double, double, double>
    {
        public override string OpName => "ATan2";
        public override string OpString => "ATan2(A,B)";
        public override double ComputeOp(ref readonly double A, ref readonly double B) { return Math.Atan2(A, B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = Math.Atan2({A},{B})"; }
    }
    public class DoubleRad2DegNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Rad2Deg";
        public override string OpString => "Rad2Deg";
        public override double ComputeOp(ref readonly double A) { return (180.0 / Math.PI) * A; }
        protected override string CodeString(string A, string Result) { return $"{Result} = (180.0 / Math.PI)*({A})"; }
    }
    public class DoubleDeg2RadNode : StandardUnaryMathOpNode<double, double>
    {
        public override string OpName => "Deg2Rad";
        public override string OpString => "Deg2Rad";
        public override double ComputeOp(ref readonly double A) { return (Math.PI / 180.0) * A; }
        protected override string CodeString(string A, string Result) { return $"{Result} = (Math.PI / 180.0)*({A})"; }
    }




    public class DoubleIsFiniteNode : StandardUnaryMathOpNode<double, bool>
    {
        public override string OpName => "IsFinite";
        public override string OpString => "IsFinite";
        public override bool ComputeOp(ref readonly double A) { return double.IsInfinity(A) == false && double.IsNaN(A) == false; }
        protected override string CodeString(string A, string Result) { return $"{Result} = (double.IsInfinity({A}) == false && double.IsNaN({A}) == false)"; }
    }

}
