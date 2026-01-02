// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using g3;
using gs;

namespace Gradientspace.NodeGraph.Geometry3
{
    [NodeFunctionLibrary("Geometry3.MeshModeling")]
    public static class G3MeshRemeshingFunctions
    {
        [NodeFunction]
        public static void RemeshMeshRelative(ref DMesh3 Mesh, double EdgeMultiplier = 1.0)
        {
            RemesherPro remesher = new(Mesh);
            MeshQueries.EdgeLengthStats(Mesh, out double minEdgeLen, out double maxEdgeLen, out double avgEdgeLen);
            EdgeMultiplier = Math.Clamp(EdgeMultiplier, 0.01, 100.0);
            remesher.SetTargetEdgeLength(avgEdgeLen * EdgeMultiplier);
            remesher.FastestRemesh();
            Mesh.CompactInPlace();
        }


        [NodeFunction]
        public static void RemeshMeshToEdgeLen(ref DMesh3 Mesh, double TargetEdgeLen = 1.0)
        {
            RemesherPro remesher = new(Mesh);
            if ( TargetEdgeLen < MathUtil.ZeroTolerance)
                throw new ArgumentException($"RemeshMeshToEdgeLen: TargetEdgeLen {TargetEdgeLen} is too small");
            remesher.SetTargetEdgeLength(TargetEdgeLen);
            remesher.FastestRemesh();
            Mesh.CompactInPlace();
        }

    }
}
