// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using g3;

namespace Gradientspace.NodeGraph.Geometry
{
    [NodeFunctionLibrary("Geometry3.Quaternion")]
    public static class Geometry3QuaternionFunctionLibrary
    {
        [NodeFunction]
        [NodeReturnValue(DisplayName = "Quaterniond")]
        public static Quaterniond MakeQuat(double x = 0, double y = 0, double z = 0, double w = 1)
        {
            return new Quaterniond(x, y, z, w);
        }
        [NodeFunction]
        public static void BreakQuat(Quaterniond Q, out double x, out double y, out double z, out double w)
        {
            x = Q.x; y = Q.y; z = Q.z; w = Q.w;
        }


        [NodeFunction]
        [NodeReturnValue(DisplayName = "Quaterniond")]
        public static Quaterniond MakeQuatFromAxisAngle(Vector3d Axis, double Angle, bool bIsRadians = false)
        {
            return (!bIsRadians) ? Quaterniond.AxisAngleD(Axis.Normalized, Angle) : Quaterniond.AxisAngleR(Axis.Normalized, Angle);
        }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "Quaterniond")]
        public static Quaterniond MakeQuatFromTo(Vector3d FromAxis, Vector3d ToAxis)
        {
            return Quaterniond.FromTo(FromAxis.Normalized, ToAxis.Normalized);
        }
        [NodeFunction]
        [NodeReturnValue(DisplayName = "Quaterniond")]
        public static Quaterniond MakeQuatFromToConstrained(Vector3d FromAxis, Vector3d ToAxis, Vector3d AroundAxis)
        {
            return Quaterniond.FromToConstrained(FromAxis.Normalized, ToAxis.Normalized, AroundAxis.Normalized);
        }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "Quaterniond")]
        public static Quaterniond MakeQuatFromSlerp(Quaterniond From, Quaterniond To, double Alpha)
        {
            return Quaterniond.Slerp(From, To, Alpha);
        }


        [NodeFunction]
        [NodeReturnValue(DisplayName = "A*B")]
        public static Quaterniond Multiply(Quaterniond A, Quaterniond B) { return A * B; }


        [NodeFunction]
        [NodeReturnValue(DisplayName = "-Q")]
        public static Quaterniond Negate(Quaterniond Q) { return -Q; }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "Inv(Q)")]
        public static Quaterniond Inverse(Quaterniond Q) { return Q.Inverse(); }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "Conj(Q)")]
        public static Quaterniond Conjugate(Quaterniond Q) { return Q.Conjugate(); }


        [NodeFunction]
        [NodeReturnValue(DisplayName = "UnitQ")]
        public static Quaterniond Normalize(Quaterniond Q, out double Length) { Length = Q.Normalize(); return Q; }

        [NodeFunction]
        public static void GetAxes(Quaterniond Q, out Vector3d X, out Vector3d Y, out Vector3d Z) { X = Q.AxisX; Y = Q.AxisY; Z = Q.AxisZ; }  // todo more efficient to make matrix?

    }
}
