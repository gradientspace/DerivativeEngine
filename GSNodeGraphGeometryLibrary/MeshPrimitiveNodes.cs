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




        [NodeFunction]
        public static void AppendSphere(ref DMesh3 Mesh, Frame3d Frame, float Radius = 1.0f, int Subdivisions = 4)
        {
            Subdivisions = Math.Clamp(Subdivisions, 0, 100);
            Radius = Math.Clamp(Radius, 0.00001f, float.MaxValue);
            Sphere3Generator_NormalizedCube SphereGen = new Sphere3Generator_NormalizedCube() { Radius = Radius, EdgeVertices = Subdivisions };
            DMesh3 SphereMesh = SphereGen.Generate().MakeDMesh();
            MeshTransforms.FromFrame(SphereMesh, Frame);
            MeshEditor Editor = new(Mesh);
            Editor.AppendMesh(SphereMesh);
            //Editor.AppendBox(Frame, 5.0);
        }
    }


}
