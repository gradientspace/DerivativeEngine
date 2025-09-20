// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using g3;

namespace Gradientspace.NodeGraph.Geometry
{
    public static class NodeGraphGeometryLibrary
    {
        public static void Initialize()
        {
            DefaultTypeInfoLibrary.Instance.RegisterType(
                typeof(g3.Vector3d), true, Vector3d.Zero, NodeParameterHandler_Vector3d);

            //DefaultTypeInfoLibrary.Instance.RegisterType(
            //    typeof(g3.Ray3d), false);
        }

        private static object? NodeParameterHandler_Vector3d(NodeParameter nodeParam)
        {
            if (nodeParam.DefaultRealVec != null && nodeParam.DefaultRealVec.Length == 3)
                return new Vector3d(nodeParam.DefaultRealVec);
            return null;
        }
    }
}
