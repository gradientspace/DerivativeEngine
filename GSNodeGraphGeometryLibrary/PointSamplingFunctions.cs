using g3;
using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Geometry
{
    [NodeFunctionLibrary("Geometry3.Sampling")]
    public static class G3SamplingFunctions
    {

        [NodeFunction(ReturnName = "Points")]
        public static Vector3d[] SphericalFibonacciPoints( int N = 32, double Radius = 1.0, Vector3d Origin = default )
        {
            Vector3d[] result = new Vector3d[N];
            SphericalFibonacciPointSet PointSet = new SphericalFibonacciPointSet(N);
            for (int i = 0; i < N; ++i)
                result[i] = PointSet[i];
            return result;
        }

    }


}
