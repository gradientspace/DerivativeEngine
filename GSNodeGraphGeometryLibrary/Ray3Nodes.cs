// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph.Nodes;

namespace GSNodeGraphGeometryLibrary
{
    public class Ray3ConstructNode : StandardBinaryMathOpNode<Vector3d, Vector3d, Ray3d>
    {
        public override string OpNamespace => "Geometry3.Ray3";
        public override string Operand1Name => "Origin";
        public override string Operand2Name => "Direction";
        public override string OpName => "Ray3";
        public override string OpString => "Ray3";
        public override Ray3d ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B) { return new Ray3d(A,B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = new Ray3d({A},{B})"; }
    }


    public class Ray3PointAtNode : StandardBinaryMathOpNode<Ray3d, double, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Ray3";
        public override string Operand1Name => "Ray";
        public override string Operand2Name => "Distance";
        public override string OpName => "PointAt";
        public override string OpString => "PointAt";
        public override Vector3d ComputeOp(ref readonly Ray3d A, ref readonly double B) { return A.PointAt(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).PointAt({B})"; }
    }


    public class Ray3ProjectNode : StandardBinaryMathOpNode<Ray3d, Vector3d, double>
    {
        public override string OpNamespace => "Geometry3.Ray3";
        public override string Operand1Name => "Ray";
        public override string Operand2Name => "Point";
        public override string OpName => "Project";
        public override string OpString => "Project";
        public override double ComputeOp(ref readonly Ray3d A, ref readonly Vector3d B) { return A.Project(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).Project({B})"; }
    }


    public class Ray3DistanceNode : StandardBinaryMathOpNode<Ray3d, Vector3d, double>
    {
        public override string OpNamespace => "Geometry3.Ray3";
        public override string Operand1Name => "Ray";
        public override string Operand2Name => "Point";
        public override string OpName => "Distance";
        public override string OpString => "Distance";
        public override double ComputeOp(ref readonly Ray3d A, ref readonly Vector3d B) { return A.Distance(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).Distance({B})"; }
    }
    public class Ray3DistanceSqrNode : StandardBinaryMathOpNode<Ray3d, Vector3d, double>
    {
        public override string OpNamespace => "Geometry3.Ray3";
        public override string Operand1Name => "Ray";
        public override string Operand2Name => "Point";
        public override string OpName => "DistanceSqr";
        public override string OpString => "DistanceSqr";
        public override double ComputeOp(ref readonly Ray3d A, ref readonly Vector3d B) { return A.DistanceSquared(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).DistanceSquared({B})"; }
    }

    public class Ray3ClosestPointNode : StandardBinaryMathOpNode<Ray3d, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Ray3";
        public override string Operand1Name => "Ray";
        public override string Operand2Name => "Point";
        public override string OpName => "ClosestPoint";
        public override string OpString => "ClosestPoint";
        public override Vector3d ComputeOp(ref readonly Ray3d A, ref readonly Vector3d B) { return A.ClosestPoint(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).ClosestPoint({B})"; }
    }


}
