// Copyright Gradientspace Corp. All Rights Reserved.
using g3;

namespace Gradientspace.NodeGraph.Geometry
{
    [NodeFunctionLibrary("Geometry3.Implicit3")]
    public static class G3ImplicitPrimitive3dFunctions
    {
        [NodeFunction]
        public static void ImplicitSphere3d(out ImplicitSphere3d Sphere, Vector3d Center, double Radius = 1.0)
        {
            Sphere = new ImplicitSphere3d() { Origin = Center, Radius = Radius };
        }

        [NodeFunction]
        public static void ImplicitLine3d(out ImplicitLine3d Line, Vector3d A, Vector3d B, double Radius = 1.0)
        {
            Line = new ImplicitLine3d() { Segment = new Segment3d(A, B), Radius = Radius };
        }


        [NodeFunction]
        public static void ImplicitUnion3d(out ImplicitNaryUnion3d Union, IEnumerable<BoundedImplicitFunction3d> Fields)
        {
            Union = new ImplicitNaryUnion3d();
            Union.Children = Fields.ToList();
        }

        [NodeFunction]
        public static void ImplicitDifference3d(out ImplicitNaryDifference3d Difference, BoundedImplicitFunction3d Field, IEnumerable<BoundedImplicitFunction3d> Subtract)
        {
            Difference = new ImplicitNaryDifference3d();
            Difference.A = Field;
            Difference.BSet = Subtract.ToList();
        }

    }

}
