// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.Nodes;

namespace Gradientspace.NodeGraph.Geometry
{

    public class QuatZeroNode : StandardMathConstantNode<Quaterniond>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "Identity";
        public override string OpString => "Identity";
        public override Quaterniond ConstantValue => Quaterniond.Identity;
        protected override string CodeString(string Result) { return $"{Result} = Quaterniond.Identity"; }
    }

    public class QuatMakeNode : StandardBinaryMathOpNode<Vector3d, double, Quaterniond>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "MakeQuat";
        public override string OpString => "MakeQuat";
        public override string Operand1Name => "XYZ";
        public override string Operand2Name => "W";
        public override string ValueOutputName => "Quat";
        public override Quaterniond ComputeOp(ref readonly Vector3d A, ref readonly double B) { return new Quaterniond(A.x,A.y,A.z,B).Normalized; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = new Quaterniond(({A}).x,(({A}).y,(({A}).z,({B})).Normalized"; }
    }
    public class QuatAxisAngleNode : StandardBinaryMathOpNode<Vector3d, double, Quaterniond>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "AxisAngle";
        public override string OpString => "AxisAngle";
        public override string Operand1Name => "Axis";
        public override string Operand2Name => "Degrees";
        public override string ValueOutputName => "Quat";
        public override Quaterniond ComputeOp(ref readonly Vector3d A, ref readonly double B) { return Quaterniond.AxisAngleD(A, B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} =  Quaterniond.AxisAngleD({A},{B})"; }
    }
    public class QuatFromToNode : StandardBinaryMathOpNode<Vector3d, Vector3d, Quaterniond>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "FromTo";
        public override string OpString => "FromTo";
        public override string Operand1Name => "AxisA";
        public override string Operand2Name => "AxisB";
        public override string ValueOutputName => "Quat";
        public override Quaterniond ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B) { return Quaterniond.FromTo(A,B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} =  Quaterniond.FromTo({A},{B})"; }
    }
    public class QuatFromToConstrainedNode : StandardTrinaryMathOpNode<Vector3d, Vector3d, Vector3d, Quaterniond>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "FromToAround";
        public override string OpString => "FromToAround";
        public override string Operand1Name => "AxisA";
        public override string Operand2Name => "AxisB";
        public override string Operand3Name => "AroundAxis";
        public override string ValueOutputName => "Quat";
        public override Quaterniond ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B, ref readonly Vector3d C) { return Quaterniond.FromToConstrained(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} =  Quaterniond.FromTo({A},{B},{C})"; }
    }
    public class QuatSlerpNode : StandardTrinaryMathOpNode<Quaterniond, Quaterniond, double, Quaterniond>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "Slerp";
        public override string OpString => "Slerp";
        public override string Operand1Name => "QuatA";
        public override string Operand2Name => "QuatB";
        public override string Operand3Name => "T";
        public override string ValueOutputName => "Quat";
        public override Quaterniond ComputeOp(ref readonly Quaterniond A, ref readonly Quaterniond B, ref readonly double C) { return Quaterniond.Slerp(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} =  Quaterniond.Slerp({A},{B},{C})"; }
    }


    public class QuatMultiplyNode : StandardBinaryMathOpNode<Quaterniond, Quaterniond, Quaterniond>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "Multiply";
        public override string OpString => "A * B";
        public override Quaterniond ComputeOp(ref readonly Quaterniond A, ref readonly Quaterniond B) { return A * B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) * ({B})"; }
    }
    public class QuatNegateNode : StandardUnaryMathOpNode<Quaterniond, Quaterniond>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "Negate";
        public override string OpString => "-A";
        public override Quaterniond ComputeOp(ref readonly Quaterniond A) { return -A; }
        protected override string CodeString(string A, string Result) { return $"{Result} = -({A})"; }
    }


    public class QuatInverseNode : StandardUnaryMathOpNode<Quaterniond, Quaterniond>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "Inverse";
        public override string OpString => "Inverse";
        public override Quaterniond ComputeOp(ref readonly Quaterniond A) { return A.Inverse(); }
        protected override string CodeString(string A, string Result) { return $"{Result} = ({A}).Inverse()"; }
    }
    public class QuatConjugateNode : StandardUnaryMathOpNode<Quaterniond, Quaterniond>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "Conjugate";
        public override string OpString => "Conjugate";
        public override Quaterniond ComputeOp(ref readonly Quaterniond A) { return A.Conjugate(); }
        protected override string CodeString(string A, string Result) { return $"{Result} = ({A}).Conjugate()"; }
    }
    public class QuatNormalizeNode : StandardUnaryMathOpNode2<Quaterniond, Quaterniond, double>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "Normalize";
        public override string Operand1Name => "Q";
        public override string Output1Name => "UnitQ";
        public override string Output2Name => "Length";
        public override string OpString => "Normalize";
        public override (Quaterniond, double) ComputeOp(ref readonly Quaterniond A) { Quaterniond tmp = A; double len = tmp.Normalize(); return (tmp, len); }
        protected override string CodeString(string A, string Result1, string Result2) { return $"{{Quaterniond tmp = {A}; {Result2} = tmp.Normalize(); {Result1}=tmp;}}"; }
    }


    public class BreakQuatNode : StandardUnaryMathOpNode2<Quaterniond, Vector3d, double>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "BreakQuat";
        public override string Operand1Name => "Q";
        public override string Output1Name => "XYZ";
        public override string Output2Name => "W";
        public override string OpString => "BreakQuat";
        public override (Vector3d, double) ComputeOp(ref readonly Quaterniond A) { return (new Vector3d(A.x,A.y,A.z), A.w); }
        protected override string CodeString(string A, string Result1, string Result2) { return $"{Result1}=new Vector3d(({A}).x,({A}).y,({A}).z); {Result2}=({A}).w;"; }
    }
    public class QuatGetAxesNode : StandardUnaryMathOpNode3<Quaterniond, Vector3d, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "GetAxes";
        public override string Operand1Name => "Q";
        public override string Output1Name => "AxisX";
        public override string Output2Name => "AxisY";
        public override string Output3Name => "AxisZ";
        public override string OpString => "GetAxes";
        public override (Vector3d, Vector3d, Vector3d) ComputeOp(ref readonly Quaterniond A) { return (A.AxisX, A.AxisY, A.AxisZ); }
        protected override string CodeString(string A, string Result1, string Result2, string Result3) { return $"{Result1}=({A}).AxisX; {Result2}=({A}).AxisY; {Result3}=({A}).AxisZ;"; }
    }



    public class QuatMultiplyVectorNode : StandardBinaryMathOpNode<Quaterniond, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "Multiply";
        public override string OpString => "Q * V";
        public override Vector3d ComputeOp(ref readonly Quaterniond Q, ref readonly Vector3d V) { return Q * V; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) * ({B})"; }
    }
    public class QuatInvMultiplyVectorNode : StandardBinaryMathOpNode<Quaterniond, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string OpName => "InverseMultiply";
        public override string OpString => "Inv(Q) * V";
        public override Vector3d ComputeOp(ref readonly Quaterniond Q, ref readonly Vector3d V) { return Q.InverseMultiply(in V); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).InverseMultiply({B})"; }
    }


    public class QuatEpsEqualNode : StandardTrinaryMathOpNode<Quaterniond, Quaterniond, double, bool>
    {
        public override string OpNamespace => "Geometry3.Quaternion";
        public override string Operand3Name => "Epsilon";
        public override string OpName => "EpsEqual";
        public override string OpString => "EpsEqual";
        public override bool ComputeOp(ref readonly Quaterniond A, ref readonly Quaterniond B, ref readonly double C) { return A.EpsilonEqual(B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = ({A}).EpsilonEqual({B},{C})"; }
    }

}
