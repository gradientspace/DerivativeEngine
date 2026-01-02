// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using gs;

namespace Gradientspace.NodeGraph.Geometry
{
    [NodeFunctionLibrary("Geometry3.Curve2")]
    public static class G3CurveCreation2dFunctions
    {

        [NodeFunction]
        public static void CubicBezier2(out BezierCurve2 Curve, Vector2d A, Vector2d B, Vector2d C, Vector2d D)
        {
            Curve = new BezierCurve2(3, [A, B, C, D], true);
        }


        [NodeFunction]
        public static void MultiCubicBezier2(out ParametricCurveSequence2 Curve, IEnumerable<Vector2d> Points, IEnumerable<Vector2d> Tangents)
        {
            if (Points.Count() != Tangents.Count())
                throw new Exception("MultiCubicBezier2: Points and Tangents counts do not match. Currently do not support discontinous points");

            int N = Points.Count()-1;
            BezierCurve2[] beziers = new BezierCurve2[N];
            for ( int i = 0; i < N; ++i ) {
                Vector2d p0 = Points.ElementAt(i);
                Vector2d p1 = p0 + (Tangents.ElementAt(i));
                Vector2d p3 = Points.ElementAt(i+1);
                Vector2d p2 = p3 - (Tangents.ElementAt(i+1));
                beziers[i] = new BezierCurve2(3, [p0, p1, p2, p3], true);
            }

            Curve = new ParametricCurveSequence2(beziers, false);
        }

    }
}
