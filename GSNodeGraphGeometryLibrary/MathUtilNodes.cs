// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.Nodes;
using g3;


namespace Gradientspace.NodeGraph.Geometry3
{
    public class DoubleATan2PositiveNode : StandardBinaryMathOpNode<double, double, double>
    {
        public override string OpName => "ATan2Positive";
        public override string OpString => "ATan2Pos(A,B)";
        public override double ComputeOp(ref readonly double A, ref readonly double B) { return g3.MathUtil.Atan2Positive(A, B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = g3.MathUtil.Atan2Positive({A},{B})"; }
    }


}
