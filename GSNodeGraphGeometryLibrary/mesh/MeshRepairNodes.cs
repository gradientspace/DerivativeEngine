// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using g3;
using gs;

namespace Gradientspace.NodeGraph.Geometry3
{
    [NodeFunctionLibrary("Geometry3.MeshModeling")]
    public static class G3MeshRepairFunctions
    {
        public enum EAutoRepairRemoveInteriorMode
        {
            None = 0, 
            Interior = 1, 
            Occluded = 2
        }


        [NodeFunction]
        public static void AutoRepairMesh(ref DMesh3 Mesh,
            double MinEdgeLenTol = 0.0001, int ErosionIters = 5, EAutoRepairRemoveInteriorMode RemoveHidden = EAutoRepairRemoveInteriorMode.None)
        {
            MeshAutoRepair repair = new(Mesh);
            repair.MinEdgeLengthTol = MinEdgeLenTol;
            repair.ErosionIterations = 5;
            repair.RemoveMode = (MeshAutoRepair.RemoveModes)(int)RemoveHidden;
            repair.Apply();
        }

    }

}