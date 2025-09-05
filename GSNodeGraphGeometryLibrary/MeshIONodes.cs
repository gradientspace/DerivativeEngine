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
                return builder.Meshes[0];
            }
            return null;
        }


        [NodeFunction]
        [NodeParameter("Path", DisplayName = "OutputPath", DefaultValue ="c:\\scratch\\AA_FROM_GRAPH.obj")]
        public static void ExportMesh(string Path, DMesh3 Mesh)
        {
            WriteOptions writeOptions = WriteOptions.Defaults;
            writeOptions.bPerVertexNormals = true;
            writeOptions.bPerVertexUVs = true;
            writeOptions.bPerVertexColors = true;
            writeOptions.bWriteGroups = true;
            IOWriteResult result = StandardMeshWriter.WriteFile(Path,
               new List<WriteMesh>() { new WriteMesh(Mesh) }, writeOptions);
        }



        [NodeFunction]
        [NodeReturnValue(DisplayName = "Mesh")]
        public static DMesh3? TestImportGLTF(string Path = "D:\\samplefiles\\gltf_avocado\\Avocado.gltf")
        {
            using (FileStream fileStream = File.OpenRead(Path)) {
                var result = GLTFFile.ParseFile(fileStream);
                GLTFFile.Root data = result.Value;

                GlobalGraphOutput.AppendLine("read..");
            }
            return null;
        }

    }
}
