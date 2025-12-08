// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Geometry3
{
    [NodeFunctionLibrary("Geometry3.MeshAABBTree")]
    public static class MeshAABBTreeFunctions
    {
        [NodeFunction]
        public static void BuildAABBTree(ref DMesh3 Mesh, bool CopyMesh, out DMeshAABBTree3 AABBTree)
        {
            DMesh3 UseMesh = (CopyMesh) ? new DMesh3(Mesh, false, false, false) : Mesh;
            AABBTree = new DMeshAABBTree3(UseMesh, true);
        }


        [NodeFunction]
        public static bool RayIntersection(ref DMeshAABBTree3 AABBTree, ref Ray3d Ray, out Vector3d HitPoint, out double RayParameter, out int TriangleID, double MaxDist = double.MaxValue)
        {
            TriangleID = AABBTree.FindNearestHitTriangle(Ray, MaxDist);
            if (TriangleID >= 0)
            {
                IntrRay3Triangle3 Intr = MeshQueries.TriangleIntersection(AABBTree.Mesh, TriangleID, Ray);
                RayParameter = Intr.RayParameter;
                HitPoint = Ray.PointAt(RayParameter);
                return true;
            }
            RayParameter = double.MaxValue; HitPoint = Vector3d.Zero;
            return false;
        }


        [NodeFunction]
        public static bool NearestPoint(ref DMeshAABBTree3 AABBTree, ref Vector3d Point, out Vector3d NearestPoint, out double Distance, out int TriangleID, double MaxDist = double.MaxValue)
        {
            TriangleID = AABBTree.FindNearestTriangle(Point, MaxDist);
            if (TriangleID >= 0) {
                DistPoint3Triangle3 Intr = MeshQueries.TriangleDistance(AABBTree.Mesh, TriangleID, Point);
                NearestPoint = Intr.TriangleClosest;
                Distance = Math.Sqrt(Intr.DistanceSquared);
                return true;
            }
            Distance = double.MaxValue; NearestPoint = Vector3d.Zero;
            return false;
        }


        [NodeFunction]
        public static void GetMesh(ref DMeshAABBTree3 AABBTree, out DMesh3 Mesh)
        {
            Mesh = AABBTree.Mesh;
        }

    }
}
