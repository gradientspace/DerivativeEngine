// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.Nodes;

namespace Gradientspace.NodeGraph.Geometry
{
    // Normalize (return length)
    // MakePerpVectors
    // Orthonormalize
    // PlaneAngleD, PlaneAngleSignedD
    // TriArea, TriNormal, TriAspectRatio, TriIsObtuse
    // VectorCot, VectorTan
    // BarycentricCoords (?)   ((maybe use triangle type...))


    public class Vector3ZeroNode : StandardMathConstantNode<Vector3d>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "Zero";
        public override string OpString => "Zero";
        public override Vector3d ConstantValue => Vector3d.Zero;
        protected override string CodeString(string Result) { return $"{Result} = Vector3d.Zero"; }
    }
    public class Vector3OneNode : StandardMathConstantNode<Vector3d>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "One";
        public override string OpString => "One";
        public override Vector3d ConstantValue => Vector3d.One;
        protected override string CodeString(string Result) { return $"{Result} = Vector3d.One"; }
    }
    public class Vector3UnitXNode : StandardMathConstantNode<Vector3d>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "UnitX";
        public override string OpString => "UnitX";
        public override Vector3d ConstantValue => Vector3d.AxisX;
        protected override string CodeString(string Result) { return $"{Result} = Vector3d.AxisX"; }
    }
    public class Vector3UnitYNode : StandardMathConstantNode<Vector3d>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "UnitY";
        public override string OpString => "UnitY";
        public override Vector3d ConstantValue => Vector3d.AxisY;
        protected override string CodeString(string Result) { return $"{Result} = Vector3d.AxisY"; }
    }
    public class Vector3UnitZNode : StandardMathConstantNode<Vector3d>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "UnitZ";
        public override string OpString => "UnitZ";
        public override Vector3d ConstantValue => Vector3d.AxisZ;
        protected override string CodeString(string Result) { return $"{Result} = Vector3d.AxisZ"; }
    }


    [GraphNodeUIName("Vector3d")]
    public class Vector3ConstantNode : GenericPODConstantNode<g3.Vector3d>
    {
        public override string? GetNodeNamespace() { return "Geometry.Vector3"; }
        public override string GetDefaultNodeName() { return "Vector3d"; }
    }


    public class Vector3AddNode : StandardBinaryMathOpNode<Vector3d, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "Add";
        public override string OpString => "A + B";
        public override Vector3d ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B) { return A + B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) + ({B})"; }
    }
    public class Vector3SubtractNode : StandardBinaryMathOpNode<Vector3d, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "Subtract";
        public override string OpString => "A - B";
        public override Vector3d ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B) { return A - B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) - ({B})"; }
    }
    public class Vector3MultiplyNode : StandardBinaryMathOpNode<Vector3d, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "Multiply";
        public override string OpString => "A * B";
        public override Vector3d ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B) { return A * B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) * ({B})"; }
    }
    public class Vector3DivideNode : StandardBinaryMathOpNode<Vector3d, Vector3d, Vector3d>
    {
        public override object? Operand2Default => Vector3d.One;
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "Divide";
        public override string OpString => "A / B";
        public override Vector3d ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B) { return A / B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) / ({B})"; }
    }
    public class Vector3NegateNode : StandardUnaryMathOpNode<Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "Negate";
        public override string OpString => "-A";
        public override Vector3d ComputeOp(ref readonly Vector3d A) { return -A; }
        protected override string CodeString(string A, string Result) { return $"{Result} = -({A})"; }
    }

    public class Vector3DotNode : StandardBinaryMathOpNode<Vector3d, Vector3d, double>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "Dot";
        public override string OpString => "A . B";
        public override double ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B) { return A.Dot(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).Dot({B})"; }
    }
    public class Vector3CrossNode : StandardBinaryMathOpNode<Vector3d, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "Cross";
        public override string OpString => "A x B";
        public override Vector3d ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B) { return A.Cross(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).Cross({B})"; }
    }
    public class Vector3UnitCrossNode : StandardBinaryMathOpNode<Vector3d, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "UnitCross";
        public override string OpString => "UnitCross";
        public override Vector3d ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B) { return A.UnitCross(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).UnitCross({B})"; }
    }


    public class Vector3AbsNode : StandardUnaryMathOpNode<Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "Abs";
        public override string OpString => "Abs";
        public override Vector3d ComputeOp(ref readonly Vector3d A) { return A.Abs; }
        protected override string CodeString(string A, string Result) { return $"{Result} = ({A}).Abs"; }
    }
    public class Vector3RoundFracNode : StandardBinaryMathOpNode<Vector3d, int, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string Operand2Name => "Decimals";
        public override string OpName => "Round";
        public override string OpString => "Round";
        public override Vector3d ComputeOp(ref readonly Vector3d A, ref readonly int B) { return A.RoundFrac(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).RoundFrac({B})"; }
    }


    public class Vector3LengthNode : StandardUnaryMathOpNode<Vector3d, double>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "Length";
        public override string OpString => "Length";
        public override double ComputeOp(ref readonly Vector3d A) { return A.Length; }
        protected override string CodeString(string A, string Result) { return $"{Result} = ({A}).Length"; }
    }
    public class Vector3LengthSquaredNode : StandardUnaryMathOpNode<Vector3d, double>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "LengthSqr";
        public override string OpString => "LengthSqr";
        public override double ComputeOp(ref readonly Vector3d A) { return A.LengthSquared; }
        protected override string CodeString(string A, string Result) { return $"{Result} = ({A}).LengthSquared"; }
    }
    public class Vector3LengthL1Node : StandardUnaryMathOpNode<Vector3d, double>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "LengthL1";
        public override string OpString => "LengthL1";
        public override double ComputeOp(ref readonly Vector3d A) { return A.LengthL1; }
        protected override string CodeString(string A, string Result) { return $"{Result} = ({A}).LengthL1"; }
    }
    public class Vector3DistanceNode : StandardBinaryMathOpNode<Vector3d, Vector3d, double>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "Distance";
        public override string OpString => "Distance";
        public override double ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B) { return A.Distance(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).Distance({B})"; }
    }
    public class Vector3DistanceSqrNode : StandardBinaryMathOpNode<Vector3d, Vector3d, double>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "DistanceSqr";
        public override string OpString => "DistanceSqr";
        public override double ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B) { return A.DistanceSquared(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).DistanceSquared({B})"; }
    }



    public class Vector3AngleDegNode : StandardBinaryMathOpNode<Vector3d, Vector3d, double>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "AngleDeg";
        public override string OpString => "AngleDeg";
        public override double ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B) { return A.AngleD(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).AngleD({B})"; }
    }
    public class Vector3AngleRadNode : StandardBinaryMathOpNode<Vector3d, Vector3d, double>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "AngleRad";
        public override string OpString => "AngleRad";
        public override double ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B) { return A.AngleR(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).AngleR({B})"; }
    }
    public class Vector3CotNode : StandardBinaryMathOpNode<Vector3d, Vector3d, double>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "VectorCot";
        public override string OpString => "VectorCot";
        public override double ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B) { return MathUtil.VectorCot(A,B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = MathUtil.VectorCot({A},{B})"; }
    }
    public class Vector3TanNode : StandardBinaryMathOpNode<Vector3d, Vector3d, double>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "VectorTan";
        public override string OpString => "VectorTan";
        public override double ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B) { return MathUtil.VectorTan(A, B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = MathUtil.VectorTan({A},{B})"; }
    }


    public class Vector3LerpNode : StandardTrinaryMathOpNode<Vector3d, Vector3d, double, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string Operand3Name => "Alpha";
        public override string OpName => "Lerp";
        public override string OpString => "Lerp";
        public override Vector3d ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B, ref readonly double C) { return Vector3d.Lerp(A,B,C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = Vector3d.Lerp({A},{B},{C})"; }
    }


    public class Vector3EpsEqualNode : StandardTrinaryMathOpNode<Vector3d, Vector3d, double, bool>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string Operand3Name => "Epsilon";
        public override string OpName => "EpsEqual";
        public override string OpString => "EpsEqual";
        public override bool ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B, ref readonly double C) { return A.EpsilonEqual(B,C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = ({A}).EpsilonEqual({B},{C})"; }
    }



    [NodeFunctionLibrary("Geometry3.Vector3")]
    public static class G3Vector3Functions
    {
        [GraphDataTypeConversion]
        public static Vector3d ConvertScalarToVector3d(double s) {
            return new Vector3d(s);
        }
        [GraphDataTypeConversion]
        public static Vector3d ConvertScalarToVector3f(float f) {
            return new Vector3d((double)f);
        }
        [GraphDataTypeConversion]
        public static Vector3d ConvertScalarToVector3i(int i) {
            return new Vector3d((double)i);
        }


        [NodeFunction]
        [NodeReturnValue(DisplayName = "Vector3d")]
        public static Vector3d MakeVec3(double x = 0, double y = 0, double z = 0)
        {
            return new Vector3d(x, y, z);
        }
        [NodeFunction]
        public static void BreakVec3(Vector3d Vec, out double x, out double y, out double z)
        {
            x = Vec.x; y = Vec.y; z = Vec.z;
        }


        [NodeFunction]
        public static void MakePerpVectors(Vector3d V, out Vector3d T1, out Vector3d T2) { Vector3d.MakePerpVectors(ref V, out T1, out T2); }

    }
}
