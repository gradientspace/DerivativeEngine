using g3;
using Gradientspace.NodeGraph.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Geometry
{
    public class AxisBox3EmptyNode : StandardMathConstantNode<AxisAlignedBox3d>
    {
        public override string OpNamespace => "Geometry3.AxisBox3";
        public override string OpName => "Empty";
        public override string OpString => "Empty";
        public override AxisAlignedBox3d ConstantValue => AxisAlignedBox3d.Empty;
        protected override string CodeString(string Result) { return $"{Result} = AxisAlignedBox3d.Empty"; }
    }


    public class AxisBox3CenterExtentsNode : StandardUnaryMathOpNode2<AxisAlignedBox3d, Vector3d, Vector3d>
    {
        public override string OpNamespace => "Geometry3.AxisBox3";
        public override string OpName => "CenterExtents";
        public override string Operand1Name => "AxisBox";
        public override string Output1Name => "Center";
        public override string Output2Name => "Extents";
        public override string OpString => "CenterExtents";
        public override (Vector3d, Vector3d) ComputeOp(ref readonly AxisAlignedBox3d A) { return (A.Center, A.Extents); }
        protected override string CodeString(string A, string Result1, string Result2) { return $"{{ {Result1} = {A}.Center; {Result2} = {A}.Extents; }}"; }
    }
}
