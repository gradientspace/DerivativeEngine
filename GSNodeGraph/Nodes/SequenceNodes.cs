// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Diagnostics;

namespace Gradientspace.NodeGraph
{
    [SystemNode]
    public class SequenceStartNode : NodeBase
    {
        public override string GetDefaultNodeName()
        {
            return "Start";
        }

        // start node should never be evaluated
        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            throw new NotImplementedException();
        }

    }




    public abstract class ControlFlowNode : NodeBase
    {
        public static string SelectedOutputPathName { get { return "@SelectedPath"; } }

        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(SelectedOutputPathName);
            if (OutputIndex == -1)
                throw new Exception("ControlFlowNode: SelectedOutputPathName output not found");

            ControlFlowOutput SelectedOutput = EvaluateInternal(in DataIn, RequestedDataOut);
            RequestedDataOut.SetItemValue(OutputIndex, new ControlFlowOutputID(SelectedOutput.OutputPathName) );
        }

        protected abstract ControlFlowOutput EvaluateInternal(in NamedDataMap DataIn, NamedDataMap RequestedDataOut);
    }


    public abstract class IterationNode : ControlFlowNode 
    {
        public static string ContinueIterationName { get { return "@ContinueIteration"; } }

        public abstract void InitializeIteration(in NamedDataMap DataIn);
        public abstract void NextIteration();
        public abstract bool DoneIterations { get; }


        protected virtual void SetContinueIterationOutput(NamedDataMap RequestedDataOut, bool bContinue)
        {
            int ContinueIndex = RequestedDataOut.IndexOfItem(ContinueIterationName);
            if (ContinueIndex == -1)
                throw new Exception("IterationNode: ContinueIterationName output not found");
            RequestedDataOut.SetItemValue(ContinueIndex, bContinue);
        }
    }

    [GraphNodeNamespace("Core.ControlFlow")]
    public class BranchNode : ControlFlowNode
    {
        public const string BooleanInputName  = "Boolean";
        public const string TrueOutputName = "True";
        public const string FalseOutputName = "False";

        public override string GetDefaultNodeName()
        {
            return "IfElse";
        }

        public BranchNode()
        {
            AddInput(BooleanInputName, new StandardNodeInputWithConstant<bool>(true));
            AddOutput(TrueOutputName, new ControlFlowOutput(TrueOutputName));
            AddOutput(FalseOutputName, new ControlFlowOutput(FalseOutputName));
        }

        // start node should never be evaluated
        protected override ControlFlowOutput EvaluateInternal(in NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            bool Value = true;
            int SelectedOutput = 0;
            if (DataIn.FindItemValueStrict<bool>(BooleanInputName, ref Value))
            {
                SelectedOutput = (Value) ? 0 : 1;
            }
            return (Outputs[SelectedOutput].Output as ControlFlowOutput)!;
        }
    }


    [GraphNodeNamespace("Core.ControlFlow")]
    public class ForLoopNode : IterationNode
    {
        public static string StartValueInputName { get { return "Start"; } }
        public static string EndValueInputName { get { return "End"; } }
        public static string IncludeEndValueInputName { get { return "Include End"; } }

        public static string IterationOutputName { get { return "Iteration"; } }
        public static string DoneOutputName { get { return "Done"; } }
        public static string IterationIndexName { get { return "Index"; } }

        public override string GetDefaultNodeName() { return "ForLoop"; }

        public ForLoopNode()
        {
            AddInput(StartValueInputName, new StandardNodeInputWithConstant<int>(0) );
            AddInput(EndValueInputName, new StandardNodeInputWithConstant<int>(10) );
            AddInput(IncludeEndValueInputName, new StandardNodeInputWithConstant<bool>(false));
            IterateOutputIndex = Outputs.Count;
            AddOutput(IterationOutputName, new ControlFlowOutput(IterationOutputName));
            AddOutput(IterationIndexName, new StandardNodeOutput<int>() );
            DoneOutputIndex = Outputs.Count;
            AddOutput(DoneOutputName, new ControlFlowOutput(DoneOutputName));
        }

        int IterateOutputIndex = 0;
        int DoneOutputIndex = 0;

        int IterationStart = 0;
        int IterationCounter = 0;
        int IterationEnd = 0;

        public override void InitializeIteration(in NamedDataMap DataIn)
        {
            IterationStart = 0;
            DataIn.FindItemValueStrict<int>(StartValueInputName, ref IterationStart);
            IterationEnd = 0;
            DataIn.FindItemValueStrict<int>(EndValueInputName, ref IterationEnd);
            bool bIncludeEnd = false;
            DataIn.FindItemValueStrict<bool>(IncludeEndValueInputName, ref bIncludeEnd);
            if (bIncludeEnd)
                IterationEnd++;
            IterationCounter = IterationStart;
        }

        public override void NextIteration()
        {
            if ( IterationCounter < IterationEnd )
                IterationCounter++;
        }

        public override bool DoneIterations { 
            get { return !(IterationCounter < IterationEnd); } 
        }

        // start node should never be evaluated
        protected override ControlFlowOutput EvaluateInternal(in NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int CounterOutputIndex = RequestedDataOut.IndexOfItem(IterationIndexName);
            if (CounterOutputIndex == -1)
                throw new Exception("ForLoopNode: IterationIndexName output not found");

            RequestedDataOut.SetItemValue(CounterOutputIndex, IterationCounter);

            bool bContinue = !DoneIterations;
            SetContinueIterationOutput(RequestedDataOut, bContinue);

            NextIteration();

            int SelectedOutput = (bContinue) ? IterateOutputIndex : DoneOutputIndex;
            ControlFlowOutput? Output = Outputs[SelectedOutput].Output as ControlFlowOutput;
            Debug.Assert(Output != null);
            return Output!;
        }
    }






    [GraphNodeNamespace("Core.ControlFlow")]
    [GraphNodeUIName("Sequence")]
    public class SequenceNode : IterationNode, INode_VariableOutputs
    {
        public static string SequenceOutputBaseName { get { return "Step"; } }
        public override string GetDefaultNodeName() { return "Sequence"; }

        public int NumOutputs { get; set; } = 3;

        public SequenceNode()
        {
            for (int j = 0; j < 3; ++j)
                AddOutput(MakeOutputName(j), new ControlFlowOutput(MakeOutputName(j)));
        }

        private string MakeOutputName(int Index) { return SequenceOutputBaseName + (Index+1).ToString(); }

        public bool AddOutput()
        {
            int NextIdx = Outputs.Count;
            AddOutput(MakeOutputName(NextIdx), new ControlFlowOutput(MakeOutputName(NextIdx)));
            NumOutputs = Outputs.Count;
            PublishNodeModifiedNotification();
            return true;
        }

        public bool RemoveOutput(int SpecifiedIndex = -1)
        {
            if (NumOutputs <= 1) return false;

            Outputs.RemoveAt(NumOutputs - 1);
            NumOutputs = Outputs.Count;
            PublishNodeModifiedNotification();
            return true;
        }

        int SequenceStart = 0;
        int SequenceIndex = 0;
        int SequenceEnd = 0;

        public override void InitializeIteration(in NamedDataMap DataIn)
        {
            SequenceStart = 0;
            SequenceEnd = Outputs.Count-1;
            SequenceIndex = SequenceStart;
        }

        public override void NextIteration()
        {
            if (SequenceIndex < SequenceEnd)
                SequenceIndex++;
        }

        public override bool DoneIterations
        {
            get { return !(SequenceIndex < SequenceEnd); }
        }

        protected override ControlFlowOutput EvaluateInternal(in NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            bool bContinue = !DoneIterations;
            SetContinueIterationOutput(RequestedDataOut, bContinue);
            int CurrentIndex = SequenceIndex;
            NextIteration();
            return (Outputs[CurrentIndex].Output as ControlFlowOutput)!;
        }


        public const string NumOutputsKey = "NumOutputs";
        public override void CollectCustomDataItems(out NodeCustomData? DataItems)
        {
            DataItems = new NodeCustomData()
                .AddIntItem(NumOutputsKey, NumOutputs);
        }
        public override void RestoreCustomDataItems(NodeCustomData DataItems)
        {
            NumOutputs = DataItems.FindIntItemOrDefault(NumOutputsKey, 0);
            updateInputsAndOutputs();
        }

        protected void updateInputsAndOutputs()
        {
            int WantOutputs = NumOutputs;
            int CurNumOutputs = Outputs.Count;
            while (Outputs.Count < WantOutputs)
                AddOutput();
            while (Outputs.Count > WantOutputs)
                Outputs.RemoveAt(Outputs.Count-1);
        }

    }




}
