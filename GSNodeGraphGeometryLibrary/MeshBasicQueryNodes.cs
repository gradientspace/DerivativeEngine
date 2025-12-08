// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Geometry3
{
    [NodeFunctionLibrary("Geometry3.Mesh")]
    public static class MeshBasicQueryFunctions
    {
        [NodeFunction]
        public static void GetBounds(ref DMesh3 Mesh, out AxisAlignedBox3d AxisBox)
        {
            // mesh doesn't have function to ignore isolated vertices...
            AxisBox = AxisAlignedBox3d.Empty;
            foreach ( int tid in Mesh.TriangleIndices() )
                AxisBox.Contain(Mesh.GetTriBounds(tid));
        }

        [NodeFunction(ReturnName = "Position")]
        public static Vector3d GetVertexPos(ref DMesh3 Mesh, int VertexID)
        {
            return Mesh.GetVertex(VertexID);
        }

        [NodeFunction(ReturnName = "Triangle")]
        public static Index3i GetTriangle(ref DMesh3 Mesh, int TriangleID)
        {
            return Mesh.GetTriangle(TriangleID);
        }

        [NodeFunction]
        public static void GetTriNormal(ref DMesh3 Mesh, int TriangleID, out Vector3d Normal)
        {
            Normal = Mesh.GetTriNormal(TriangleID);
        }

        [NodeFunction]
        public static void GetTriInfo(ref DMesh3 Mesh, int TriangleID, out Vector3d Normal, out Vector3d Centroid, out double Area)
        {
            Mesh.GetTriInfo(TriangleID, out Normal, out Area, out Centroid);
        }


    }
}
