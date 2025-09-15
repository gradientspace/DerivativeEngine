// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using g3;

namespace Gradientspace.NodeGraph.Geometry
{
    // PerVertexTransform functions

    [NodeFunctionLibrary("Geometry3.MeshTransforms")]
    [MappedFunctionLibraryName("Geometry.MeshTransforms")]
    public static class G3MeshTransformFunctions
    {
        [NodeFunction]
        public static void TranslateMesh(ref DMesh3 Mesh, Vector3d Translation)
        {
            MeshTransforms.Translate(Mesh, Translation);
        }

        [NodeFunction]
        public static void ScaleMesh(ref DMesh3 Mesh, Vector3d Scale, Vector3d Origin)
        {
            MeshTransforms.Scale(Mesh, Scale, Origin);
        }

        [NodeFunction]
        public static void RotateMesh(ref DMesh3 Mesh, Quaterniond Rotation, Vector3d Origin)
        {
            MeshTransforms.Rotate(Mesh, Origin, Rotation);
        }

        [NodeFunction]
        public static void WorldToFrame(ref DMesh3 Mesh, Frame3d Frame)
        {
            MeshTransforms.ToFrame(Mesh, Frame);
        }

        [NodeFunction]
        public static void FrameToWorld(ref DMesh3 Mesh, Frame3d Frame)
        {
            MeshTransforms.FromFrame(Mesh, Frame);
        }

        [NodeFunction]
        public static void TransformMesh(ref DMesh3 Mesh, Matrix4d Transform)
        {
            MeshTransforms.TransformMesh(Mesh, Transform);
        }




        [NodeFunction]
        public static void FlipOrientation(ref DMesh3 Mesh, bool FlipNormals = true)
        {
            Mesh.ReverseOrientation(FlipNormals);
        }

        [NodeFunction]
        public static void YUpToZup(ref DMesh3 Mesh)
        {
            MeshTransforms.ConvertYUpToZUp(Mesh);
        }

        [NodeFunction]
        public static void ZUpToYUp(ref DMesh3 Mesh)
        {
            MeshTransforms.ConvertZUpToYUp(Mesh);
        }

        [NodeFunction]
        public static void FlipHandedness(ref DMesh3 Mesh)
        {
            MeshTransforms.FlipLeftRightCoordSystems(Mesh);
        }

    }
}
