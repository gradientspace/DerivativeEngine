// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using g3;

namespace Gradientspace.NodeGraph.Geometry
{
    [NodeFunctionLibrary("Geometry3.Vector3")]
    public static class Geometry3Vector3FunctionLibrary
    {
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

        [NodeFunction] [NodeReturnValue(DisplayName = "A+B")]
        public static Vector3d Add(Vector3d A, Vector3d B) { return A + B; }

        [NodeFunction] [NodeReturnValue(DisplayName = "A-B")]
        public static Vector3d Subtract(Vector3d A, Vector3d B) { return A - B; }

        [NodeFunction] [NodeReturnValue(DisplayName = "A*B")]
        public static Vector3d Multiply(Vector3d A, Vector3d B) { return A*B; }

        [NodeFunction] [NodeReturnValue(DisplayName = "A/B")]
        public static Vector3d Divide(Vector3d A, Vector3d B) { return A / B; }

        [NodeFunction] [NodeReturnValue(DisplayName = "-A")]
        public static Vector3d Negate(Vector3d A) { return -A; }

        [NodeFunction] [NodeReturnValue(DisplayName = "A*c")]
        public static Vector3d Scale(Vector3d A, double c) { return A * c; }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "A/c")]
        public static Vector3d InvScale(Vector3d A, double c) { return A / c; }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "A.B")]
        public static double Dot(Vector3d A, Vector3d B) { return A.Dot(B); }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "AxB")]
        public static Vector3d Cross(Vector3d A, Vector3d B) { return A.Cross(B); }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "AxB")]
        public static Vector3d UnitCross(Vector3d A, Vector3d B) { return A.UnitCross(B); }


        [NodeFunction]
        [NodeReturnValue(DisplayName = "UnitVec")]
        public static Vector3d Normalize(Vector3d V, out double Length) { Length = V.Normalize(); return V; }


        [NodeFunction]
        [NodeReturnValue(DisplayName = "Len")]
        public static double Length(Vector3d A) { return A.Length; }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "LenSqr")]
        public static double LengthSqr(Vector3d A) { return A.LengthSquared; }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "Dist")]
        public static double Distance(Vector3d A, Vector3d B) { return A.Distance(B); }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "DistSqr")]
        public static double DistanceSqr(Vector3d A, Vector3d B) { return A.DistanceSquared(B); }



        [NodeFunction]
        [NodeReturnValue(DisplayName = "Degrees")]
        public static double AngleDeg(Vector3d A, Vector3d B) { return A.AngleD(B); }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "Radians")]
        public static double AngleRad(Vector3d A, Vector3d B) { return A.AngleR(B); }



        [NodeFunction]
        [NodeReturnValue(DisplayName = "Vector3d")]
        public static Vector3d Lerp(Vector3d A, Vector3d B, double Alpha) { return Vector3d.Lerp(A, B, Math.Clamp(Alpha, 0.0, 1.0)); }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "Vector3d")]
        public static Vector3d Round(Vector3d V, int NumDecimals = 2) { V.Round(NumDecimals); return V; }

        [NodeFunction]
        public static void MakePerpVectors(Vector3d V, out Vector3d T1, out Vector3d T2) { Vector3d.MakePerpVectors(ref V, out T1, out T2); }


    }
}
