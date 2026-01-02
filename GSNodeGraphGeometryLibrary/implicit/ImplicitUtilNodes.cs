// Copyright Gradientspace Corp. All Rights Reserved.
using g3;

namespace Gradientspace.NodeGraph.Geometry
{
    [NodeFunctionLibrary("Geometry3.Implicit3")]
    public static class G3ImplicitUtilFunctions
    {
        [NodeFunction(IsPure = true)]
        public static void GetImplicitBounds(ref BoundedImplicitFunction3d Implicit, out AxisAlignedBox3d Bounds)
        {
            Bounds = Implicit.Bounds();
        }

        [NodeFunction(IsPure = true)]
        public static void GridCellSizeFromBounds(ref AxisAlignedBox3d Bounds, out double CellSize, int MaxAxisCellCount = 64, double MinCellSize = 0.01 )
        {
            double countCellSize = Bounds.MaxDim / MaxAxisCellCount;
            CellSize = Math.Max(countCellSize, MinCellSize);
        }

        [NodeFunction(IsPure = true)]
        public static void GridCellSizeFromImplicit(ref BoundedImplicitFunction3d Implicit, out double CellSize, int MaxAxisCellCount = 64, double MinCellSize = 0.01)
        {
            AxisAlignedBox3d Bounds = Implicit.Bounds();
            double countCellSize = Bounds.MaxDim / MaxAxisCellCount;
            CellSize = Math.Max(countCellSize, MinCellSize);
        }

    }
}