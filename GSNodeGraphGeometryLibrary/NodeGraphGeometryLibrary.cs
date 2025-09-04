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
                typeof(g3.Vector3d), true, Vector3d.Zero);

            DefaultTypeInfoLibrary.Instance.RegisterType(
                typeof(g3.Ray3d), false);

        }
    }
}
