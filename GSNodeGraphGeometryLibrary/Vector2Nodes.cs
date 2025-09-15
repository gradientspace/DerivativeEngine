// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.Nodes;

namespace Gradientspace.NodeGraph.Geometry
{
    public class Vector2ZeroNode : StandardMathConstantNode<Vector2d>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "Zero";
        public override string OpString => "Zero";
        public override Vector2d ConstantValue => Vector2d.Zero;
        protected override string CodeString(string Result) { return $"{Result} = Vector2d.Zero"; }
    }
    public class Vector2OneNode : StandardMathConstantNode<Vector2d>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "One";
        public override string OpString => "One";
        public override Vector2d ConstantValue => Vector2d.One;
        protected override string CodeString(string Result) { return $"{Result} = Vector2d.One"; }
    }
    public class Vector2UnitXNode : StandardMathConstantNode<Vector2d>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "UnitX";
        public override string OpString => "UnitX";
        public override Vector2d ConstantValue => Vector2d.AxisX;
        protected override string CodeString(string Result) { return $"{Result} = Vector2d.AxisX"; }
    }
    public class Vector2UnitYNode : StandardMathConstantNode<Vector2d>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "UnitY";
        public override string OpString => "UnitY";
        public override Vector2d ConstantValue => Vector2d.AxisY;
        protected override string CodeString(string Result) { return $"{Result} = Vector2d.AxisY"; }
    }


    [GraphNodeUIName("Vector2d")]
    public class Vector2ConstantNode : GenericPODConstantNode<g3.Vector2d>
    {
        public override string? GetNodeNamespace() { return "Geometry3.Vector2"; }
        public override string GetDefaultNodeName() { return "Vector2d"; }
    }
    public class Vector2MakeNode : StandardBinaryMathOpNode<double, double, Vector2d>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string Operand1Name => "X";
        public override string Operand2Name => "Y";
        public override string OpName => "MakeVec2";
        public override string OpString => "MakeVec2";
        public override Vector2d ComputeOp(ref readonly double A, ref readonly double B) { return new Vector2d(A, B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = new Vector2d({A}, {B})"; }
    }
    public class Vector2BreakNode : StandardUnaryMathOpNode2<Vector2d, double, double>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "BreakVec2";
        public override string OpString => "BreakVec2";
        public override string Operand1Name => "V";
        public override string Output1Name => "X";
        public override string Output2Name => "Y";
        public override (double, double) ComputeOp(ref readonly Vector2d A) { return (A.x, A.y); }
        protected override string CodeString(string A, string Result1, string Result2) { return $"{Result1}=({A}).x; {Result2}=({A}).y;"; }
    }


    public class Vector2AddNode : StandardBinaryMathOpNode<Vector2d, Vector2d, Vector2d>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "Add";
        public override string OpString => "A + B";
        public override Vector2d ComputeOp(ref readonly Vector2d A, ref readonly Vector2d B) { return A + B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) + ({B})"; }
    }
    public class Vector2SubtractNode : StandardBinaryMathOpNode<Vector2d, Vector2d, Vector2d>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "Subtract";
        public override string OpString => "A - B";
        public override Vector2d ComputeOp(ref readonly Vector2d A, ref readonly Vector2d B) { return A - B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) - ({B})"; }
    }
    public class Vector2MultiplyNode : StandardBinaryMathOpNode<Vector2d, Vector2d, Vector2d>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "Multiply";
        public override string OpString => "A * B";
        public override Vector2d ComputeOp(ref readonly Vector2d A, ref readonly Vector2d B) { return A * B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) * ({B})"; }
    }
    public class Vector2DivideNode : StandardBinaryMathOpNode<Vector2d, Vector2d, Vector2d>
    {
        public override object? Operand2Default => Vector2d.One;
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "Divide";
        public override string OpString => "A / B";
        public override Vector2d ComputeOp(ref readonly Vector2d A, ref readonly Vector2d B) { return A / B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) / ({B})"; }
    }
    public class Vector2NegateNode : StandardUnaryMathOpNode<Vector2d, Vector2d>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "Negate";
        public override string OpString => "-A";
        public override Vector2d ComputeOp(ref readonly Vector2d A) { return -A; }
        protected override string CodeString(string A, string Result) { return $"{Result} = -({A})"; }
    }

    public class Vector2NormalizeNode : StandardUnaryMathOpNode2<Vector2d, Vector2d, double>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "Normalize";
        public override string Operand1Name => "Vec2";
        public override string Output1Name => "UnitVec2";
        public override string Output2Name => "Length"; 
        public override string OpString => "Normalize";
        public override (Vector2d,double) ComputeOp(ref readonly Vector2d A) { Vector2d tmp = A; double len = tmp.Normalize(); return (tmp, len);  }
        protected override string CodeString(string A, string Result1, string Result2) { return $"{{Vector2d tmp = {A}; {Result2} = tmp.Normalize(); {Result1}=tmp;}}"; }
    }

    public class Vector2DotNode : StandardBinaryMathOpNode<Vector2d, Vector2d, double>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "Dot";
        public override string OpString => "A . B";
        public override double ComputeOp(ref readonly Vector2d A, ref readonly Vector2d B) { return A.Dot(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).Dot({B})"; }
    }


    public class Vector2AbsNode : StandardUnaryMathOpNode<Vector2d, Vector2d>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "Abs";
        public override string OpString => "Abs";
        public override Vector2d ComputeOp(ref readonly Vector2d A) { return A.Abs(); }
        protected override string CodeString(string A, string Result) { return $"{Result} = ({A}).Abs()"; }
    }
    public class Vector2RoundFracNode : StandardBinaryMathOpNode<Vector2d, int, Vector2d>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string Operand2Name => "Decimals";
        public override string OpName => "Round";
        public override string OpString => "Round";
        public override Vector2d ComputeOp(ref readonly Vector2d A, ref readonly int B) { return A.RoundFrac(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).RoundFrac({B})"; }
    }


    public class Vector2LengthNode : StandardUnaryMathOpNode<Vector2d, double>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "Length";
        public override string OpString => "Length";
        public override double ComputeOp(ref readonly Vector2d A) { return A.Length; }
        protected override string CodeString(string A, string Result) { return $"{Result} = ({A}).Length"; }
    }
    public class Vector2LengthSquaredNode : StandardUnaryMathOpNode<Vector2d, double>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "LengthSqr";
        public override string OpString => "LengthSqr";
        public override double ComputeOp(ref readonly Vector2d A) { return A.LengthSquared; }
        protected override string CodeString(string A, string Result) { return $"{Result} = ({A}).LengthSquared"; }
    }
    public class Vector2DistanceNode : StandardBinaryMathOpNode<Vector2d, Vector2d, double>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "Distance";
        public override string OpString => "Distance";
        public override double ComputeOp(ref readonly Vector2d A, ref readonly Vector2d B) { return A.Distance(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).Distance({B})"; }
    }
    public class Vector2DistanceSqrNode : StandardBinaryMathOpNode<Vector2d, Vector2d, double>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "DistanceSqr";
        public override string OpString => "DistanceSqr";
        public override double ComputeOp(ref readonly Vector2d A, ref readonly Vector2d B) { return A.DistanceSquared(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).DistanceSquared({B})"; }
    }



    public class Vector2AngleDegNode : StandardBinaryMathOpNode<Vector2d, Vector2d, double>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "AngleDeg";
        public override string OpString => "AngleDeg";
        public override double ComputeOp(ref readonly Vector2d A, ref readonly Vector2d B) { return A.AngleD(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).AngleD({B})"; }
    }
    public class Vector2AngleRadNode : StandardBinaryMathOpNode<Vector2d, Vector2d, double>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string OpName => "AngleRad";
        public override string OpString => "AngleRad";
        public override double ComputeOp(ref readonly Vector2d A, ref readonly Vector2d B) { return A.AngleR(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).AngleR({B})"; }
    }


    public class Vector2LerpNode : StandardTrinaryMathOpNode<Vector2d, Vector2d, double, Vector2d>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string Operand3Name => "T";
        public override string OpName => "Lerp";
        public override string OpString => "Lerp";
        public override Vector2d ComputeOp(ref readonly Vector2d A, ref readonly Vector2d B, ref readonly double C) { return Vector2d.Lerp(A,B,C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = Vector2d.Lerp({A},{B},{C})"; }
    }


    public class Vector2EpsEqualNode : StandardTrinaryMathOpNode<Vector2d, Vector2d, double, bool>
    {
        public override string OpNamespace => "Geometry3.Vector2";
        public override string Operand3Name => "Epsilon";
        public override string OpName => "EpsEqual";
        public override string OpString => "EpsEqual";
        public override bool ComputeOp(ref readonly Vector2d A, ref readonly Vector2d B, ref readonly double C) { return A.EpsilonEqual(B,C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = ({A}).EpsilonEqual({B},{C})"; }
    }


    [NodeFunctionLibrary("Geometry3.Vector2")]
    public static class G3Vector2Functions
    {
        [GraphDataTypeConversion]
        public static Vector2d ConvertScalarToVector2d(double s) {
            return new Vector2d(s);
        }
        [GraphDataTypeConversion]
        public static Vector2d ConvertScalarToVector2f(float f) {
            return new Vector2d((double)f);
        }
        [GraphDataTypeConversion]
        public static Vector2d ConvertScalarToVector2i(int i) {
            return new Vector2d((double)i);
        }
    }
}
