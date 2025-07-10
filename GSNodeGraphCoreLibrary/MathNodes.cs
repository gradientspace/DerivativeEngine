// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;

namespace Gradientspace.NodeGraph.Nodes
{
    public abstract class StandardBinaryFloatOpNode : StandardNode
    {
        public static string Operand1Name { get { return "A"; } }
        public static string Operand2Name { get { return "B"; } }
        public static string ValueOutputName { get { return "Value"; } }

        public StandardBinaryFloatOpNode()
        {
            AddInput(Operand1Name, new StandardNodeInputWithConstant<float>(0.0f));
            AddInput(Operand2Name, new StandardNodeInputWithConstant<float>(0.0f));
            AddOutput(ValueOutputName, new StandardNodeOutput<float>());
        }

        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ValueOutputName);
            if (OutputIndex == -1)
                throw new Exception("FloatAddNode: output not found");

            float A = 0, B = 0, Result = 0;
            if (DataIn.FindItemValueStrict<float>(Operand1Name, ref A) &&
                 DataIn.FindItemValueStrict<float>(Operand2Name, ref B))
            {
                Result = ComputeResult(A,B);
            }

            RequestedDataOut.SetItemValue(OutputIndex, Result);
        }

        public abstract float ComputeResult(float A, float B);
    }

    [GraphNodeNamespace("Gradientspace.Math")]
    [GraphNodeUIName("Add Float")]
    public class FloatAddNode : StandardBinaryFloatOpNode
    {
        public override string GetDefaultNodeName() { 
            return "A + B";
        }
        public override float ComputeResult(float A, float B) { return A + B; }
    }

    [GraphNodeNamespace("Gradientspace.Math")]
    [GraphNodeUIName("Multiply Float")]
    public class FloatMultiplyNode : StandardBinaryFloatOpNode
    {
        public override string GetDefaultNodeName() {
            return "A * B";
        }
        public override float ComputeResult(float A, float B) { return A * B; }
    }


    [GraphNodeFunctionLibrary("Gradientspace.Math")]
    public static class StandardMathFunctionLibrary
    {

        [NodeFunction]
        public static float Add(float A, float B)
        {
            return A + B;
        }


        [NodeFunction]
        public static float Subtract(float A, float MinusB)
        {
            return A - MinusB;
        }

    }



    [GraphNodeNamespace("Gradientspace.Math")]
    [GraphNodeUIName("Add N Floats")]
    public class MultiFloatAddNode : StandardNode, INode_VariableInputs
    {
        public static string OperandBaseName { get { return "Input"; } }
        public static string ValueOutputName { get { return "Value"; } }

        public int NumInputs { get; set; } = 2;

        public MultiFloatAddNode()
        {
            AddInput(MakeInputName(0), new StandardNodeInputWithConstant<float>(0.0f));
            AddInput(MakeInputName(1), new StandardNodeInputWithConstant<float>(0.0f));
            AddOutput(ValueOutputName, new StandardNodeOutput<float>());
        }

        private string MakeInputName(int Index) { return OperandBaseName + (Index+1).ToString(); }

        public bool AddInput()
        {
            AddInput(MakeInputName(NumInputs), new StandardNodeInputWithConstant<float>(0.0f));
            NumInputs = NumInputs + 1;
            PublishNodeModifiedNotification();
            return true;
        }

        public bool RemoveInput(int SpecifiedIndex = -1)
        {
            if (NumInputs <= 2) return false;

            Inputs.RemoveAt(NumInputs - 1);
            NumInputs = Inputs.Count;
            PublishNodeModifiedNotification();
            return true;
        }


        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ValueOutputName);
            if (OutputIndex == -1)
                throw new Exception("MultiFloatAddNode: output not found");

            float Sum = 0; 
            for (int i = 0; i < NumInputs; ++i) {
                float Val = 0;
                if (DataIn.FindItemValueStrict<float>(MakeInputName(i), ref Val))
                    Sum += Val;
            }

            RequestedDataOut.SetItemValue(OutputIndex, Sum);
        }

    }

}

