// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using g3;

namespace Gradientspace.NodeGraph.Geometry3
{
    [NodeFunctionLibrary("Geometry3.MeshAttributes")]
    public static class Geometry3MeshAttributesFunctionLibrary
    {
        [NodeFunction]
        public static void RecomputeNormals(ref DMesh3 Mesh)
        {
            MeshNormals.QuickCompute(Mesh);
        }
    }

}
