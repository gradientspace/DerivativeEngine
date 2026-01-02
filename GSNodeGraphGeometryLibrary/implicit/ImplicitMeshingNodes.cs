// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using gs;

namespace Gradientspace.NodeGraph.Geometry
{
    [NodeFunctionLibrary("Geometry3.Implicit3")]
    public static class G3ImplicitMeshing3dFunctions
    {

        [NodeFunction]
        public static void MarchingCubes(ref BoundedImplicitFunction3d Implicit, double IsoValue, double CellSize, out DMesh3 Mesh)
        {
            AxisAlignedBox3d bounds = Implicit.Bounds();
            int max_axis_count = (int)(bounds.MaxDim / CellSize);
            if ( max_axis_count <= 1 || max_axis_count > 2048 )
                throw new ArgumentException("MarchingCubes: CellSize results in grid dimension outside valid range [2,2048]");

            if ( IsoValue > 0 )
                bounds.Expand(2*IsoValue);

            MarchingCubesPro mc = new MarchingCubesPro() {
                Implicit = Implicit,
                Bounds = bounds,
                CubeSize = CellSize,
                IsoValue = IsoValue
            };
            mc.Generate();
            Mesh = mc.Mesh;
        }



    }


}