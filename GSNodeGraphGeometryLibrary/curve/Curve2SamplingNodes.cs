// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using gs;

namespace Gradientspace.NodeGraph.Geometry
{
    public enum ECurveParamMode
    {
        Default = 1,
        ArcLength = 2
    }


    [NodeFunctionLibrary("Geometry3.Curve2")]
    public static class G3CurveSampling2dFunctions
    {
        [NodeFunction]
        public static void SampleCurveByCount(ref IParametricCurve2d Curve, out Vector2d[] Points, int NumPoints = 10, ECurveParamMode ParamMode = ECurveParamMode.ArcLength)
        {
            if (Curve.HasArcLength == false) {
                Points = CurveSampler2.SampleT(Curve, NumPoints).AsArray();
                return;
            }

            double DeltaT = 1.0 / (double)(NumPoints - 1);
            double Len = Curve.ArcLength;
            double Spacing = Math.Max(Len * DeltaT, 0.0001);
            Points = CurveSampler2.AutoSample(Curve, Spacing, DeltaT).AsArray();
        }

    }
}
