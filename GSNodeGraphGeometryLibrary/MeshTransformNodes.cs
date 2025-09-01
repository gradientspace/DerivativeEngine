// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using g3;

namespace Gradientspace.NodeGraph.Geometry
{
    [NodeFunctionLibrary("Geometry3.MeshTransforms")]
    [MappedFunctionLibraryName("Geometry.MeshTransforms")]
    public static class G3MeshTransformFunctions
    {
        [NodeFunction]
        [NodeReturnValue(DisplayName = "Mesh")]
        public static DMesh3? TranslateMesh(DMesh3 Mesh, float x = 0, float y = 0, float z = 0)
        {
            MeshTransforms.Translate(Mesh, new Vector3d(x, y, z));
            return Mesh;
        }


        [NodeFunction]
        [NodeReturnValue(DisplayName = "Mesh")]
        public static DMesh3? ReverseOrientation(DMesh3 Mesh, bool bFlipNormals = true)
        {
            Mesh.ReverseOrientation(bFlipNormals);
            return Mesh;
        }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "Mesh")]
        public static DMesh3? YUpToZup(DMesh3 Mesh)
        {
            MeshTransforms.ConvertYUpToZUp(Mesh);
            return Mesh;
        }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "Mesh")]
        public static DMesh3? ZUpToYUp(DMesh3 Mesh)
        {
            MeshTransforms.ConvertZUpToYUp(Mesh);
            return Mesh;
        }

        [NodeFunction]
        [NodeReturnValue(DisplayName = "Mesh")]
        public static DMesh3? FlipHandedness(DMesh3 Mesh)
        {
            MeshTransforms.FlipLeftRightCoordSystems(Mesh);
            return Mesh;
        }

    }
}
