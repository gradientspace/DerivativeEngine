// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;

namespace Gradientspace.NodeGraph
{
    [GraphNodeFunctionLibrary("Gradientspace.Math")]
    public static class GradientspaceIntMathFunctionLibrary
    {
        [NodeFunction]
        public static int Add(int A, int B) {
            return A + B;
        }
        [NodeFunction]
        public static int Subtract(int A, int B) {
            return A - B;
        }
        [NodeFunction]
        public static int Multiply(int A, int B) {
            return A * B;
        }
        [NodeFunction]
        public static int Divide(int A, int B) {
            return A / B;
        }
        [NodeFunction]
        public static int Pow2(int Value, int Exponent)
        {
            Exponent = Math.Max(Exponent, 0);
            return (Exponent == 0) ? 1 : (Value << Exponent);
        }
        [NodeFunction]
        public static int LeftShift(int Value, int NumBits)
        {
            NumBits = Math.Max(NumBits, 0);
            return (Value << NumBits);
        }
    }

}
