// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using g3;

namespace Gradientspace.NodeGraph.Geometry
{
    [NodeFunctionLibrary("Geometry3.Remeshing")]
    public static class GeometryRemeshingFunctionLibrary
    {
        [NodeFunction]
        [NodeReturnValue(DisplayName = "Mesh")]
        [MappedNodeFunctionName("Geometry.Remeshing.ReduceMeshToTriangleCount")]
        public static DMesh3? SimplifyToTriangleCount(DMesh3 Mesh, int TriangleCount=1000)
        {
            Reducer r = new Reducer(Mesh);
            r.ReduceToTriangleCount(TriangleCount);
            return Mesh;
        }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "Mesh")]
        public static DMesh3? SimplifyToVertexCount(DMesh3 Mesh, int VertexCount = 1000)
        {
            Reducer r = new Reducer(Mesh);
            r.ReduceToVertexCount(VertexCount);
            return Mesh;
        }


    }
}
