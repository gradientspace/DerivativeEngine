// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Geometry3
{
    [NodeFunctionLibrary("Geometry3.MeshUtil")]
    public static class MeshUtilFunctions
    {
        [NodeFunction(ReturnName ="Copy")]
        public static DMesh3 DuplicateMesh(ref DMesh3 Mesh)
        {
            return new DMesh3(Mesh, true);
        }
    }
}
