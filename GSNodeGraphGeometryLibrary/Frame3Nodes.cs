using g3;
using Gradientspace.NodeGraph.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Geometry
{
    // ray-plane intersection (must be a func - return bool, point, dist)



    public class Frame3IdentityNode : StandardMathConstantNode<Frame3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "Identity";
        public override string OpString => "Identity";
        public override Frame3d ConstantValue => Frame3d.Identity;
        protected override string CodeString(string Result) { return $"{Result} = Frame3d.Identity"; }
    }


    public class Frame3FromAxisNode : StandardTrinaryMathOpNode<Vector3d, Vector3d, int, Frame3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "FrameFromAxis";
        public override string OpString => "FrameFromAxis";
        public override string Operand1Name => "Origin";
        public override string Operand2Name => "Axis";
        public override string Operand3Name => "Index";
        public override string ValueOutputName => "Frame";
        public override object? Operand2Default => Vector3d.UnitZ;
        public override object? Operand3Default => 2;
        public override Frame3d ComputeOp(ref readonly Vector3d A, ref readonly Vector3d B, ref readonly int C) { return new Frame3d(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = new Frame3d({A}, {B}, {C})"; }
    }
    public class Frame3MakeNode : StandardBinaryMathOpNode<Vector3d, Quaterniond, Frame3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "MakeFrame";
        public override string OpString => "MakeFrame";
        public override string Operand1Name => "Origin";
        public override string Operand2Name => "Rotation";
        public override string ValueOutputName => "Frame";
        public override Frame3d ComputeOp(ref readonly Vector3d A, ref readonly Quaterniond B) { return new Frame3d(A, B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = new Frame3d({A}, {B})"; }
    }



    public class Frame3DecomposeNode : StandardUnaryMathOpNode2<Frame3d, Vector3d, Quaterniond>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "SplitFrame";
        public override string Operand1Name => "Frame";
        public override string Output1Name => "Origin";
        public override string Output2Name => "Rotation";
        public override string OpString => "Normalize";
        public override (Vector3d, Quaterniond) ComputeOp(ref readonly Frame3d A) { return (A.Origin, A.Rotation); }
        protected override string CodeString(string A, string Result1, string Result2) { return $"{{ {Result1} = {A}.Origin; {Result2} = {A}.Rotation; }}"; }
    }
    public class Frame3AxesNode : StandardUnaryMathOpNode3<Frame3d, Vector3d, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Vector3";
        public override string OpName => "GetXYZ";
        public override string Operand1Name => "Frame";
        public override string Output1Name => "X";
        public override string Output2Name => "Y";
        public override string Output3Name => "Z";
        public override string OpString => "GetXYZ";
        public override (Vector3d, Vector3d, Vector3d) ComputeOp(ref readonly Frame3d A) { return (A.X, A.Y, A.Z); }
        protected override string CodeString(string A, string Result1, string Result2, string Result3) { return $"{A}.GetXYZ(out {Result1}, out {Result2}, out {Result3})"; }
    }
    public class Frame3InterpolateNode : StandardTrinaryMathOpNode<Frame3d, Frame3d, double, Frame3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string Operand3Name => "T";
        public override string OpName => "Interpolate";
        public override string OpString => "Interpolate";
        public override Frame3d ComputeOp(ref readonly Frame3d A, ref readonly Frame3d B, ref readonly double C) { return Frame3d.Interpolate(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = Frame3d.Interpolate({A}, {B}, {C})"; }
    }




    public class Frame3TranslateNode : StandardBinaryMathOpNode<Frame3d, Vector3d, Frame3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "Translated";
        public override string OpString => "Translated";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "Translation";
        public override string ValueOutputName => "NewFrame";
        public override Frame3d ComputeOp(ref readonly Frame3d A, ref readonly Vector3d B) { return A.Translated(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).Translated({B})"; }
    }
    public class Frame3AxisTranslateNode : StandardTrinaryMathOpNode<Frame3d, double, int, Frame3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "AxisTranslated";
        public override string OpString => "AxisTranslated";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "Distance";
        public override string Operand3Name => "Axis";
        public override object? Operand3Default => 2;
        public override string ValueOutputName => "NewFrame";
        public override Frame3d ComputeOp(ref readonly Frame3d A, ref readonly double Dist, ref readonly int Axis) { return A.Translated(Dist, Axis); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = ({A}).Translated({B}, {C})"; }
    }


    public class Frame3RotateNode : StandardBinaryMathOpNode<Frame3d, Quaterniond, Frame3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "Rotated";
        public override string OpString => "Rotated";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "Rotation";
        public override string ValueOutputName => "NewFrame";
        public override Frame3d ComputeOp(ref readonly Frame3d A, ref readonly Quaterniond B) { return A.Rotated(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}).Rotated({B})"; }
    }
    public class Frame3AxisRotateNode : StandardTrinaryMathOpNode<Frame3d, double, int, Frame3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "AxisRotated";
        public override string OpString => "AxisRotated";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "AngleDeg";
        public override string Operand3Name => "Axis";
        public override string ValueOutputName => "NewFrame";
        public override object? Operand3Default => 2;
        public override Frame3d ComputeOp(ref readonly Frame3d A, ref readonly double AngleDeg, ref readonly int Axis) { return A.Rotated(AngleDeg, Axis); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = ({A}).Rotated({B}, {C})"; }
    }
    public class Frame3RotateAroundNode : StandardTrinaryMathOpNode<Frame3d, Quaterniond, Vector3d, Frame3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "RotatedAround";
        public override string OpString => "RotatedAround";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "Rotation";
        public override string Operand3Name => "RotOrigin";
        public override string ValueOutputName => "NewFrame";
        public override Frame3d ComputeOp(ref readonly Frame3d A, ref readonly Quaterniond Rotation, ref readonly Vector3d Origin) { return A.RotatedAround(Origin, Rotation); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = ({A}).RotatedAround({C}, {B})"; }
    }



    public class Frame3AlignAxisNode : StandardTrinaryMathOpNode<Frame3d, int, Vector3d, Frame3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "AlignAxis";
        public override string OpString => "AlignAxis";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "Axis";
        public override string Operand3Name => "Direction";
        public override string ValueOutputName => "NewFrame";
        public override object? Operand2Default => 2;
        public override object? Operand3Default => Vector3d.UnitZ;
        public override Frame3d ComputeOp(ref readonly Frame3d A, ref readonly int AxisIdx, ref readonly Vector3d ToDir) { return A.GetAlignAxis(AxisIdx, ToDir); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = ({A}).GetAlignAxis({B}, {C})"; }
    }
    public class Frame3ConstrainedAlignAxisNode : StandardFourArgMathOpNode<Frame3d, int, Vector3d, Vector3d, Frame3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "AlignAxisAround";
        public override string OpString => "AlignAxisAround";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "Axis";
        public override string Operand3Name => "Direction";
        public override string Operand4Name => "Around";
        public override object? Operand2Default => 2;
        public override object? Operand3Default => Vector3d.UnitZ;
        public override object? Operand4Default => Vector3d.UnitX;
        public override string ValueOutputName => "NewFrame";
        public override Frame3d ComputeOp(ref readonly Frame3d A, ref readonly int AxisIdx, ref readonly Vector3d ToDir, ref readonly Vector3d AroundDir) { return A.GetAlignAxisConstrained(AxisIdx, ToDir, AroundDir); }
        protected override string CodeString(string A, string B, string C, string D, string Result) { return $"{Result} = ({A}).GetAlignAxisConstrained({B}, {C}, {D})"; }
    }


    public class Frame3PointToFrameNode : StandardBinaryMathOpNode<Frame3d, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "ToFrameP";
        public override string OpString => "ToFrameP";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "WorldP";
        public override string ValueOutputName => "LocalP";
        public override Vector3d ComputeOp(ref readonly Frame3d A, ref readonly Vector3d B) { return A.ToFrameP(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = {A}.ToFrameP({B})"; }
    }
    public class Frame3PointFromFrameNode : StandardBinaryMathOpNode<Frame3d, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "FromFrameP";
        public override string OpString => "FromFrameP";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "LocalP";
        public override string ValueOutputName => "WorldP";
        public override Vector3d ComputeOp(ref readonly Frame3d A, ref readonly Vector3d B) { return A.FromFrameP(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = {A}.FromFrameP({B})"; }
    }


    public class Frame3VecToFrameNode : StandardBinaryMathOpNode<Frame3d, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "ToFrameV";
        public override string OpString => "ToFrameV";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "WorldV";
        public override string ValueOutputName => "LocalV";
        public override Vector3d ComputeOp(ref readonly Frame3d A, ref readonly Vector3d B) { return A.ToFrameV(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = {A}.ToFrameV({B})"; }
    }
    public class Frame3VecFromFrameNode : StandardBinaryMathOpNode<Frame3d, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "FromFrameV";
        public override string OpString => "FromFrameV";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "LocalV";
        public override string ValueOutputName => "WorldV";
        public override Vector3d ComputeOp(ref readonly Frame3d A, ref readonly Vector3d B) { return A.FromFrameV(B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = {A}.FromFrameV({B})"; }
    }



    public class Frame3QuatToFrameNode : StandardBinaryMathOpNode<Frame3d, Quaterniond, Quaterniond>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "ToFrameQ";
        public override string OpString => "ToFrameQ";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "WorldQ";
        public override string ValueOutputName => "LocalQ";
        public override Quaterniond ComputeOp(ref readonly Frame3d A, ref readonly Quaterniond B) { return A.ToFrame(in B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = {A}.ToFrame(in {B})"; }
    }
    public class Frame3QuatFromFrameNode : StandardBinaryMathOpNode<Frame3d, Quaterniond, Quaterniond>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "FromFrameQ";
        public override string OpString => "FromFrameQ";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "LocalQ";
        public override string ValueOutputName => "WorldQ";
        public override Quaterniond ComputeOp(ref readonly Frame3d A, ref readonly Quaterniond B) { return A.FromFrame(in B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = {A}.FromFrame(in {B})"; }
    }


    public class Frame3ToFrameNode : StandardBinaryMathOpNode<Frame3d, Frame3d, Frame3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "ToFrameF";
        public override string OpString => "ToFrameF";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "WorldF";
        public override string ValueOutputName => "LocalF";
        public override Frame3d ComputeOp(ref readonly Frame3d A, ref readonly Frame3d B) { return A.ToFrame(in B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = {A}.ToFrame(in {B})"; }
    }
    public class Frame3FromFrameNode : StandardBinaryMathOpNode<Frame3d, Frame3d, Frame3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "FromFrameF";
        public override string OpString => "FromFrameF";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "LocalF";
        public override string ValueOutputName => "WorldF";
        public override Frame3d ComputeOp(ref readonly Frame3d A, ref readonly Frame3d B) { return A.FromFrame(in B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = {A}.FromFrame(in {B})"; }
    }


    public class Frame3RayToFrameNode : StandardBinaryMathOpNode<Frame3d, Ray3d, Ray3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "ToFrameRay";
        public override string OpString => "ToFrameRay";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "WorldRay";
        public override string ValueOutputName => "LocalRay";
        public override Ray3d ComputeOp(ref readonly Frame3d A, ref readonly Ray3d B) { return A.ToFrame(in B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = {A}.ToFrame(in {B})"; }
    }
    public class Frame3RayFromFrameNode : StandardBinaryMathOpNode<Frame3d, Ray3d, Ray3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "FromFrameRay";
        public override string OpString => "FromFrameRay";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "LocalRay";
        public override string ValueOutputName => "WorldRay";
        public override Ray3d ComputeOp(ref readonly Frame3d A, ref readonly Ray3d B) { return A.FromFrame(in B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = {A}.FromFrame(in {B})"; }
    }



    public class Frame3BoxToFrameNode : StandardBinaryMathOpNode<Frame3d, Box3d, Box3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "ToFrameBox";
        public override string OpString => "ToFrameBox";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "WorldBox";
        public override string ValueOutputName => "LocalBox";
        public override Box3d ComputeOp(ref readonly Frame3d A, ref readonly Box3d B) { return A.ToFrame(in B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = {A}.ToFrame(in {B})"; }
    }
    public class Frame3BoxFromFrameNode : StandardBinaryMathOpNode<Frame3d, Box3d, Box3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "FromFrameBox";
        public override string OpString => "FromFrameBox";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "LocalBox";
        public override string ValueOutputName => "WorldBox";
        public override Box3d ComputeOp(ref readonly Frame3d A, ref readonly Box3d B) { return A.FromFrame(in B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = {A}.FromFrame(in {B})"; }
    }



    public class Frame3ProjectToPlaneNode : StandardTrinaryMathOpNode<Frame3d, Vector3d, int, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "ToPlane3D";
        public override string OpString => "ToPlane3D";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "Point";
        public override string Operand3Name => "Axis";
        public override string ValueOutputName => "Position";
        public override object? Operand3Default => 2;
        public override Vector3d ComputeOp(ref readonly Frame3d A, ref readonly Vector3d Point, ref readonly int AxisIdx) { return A.ProjectToPlane(Point, AxisIdx); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = ({A}).ProjectToPlane({B}, {C})"; }
    }
    public class Frame3ToPlaneUVNode : StandardTrinaryMathOpNode<Frame3d, Vector3d, int, Vector2d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "ToPlaneUV";
        public override string OpString => "ToPlaneUV";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "Point";
        public override string Operand3Name => "Axis";
        public override string ValueOutputName => "UV";
        public override object? Operand3Default => 2;
        public override Vector2d ComputeOp(ref readonly Frame3d A, ref readonly Vector3d Point, ref readonly int AxisIdx) { return A.ToPlaneUV(Point, AxisIdx); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = ({A}).ToPlaneUV({B}, {C})"; }
    }
    public class Frame3FromPlaneUVNode : StandardTrinaryMathOpNode<Frame3d, Vector2d, int, Vector3d>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "FromPlaneUV";
        public override string OpString => "FromPlaneUV";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "UV";
        public override string Operand3Name => "Axis";
        public override string ValueOutputName => "Point";
        public override object? Operand3Default => 2;
        public override Vector3d ComputeOp(ref readonly Frame3d A, ref readonly Vector2d UV, ref readonly int AxisIdx) { return A.FromPlaneUV(UV, AxisIdx); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = ({A}).FromPlaneUV({B}, {C})"; }
    }



    public class Frame3SignedDistNode : StandardTrinaryMathOpNode<Frame3d, Vector3d, int, double>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string OpName => "SDistToPlane";
        public override string OpString => "SDistToPlane";
        public override string Operand1Name => "Frame";
        public override string Operand2Name => "Point";
        public override string Operand3Name => "PlaneAxis";
        public override string ValueOutputName => "SignedDist";
        public override object? Operand3Default => 2;
        public override double ComputeOp(ref readonly Frame3d A, ref readonly Vector3d Point, ref readonly int AxisIdx) { return A.DistanceToPlaneSigned(Point, AxisIdx); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = ({A}).DistanceToPlaneSigned({B}, {C})"; }
    }


    public class Frame3EpsEqualNode : StandardTrinaryMathOpNode<Frame3d, Frame3d, double, bool>
    {
        public override string OpNamespace => "Geometry3.Frame3";
        public override string Operand3Name => "Epsilon";
        public override string OpName => "EpsEqual";
        public override string OpString => "EpsEqual";
        public override bool ComputeOp(ref readonly Frame3d A, ref readonly Frame3d B, ref readonly double C) { return A.EpsilonEqual(B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = ({A}).EpsilonEqual({B},{C})"; }
    }

}
