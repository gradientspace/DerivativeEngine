using g3;
using Gradientspace.NodeGraph.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Geometry
{
    // Inverse()
    // Conjugate()
    // Psuedoinverse
    // affine decomposition  (get rotation / get translation)
    // InverseTransform


    public class Matrix4IdentityNode : StandardMathConstantNode<Matrix4d>
    {
        public override string OpNamespace => "Geometry3.Matrix4";
        public override string OpName => "Identity";
        public override string OpString => "Identity";
        public override Matrix4d ConstantValue => Matrix4d.Identity;
        protected override string CodeString(string Result) { return $"{Result} = Matrix4d.Identity"; }
    }


    public class Matrix4MakeScaleNode : StandardUnaryMathOpNode<Vector3d, Matrix4d>
    {
        public override string OpNamespace => "Geometry3.Matrix4";
        public override string OpName => "MakeScale";
        public override string OpString => "MakeScale";
        public override Matrix4d ComputeOp(ref readonly Vector3d A) { return  Matrix4d.Scale(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Matrix4d.Scale({A});"; }
    }
    public class Matrix4MakeTranslateNode : StandardUnaryMathOpNode<Vector3d, Matrix4d>
    {
        public override string OpNamespace => "Geometry3.Matrix4";
        public override string OpName => "MakeTranslate";
        public override string OpString => "MakeTranslate";
        public override Matrix4d ComputeOp(ref readonly Vector3d A) { return Matrix4d.Translation(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Matrix4d.Translation({A});"; }
    }
    public class Matrix4MakeFromFrameNode : StandardUnaryMathOpNode<Frame3d, Matrix4d>
    {
        public override string OpNamespace => "Geometry3.Matrix4";
        public override string OpName => "MakeFromFrame";
        public override string OpString => "MakeFromFrame";
        public override Matrix4d ComputeOp(ref readonly Frame3d F) { return new Matrix4d(F); }
        protected override string CodeString(string A, string Result) { return $"{Result} = new Matrix4d({A});"; }
    }


    public class Matrix4MultiplyNode : StandardBinaryMathOpNode<Matrix4d, Matrix4d, Matrix4d>
    {
        public override string OpNamespace => "Geometry3.Matrix4";
        public override string OpName => "Multiply";
        public override string OpString => "A * B";
        public override Matrix4d ComputeOp(ref readonly Matrix4d A, ref readonly Matrix4d B) { return A * B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) * ({B})"; }
    }
    public class Matrix4ScalarMultiplyNode : StandardBinaryMathOpNode<Matrix4d, double, Matrix4d>
    {
        public override string OpNamespace => "Geometry3.Matrix4";
        public override string OpName => "Scale";
        public override string OpString => "M * S";
        public override object? Operand2Default => 1.0;
        public override Matrix4d ComputeOp(ref readonly Matrix4d A, ref readonly double B) { return A * B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) * ({B})"; }
    }



    public class Matrix4TransposeNode : StandardUnaryMathOpNode<Matrix4d, Matrix4d>
    {
        public override string OpNamespace => "Geometry3.Matrix4";
        public override string OpName => "Negate";
        public override string OpString => "-A";
        public override Matrix4d ComputeOp(ref readonly Matrix4d A) { return A.Transpose(); }
        protected override string CodeString(string A, string Result) { return $"{Result} = ({A}).Transpose()"; }
    }



    public class Matrix4TransformPointNode : StandardBinaryMathOpNode<Matrix4d, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Matrix4";
        public override string OpName => "Multiply Point";
        public override string OpString => "M * Point";
        public override string Operand2Name => "Point";
        public override Vector3d ComputeOp(ref readonly Matrix4d M, ref readonly Vector3d V) { return M.TransformPointAffine(V); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).TransformPointAffine({B})"; }
    }
    public class Matrix4TransformVectorNode : StandardBinaryMathOpNode<Matrix4d, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Matrix4";
        public override string OpName => "Multiply Vector";
        public override string OpString => "M * Vector";
        public override string Operand2Name => "Vector";
        public override Vector3d ComputeOp(ref readonly Matrix4d M, ref readonly Vector3d V) { return M.TransformVectorAffine(V); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).TransformVectorAffine({B})"; }
    }


    public class Matrix4EpsEqualNode : StandardTrinaryMathOpNode<Matrix4d, Matrix4d, double, bool>
    {
        public override string OpNamespace => "Geometry3.Matrix4";
        public override string Operand3Name => "Epsilon";
        public override string OpName => "EpsEqual";
        public override string OpString => "EpsEqual";
        public override bool ComputeOp(ref readonly Matrix4d A, ref readonly Matrix4d B, ref readonly double C) { return A.EpsilonEqual(B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = ({A}).EpsilonEqual({B},{C})"; }
    }

}
