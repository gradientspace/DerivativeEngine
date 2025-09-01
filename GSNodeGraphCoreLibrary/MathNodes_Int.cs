// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;

namespace Gradientspace.NodeGraph.Nodes
{
    //  DivRem
    //  CopySign


    public class IntAddNode : StandardBinaryMathOpNode<int, int, int>
    {
        public override string OpName => "Add";
        public override string OpString => "A + B";
        public override int ComputeOp(ref readonly int A, ref readonly int B) { return A + B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) + ({B})"; }
    }
    public class IntSubtractNode : StandardBinaryMathOpNode<int, int, int>
    {
        public override string OpName => "Subtract";
        public override string OpString => "A - B";
        public override int ComputeOp(ref readonly int A, ref readonly int B) { return A - B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) - ({B})"; }
    }
    public class IntMultiplyNode : StandardBinaryMathOpNode<int, int, int>
    {
        public override string OpName => "Multiply";
        public override string OpString => "A * B";
        public override int ComputeOp(ref readonly int A, ref readonly int B) { return A * B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) * ({B})"; }
    }
    public class IntDivideNode : StandardBinaryMathOpNode<int, int, int>
    {
        public override string OpName => "Divide";
        public override string OpString => "A / B";
        public override int ComputeOp(ref readonly int A, ref readonly int B) { return A / B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) / ({B})"; }
    }
    public class IntModuloNode : StandardBinaryMathOpNode<int, int, int>
    {
        public override string OpName => "Modulo";
        public override string OpString => "A % B";
        public override int ComputeOp(ref readonly int A, ref readonly int B) { return A % B; }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) % ({B})"; }
    }
    public class IntNegateNode : StandardUnaryMathOpNode<int, int>
    {
        public override string OpName => "Negate";
        public override string OpString => "-A";
        public override int ComputeOp(ref readonly int A) { return -A; }
        protected override string CodeString(string A, string Result) { return $"{Result} = -({A})"; }
    }
    public class IntMulAddNode : StandardTrinaryMathOpNode<int, int, int, int>
    {
        public override string OpName => "A*B+C";
        public override string OpString => "A*B+C";
        public override int ComputeOp(ref readonly int A, ref readonly int B, ref readonly int C) { return A*B+C; }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = ({A})*({B})+({C})"; }
    }
    public class IntIncrementNode : StandardUnaryMathOpNode<int, int>
    {
        public override string OpName => "Increment";
        public override string OpString => "A++";
        public override int ComputeOp(ref readonly int A) { return A + 1; }
        protected override string CodeString(string A, string Result) { return $"{Result} = ({A})+1"; }
    }
    public class IntDecrementNode : StandardUnaryMathOpNode<int, int>
    {
        public override string OpName => "Decrement";
        public override string OpString => "A--";
        public override int ComputeOp(ref readonly int A) { return A - 1; }
        protected override string CodeString(string A, string Result) { return $"{Result} = ({A})-1"; }
    }


    public class IntPow2Node : StandardBinaryMathOpNode<int, int, int>
    {
        public override string Operand2Name => "Power";
        public override string OpName => "Pow2";
        public override string OpString => "Pow2";
        public override int ComputeOp(ref readonly int A, ref readonly int B) {
            int e = Math.Max(B, 0);
            return (e == 0) ? 1 : (A << e); }
        protected override string CodeString(string A, string B, string Result) { 
            return $"{{ int local_tmp = Math.Max({B},0);  {Result} = ({A}) << local_tmp; }}"; 
        }
    }


    public class IntLeftShiftNode : StandardBinaryMathOpNode<int, int, int>
    {
        public override string Operand2Name => "NumBits";
        public override string OpName => "LeftShift";
        public override string OpString => "LeftShift";
        public override int ComputeOp(ref readonly int A, ref readonly int B) { return A << Math.Max(B,0); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) << Math.Max({B},0)"; }
    }
    public class IntRightShiftNode : StandardBinaryMathOpNode<int, int, int>
    {
        public override string Operand2Name => "NumBits";
        public override string OpName => "RightShift";
        public override string OpString => "RightShift";
        public override int ComputeOp(ref readonly int A, ref readonly int B) { return A >> Math.Max(B,0); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = ({A}) >> Math.Max({B},0)"; }
    }



    public class IntAbsNode : StandardUnaryMathOpNode<int, int>
    {
        public override string OpName => "Abs";
        public override string OpString => "Abs";
        public override int ComputeOp(ref readonly int A) { return Math.Abs(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Abs({A})"; }
    }
    public class IntSignNode : StandardUnaryMathOpNode<int, int>
    {
        public override string OpName => "Sign";
        public override string OpString => "Sign";
        public override int ComputeOp(ref readonly int A) { return Math.Sign(A); }
        protected override string CodeString(string A, string Result) { return $"{Result} = Math.Sign({A})"; }
    }
    public class IntMinNode : StandardBinaryMathOpNode<int, int, int>
    {
        public override string OpName => "Min";
        public override string OpString => "Min";
        public override int ComputeOp(ref readonly int A, ref readonly int B) { return Math.Min(A, B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = Math.Min({A},{B})"; }
    }
    public class IntMaxNode : StandardBinaryMathOpNode<int, int, int>
    {
        public override string OpName => "Max";
        public override string OpString => "Max";
        public override int ComputeOp(ref readonly int A, ref readonly int B) { return Math.Max(A, B); }
        protected override string CodeString(string A, string B, string Result) { return $"{Result} = Math.Max({A},{B})"; }
    }
    public class IntClampNode : StandardTrinaryMathOpNode<int, int, int, int>
    {
        public override string Operand2Name => "Min";
        public override string Operand3Name => "Max";
        public override string OpName => "Clamp";
        public override string OpString => "Clamp";
        public override int ComputeOp(ref readonly int A, ref readonly int B, ref readonly int C) { return Math.Clamp(A, B, C); }
        protected override string CodeString(string A, string B, string C, string Result) { return $"{Result} = Math.Clamp({A},{B},{C})"; }
    }

}
