// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using g3;

namespace Gradientspace.NodeGraph.Geometry3
{
    public enum EMeshBooleanOperation
    {
        Union,
        Intersection,
        Difference
    }

    [NodeFunctionLibrary("Geometry3.MeshModeling")]
    public static class Geometry3MeshModelingFunctionLibrary
    {
        [NodeFunction]
        [NodeReturnValue(DisplayName = "Mesh")]
        public static DMesh3? MeshBoolean(DMesh3 Target, DMesh3 Tool, EMeshBooleanOperation Operation)
        {

            // argh this only does union!

            MeshBoolean boolean = new MeshBoolean() { Target = Target, Tool = Tool };
            bool bOK = boolean.Compute();
            return (bOK) ? boolean.Result : null;            
        }



        [NodeFunction]
        public static void ProcessMeshVertices(ref DMesh3 Mesh, Func<Vector3d, Vector3d, Vector3d> DeformFunc )
        {
            DMesh3? UseMesh = Mesh;
            if ( Mesh.HasVertexNormals == false )
            {
                UseMesh = new DMesh3(Mesh, false, false, false, false);
                MeshNormals.QuickCompute(UseMesh);
            }

            foreach (int vid in UseMesh.VertexIndices())
            {
                Vector3d v = UseMesh.GetVertex(vid);
                Vector3d n = UseMesh.GetVertexNormal(vid);
                Vector3d newv = DeformFunc(v, n);
                UseMesh.SetVertex(vid, newv);
            }

            if ( UseMesh != Mesh )
            {
                foreach (int vid in UseMesh.VertexIndices())
                    Mesh.SetVertex(vid, UseMesh.GetVertex(vid));
            }
        }




    }



}
