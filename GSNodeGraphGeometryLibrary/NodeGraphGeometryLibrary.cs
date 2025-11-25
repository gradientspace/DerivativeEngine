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
                typeof(g3.Vector2d), true, Vector2d.Zero, NodeParameterHandler_Vector2d);

            DefaultTypeInfoLibrary.Instance.RegisterType(
                typeof(g3.Vector3d), true, Vector3d.Zero, NodeParameterHandler_Vector3d);

            DefaultTypeInfoLibrary.Instance.RegisterType(
                typeof(g3.Quaterniond), true, Quaterniond.Identity, NodeParameterHandler_Quaterniond);

            // types below need json serializer to support input constant...

            //DefaultTypeInfoLibrary.Instance.RegisterType(
            //    typeof(g3.Frame3d), true, Frame3d.Identity, NodeParameterHandler_Frame3d);

            //DefaultTypeInfoLibrary.Instance.RegisterType(
            //    typeof(g3.Ray3d), false);
        }

        private static object? NodeParameterHandler_Vector2d(NodeParameter nodeParam)
        {
            if (nodeParam.DefaultRealVec != null && nodeParam.DefaultRealVec.Length == 2)
                return new Vector2d(nodeParam.DefaultRealVec);
            return null;
        }

        private static object? NodeParameterHandler_Vector3d(NodeParameter nodeParam)
        {
            if (nodeParam.DefaultRealVec != null && nodeParam.DefaultRealVec.Length == 3)
                return new Vector3d(nodeParam.DefaultRealVec);
            return null;
        }

        private static object? NodeParameterHandler_Quaterniond(NodeParameter nodeParam)
        {
            if (nodeParam.DefaultRealVec != null && nodeParam.DefaultRealVec.Length == 4)
                return new Quaterniond(nodeParam.DefaultRealVec);
            return null;
        }
    }
}
