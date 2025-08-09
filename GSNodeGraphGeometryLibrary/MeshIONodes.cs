// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using g3;
using System.IO;

namespace Gradientspace.NodeGraph.Geometry
{
    [NodeFunctionLibrary("Geometry3.MeshIO")]
    [MappedFunctionLibraryName("Geometry.MeshIO")]
    public static class GeometryMeshIOFunctionLibrary
    {


        [NodeFunction]
        [NodeReturnValue(DisplayName = "Mesh")]
        //[NodeParameter("Path", DisplayName = "PathX", DefaultValue ="c:\\scratch\\bunny.obj")]
        public static DMesh3? ImportMesh(string Path = "c:\\scratch\\bunny.obj")
        {
            DMesh3Builder builder = new DMesh3Builder();
            StandardMeshReader reader = new StandardMeshReader() { MeshBuilder = builder };
            IOReadResult result = reader.Read(Path, ReadOptions.Defaults);
            if (result.code == IOCode.Ok && builder.Meshes.Count > 0)
            {
                return builder.Meshes[0];
            }
            return null;
        }


        [NodeFunction]
        [NodeParameter("Path", DisplayName = "OutputPath", DefaultValue ="c:\\scratch\\AA_FROM_GRAPH.obj")]
        public static void ExportMesh(string Path, DMesh3 Mesh)
        {
            IOWriteResult result = StandardMeshWriter.WriteFile(Path,
               new List<WriteMesh>() { new WriteMesh(Mesh) }, WriteOptions.Defaults);
        }

    }
}
