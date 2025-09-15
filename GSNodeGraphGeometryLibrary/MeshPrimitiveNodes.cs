// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using g3;

namespace Gradientspace.NodeGraph.Geometry3
{

    [NodeFunctionLibrary("Geometry3.Primitives")]
    public static class G3MeshPrimitiveFunctions
    {
        [NodeFunction(ReturnName="Mesh")]
        public static DMesh3? CreateSphere(float Radius = 1.0f, int Subdivisions = 4)
        {
            Subdivisions = Math.Clamp(Subdivisions, 0, 100);
            Radius = Math.Clamp(Radius, 0.00001f, float.MaxValue);
            Sphere3Generator_NormalizedCube SphereGen = new Sphere3Generator_NormalizedCube() { Radius = Radius, EdgeVertices = Subdivisions };
            return SphereGen.Generate().MakeDMesh();
        }
    }


}
