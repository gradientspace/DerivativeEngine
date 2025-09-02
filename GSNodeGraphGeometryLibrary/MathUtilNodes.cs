// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.Nodes;
using g3;


namespace Gradientspace.NodeGraph.Geometry3
{
    //  WyvillFalloff
    //  LinearRampT


    public class DoubleATan2PositiveNode : StandardTrinaryMathOpNode<double, double, bool, double>
    {
        public override string Operand1Name => "Y";
        public override string Operand2Name => "X";
        public override string Operand3Name => "ToRadians";
        public override string ValueOutputName => "Angle";
        public override string OpName => "ATan2Positive";
        public override string OpString => "ATan2Pos(Y,X)";
        public override double ComputeOp(ref readonly double A, ref readonly double B, ref readonly bool C) { return g3.MathUtil.Atan2Positive(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = g3.MathUtil.Atan2Positive({A},{B},{C})"; }
    }


    public class DoubleLerpNode : StandardTrinaryMathOpNode<double, double, double, double>
    {
        public override string Operand3Name => "Alpha";
        public override string OpName => "Lerp";
        public override string OpString => "Lerp";
        public override double ComputeOp(ref readonly double A, ref readonly double B, ref readonly double C) { return g3.MathUtil.Lerp(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = g3.MathUtil.Lerp({A},{B},{C})"; }
    }
    public class DoubleSmoothStepNode : StandardTrinaryMathOpNode<double, double, double, double>
    {
        public override string Operand3Name => "Alpha";
        public override string OpName => "SmoothStep";
        public override string OpString => "SmoothStep";
        public override double ComputeOp(ref readonly double A, ref readonly double B, ref readonly double C) { return g3.MathUtil.SmoothStep(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = g3.MathUtil.SmoothStep({A},{B},{C})"; }
    }


    public class DoubleMin3Node : StandardTrinaryMathOpNode<double, double, double, double>
    {
        public override string OpName => "Min3";
        public override string OpString => "Min3";
        public override double ComputeOp(ref readonly double A, ref readonly double B, ref readonly double C) { return g3.MathUtil.Min(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = g3.MathUtil.Min({A},{B},{C})"; }
    }
    public class DoubleMax3Node : StandardTrinaryMathOpNode<double, double, double, double>
    {
        public override string OpName => "Max3";
        public override string OpString => "Max3";
        public override double ComputeOp(ref readonly double A, ref readonly double B, ref readonly double C) { return g3.MathUtil.Max(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = g3.MathUtil.Max({A},{B},{C})"; }
    }


    public class DoubleSignedClampNode : StandardTrinaryMathOpNode<double, double, double, double>
    {
        public override string Operand2Name => "Min";
        public override string Operand3Name => "Max";
        public override string OpName => "SignedClamp";
        public override string OpString => "SignedClamp";
        public override double ComputeOp(ref readonly double A, ref readonly double B, ref readonly double C) { return g3.MathUtil.SignedClamp(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = g3.MathUtil.SignedClamp({A},{B},{C})"; }
    }
    public class DoubleClampAngleDegNode : StandardTrinaryMathOpNode<double, double, double, double>
    {
        public override string Operand2Name => "MinAngle";
        public override string Operand3Name => "MaxAngle";
        public override string OpName => "ClampAngleDeg";
        public override string OpString => "ClampAngleDeg";
        public override double ComputeOp(ref readonly double A, ref readonly double B, ref readonly double C) { return g3.MathUtil.ClampAngleDeg(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = g3.MathUtil.ClampAngleDeg({A},{B},{C})"; }
    }
    public class DoubleClampAngleRadNode : StandardTrinaryMathOpNode<double, double, double, double>
    {
        public override string Operand2Name => "MinAngle";
        public override string Operand3Name => "MaxAngle";
        public override string OpName => "ClampAngleRad";
        public override string OpString => "ClampAngleRad";
        public override double ComputeOp(ref readonly double A, ref readonly double B, ref readonly double C) { return g3.MathUtil.ClampAngleRad(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = g3.MathUtil.ClampAngleRad({A},{B},{C})"; }
    }
    public class DoubleInRangeNode : StandardTrinaryMathOpNode<double, double, double, bool>
    {
        public override string Operand2Name => "Min";
        public override string Operand3Name => "MAx";
        public override string OpName => "InRange";
        public override string OpString => "InRange";
        public override bool ComputeOp(ref readonly double A, ref readonly double B, ref readonly double C) { return g3.MathUtil.InRange(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = g3.MathUtil.InRange({A},{B},{C})"; }
    }




}
