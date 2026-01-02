// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using g3;

namespace Gradientspace.NodeGraph.Geometry3
{
    [NodeFunctionLibrary("Geometry3.MeshModeling")]
    public static class G3MeshSmoothingFunctions
    {
        [NodeFunction]
        public static void SmoothMeshLaplacian(ref DMesh3 Mesh, double SoftWeight = 1.0)
        {
            LaplacianMeshSmoother smoother = new(Mesh);
            if (SoftWeight > 0) {
                foreach (int vid in Mesh.VertexIndices())
                    smoother.SetConstraint(vid, Mesh.GetVertex(vid), SoftWeight);
            }
            smoother.SolveAndUpdateMesh();
        }



        public enum EIterativeMeshSmoothingType
        {
            Uniform = 0,
            Cotan = 1,
            MeanValue = 2
        }


        [NodeFunction]
        public static void SmoothMeshIterative(ref DMesh3 Mesh,
            EIterativeMeshSmoothingType Type = EIterativeMeshSmoothingType.Uniform,
            double Alpha = 0.2, int Iterations = 5)
        {
            int[] vertices = [.. Mesh.VertexIndices()];
            MeshIterativeSmooth smoother = new MeshIterativeSmooth(Mesh, vertices, true);
            smoother.SmoothType = (MeshIterativeSmooth.SmoothTypes)(int)Type;
            smoother.Alpha = Math.Clamp(Alpha, 0, 1);
            smoother.Rounds = Math.Clamp(Iterations, 0, 1000);
            smoother.Smooth();
        }

    }
}