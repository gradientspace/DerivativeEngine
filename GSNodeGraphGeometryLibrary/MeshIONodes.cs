// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using g3;
using System.IO;

namespace Gradientspace.NodeGraph.Geometry
{
    [NodeFunctionLibrary("Geometry3.MeshIO")]
    [MappedFunctionLibraryName("Geometry.MeshIO")]
    public static class G3MeshIOFunctions
    {


        [NodeFunction]
        [NodeReturnValue(DisplayName = "Mesh")]
        //[NodeParameter("Path", DisplayName = "PathX", DefaultValue ="c:\\scratch\\bunny.obj")]
        public static DMesh3? ImportMesh(string Path = "c:\\scratch\\bunny.obj")
        {
            DMesh3Builder builder = new DMesh3Builder();
            StandardMeshReader reader = new StandardMeshReader() { MeshBuilder = builder };
            ReadOptions options = ReadOptions.Defaults;
            options.BaseFilePath = System.IO.Path.GetDirectoryName(Path);
            IOReadResult result = reader.Read(Path, ReadOptions.Defaults);
            if (result.code == IOCode.Ok && builder.Meshes.Count > 0)
            {
                DMesh3 Result = builder.Meshes[0];
                for (int i = 1; i < builder.Meshes.Count; ++i)
                    MeshEditor.AppendMesh(Result, builder.Meshes[i]);
                return Result;
            }
            return null;
        }


        [NodeFunction]
        [NodeParameter("Path", DisplayName = "OutputPath", DefaultValue ="c:\\scratch\\AA_FROM_GRAPH.obj")]
        public static void ExportMesh(string Path, DMesh3 Mesh)
        {
            if (Mesh == null) {
                GlobalGraphOutput.AppendError("[ExportMesh] Mesh is null");
                return;
            }
            WriteOptions writeOptions = WriteOptions.Defaults;
            writeOptions.bPerVertexNormals = true;
            writeOptions.bPerVertexUVs = true;
            writeOptions.bPerVertexColors = true;
            writeOptions.bWriteGroups = true;
            IOWriteResult result = StandardMeshWriter.WriteFile(Path,
               new List<WriteMesh>() { new WriteMesh(Mesh) }, writeOptions);
        }



        [NodeFunction]
        [NodeReturnValue(DisplayName = "Meshes")]
        public static List<DMesh3> ImportMeshes(string Path = "c:\\scratch\\bunny.obj")
        {
            DMesh3Builder builder = new DMesh3Builder();
            StandardMeshReader reader = new StandardMeshReader() { MeshBuilder = builder };
            ReadOptions options = ReadOptions.Defaults;
            options.BaseFilePath = System.IO.Path.GetDirectoryName(Path);
            IOReadResult result = reader.Read(Path, ReadOptions.Defaults);
            return builder.Meshes;
        }


        [NodeFunction]
        [NodeParameter("Path", DisplayName = "OutputPath", DefaultValue = "c:\\scratch\\AA_FROM_GRAPH.obj")]
        public static void ExportMeshes(string Path, IEnumerable<DMesh3> Meshes)
        {
            List<WriteMesh> writeMeshes = new List<WriteMesh>();
            foreach (DMesh3 mesh in Meshes)
                writeMeshes.Add(new WriteMesh(mesh));

            WriteOptions writeOptions = WriteOptions.Defaults;
            writeOptions.bPerVertexNormals = true;
            writeOptions.bPerVertexUVs = true;
            writeOptions.bPerVertexColors = true;
            writeOptions.bWriteGroups = true;
            writeOptions.bCombineMeshes = false;
            IOWriteResult result = StandardMeshWriter.WriteFile(Path, writeMeshes, writeOptions);
        }

    }
}
