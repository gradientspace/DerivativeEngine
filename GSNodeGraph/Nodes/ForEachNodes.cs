using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
    [ClassHierarchyNode]
    public abstract class EnumerablePlaceholderNodeBase : PlaceholderNodeBase
    {
        public const string EnumerableInputOutputName = "Enumerable";

        protected PlaceholderNodeInput PlaceholderInput;

        public EnumerablePlaceholderNodeBase()
        {
            PlaceholderInput = new PlaceholderNodeInput(DynamicInputDataTypeFilters.EnumerableFilter);
            PlaceholderInput.CustomTypeString = "IEnumerable<>";
            AddInput(EnumerableInputOutputName, PlaceholderInput);
        }

        public override bool GetPlaceholderReplacementNodeInfo(
            string PlaceholderInputName, in GraphDataType incomingType,
            out Type ReplacementNodeClassType, out string ReplacementNodeInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            bool bCanReplace =
                (PlaceholderInputName == EnumerableInputOutputName && PlaceholderInput.IsCompatibleWith(incomingType));
            if (!bCanReplace)
            {
                ReplacementNodeClassType = typeof(void);
                ReplacementNodeInputName = "";
                ReplacementNodeInitializer = null;
                return false;
            }
            GetReplacementInfo(out ReplacementNodeClassType, out ReplacementNodeInputName, out ReplacementNodeInitializer);
            return true;
        }

        protected abstract void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer);
    }




    [GraphNodeNamespace("Gradientspace.ControlFlow")]
    public class ForEachPlaceholderNode : EnumerablePlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "For Each"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(ForEachEnumerableNode);
            ReplacementInputName = ForEachEnumerableNode.EnumerableInputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as ForEachEnumerableNode)?.Initialize(incomingType.DataType);
            };
        }
    }


    [SystemNode]
    [GraphNodeNamespace("Gradientspace.ControlFlow")]
    public class ForEachEnumerableNode : IterationNode
    {
        public static string EnumerableInputName { get { return "Enumeration"; } }
        public static string IterationOutputName { get { return "Iteration"; } }
        public static string IterationElementName { get { return "Element"; } }
        public static string DoneOutputName { get { return "Done"; } }
        public static string IterationIndexName { get { return "Index"; } }

        public override string GetDefaultNodeName() { return "ForEach"; }

        protected Type EnumerableElementType = typeof(object);

        public ForEachEnumerableNode()
        {
        }

        public virtual void Initialize(Type enumerableType)
        {
            Debug.Assert( TypeUtils.IsEnumerable(enumerableType) );
            Type elementType = TypeUtils.GetEnumerableElementType(enumerableType) ?? typeof(object);
            initializeInternal(elementType);
        }
        protected void initializeInternal(Type enumerableElementType)
        {
            EnumerableElementType = enumerableElementType;
            updateInputsOutputs();
            PublishNodeModifiedNotification();
        }

        protected virtual void updateInputsOutputs()
        {
            Inputs.Clear();
            Outputs.Clear();

            AddInput(EnumerableInputName, new EnumerableNodeInput(EnumerableElementType));

            IterateOutputIndex = Outputs.Count;
            AddOutput(IterationOutputName, new ControlFlowOutput(IterationOutputName));
            AddOutput(IterationElementName, new StandardNodeOutputBase(EnumerableElementType));
            AddOutput(IterationIndexName, new StandardNodeOutput<int>());
            DoneOutputIndex = Outputs.Count;
            AddOutput(DoneOutputName, new ControlFlowOutput(DoneOutputName));
        }

        int IterateOutputIndex = 0;
        int DoneOutputIndex = 0;

        int IterationStart = 0;
        int IterationCounter = 0;
        int IterationEnd = 0;

        List<object>? IterationItems;

        public override void InitializeIteration(in NamedDataMap DataIn)
        {
            object? FoundEnumerable = DataIn.FindItemValueAsType(EnumerableInputName, typeof(System.Collections.IEnumerable));
            System.Collections.IEnumerable enumerable = (FoundEnumerable as System.Collections.IEnumerable)!;

            IterationItems = new List<object>();
            foreach (object o in enumerable)
                IterationItems.Add(o);

            IterationStart = 0;
            IterationEnd = IterationItems.Count;
            IterationCounter = IterationStart;
        }

        public override void NextIteration()
        {
            if (IterationCounter < IterationEnd)
                IterationCounter++;
        }

        public override bool DoneIterations
        {
            get { return !(IterationCounter < IterationEnd); }
        }

        // start node should never be evaluated
        protected override ControlFlowOutput EvaluateInternal(in NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int CounterOutputIndex = RequestedDataOut.IndexOfItem(IterationIndexName);
            if (CounterOutputIndex == -1)
                throw new Exception("ForLoopNode: IterationIndexName output not found");

            int UseItemIndex = Math.Min(IterationCounter, IterationEnd-1);
            RequestedDataOut.SetItemValue(CounterOutputIndex, UseItemIndex);
            RequestedDataOut.SetItemValueChecked(IterationElementName, IterationItems![UseItemIndex]);

            bool bContinue = !DoneIterations;
            SetContinueIterationOutput(RequestedDataOut, bContinue);

            NextIteration();

            int SelectedOutput = (bContinue) ? IterateOutputIndex : DoneOutputIndex;
            ControlFlowOutput? Output = Outputs[SelectedOutput].Output as ControlFlowOutput;
            Debug.Assert(Output != null);

            if (SelectedOutput == DoneOutputIndex)
                IterationItems = null;

            return Output!;
        }


        public const string ElementTypeString = "ElementType";
        public override void CollectCustomDataItems(out List<Tuple<string, object>>? DataItems)
        {
            DataItems = new List<Tuple<string, object>>();
            DataItems.Add(new(ElementTypeString, EnumerableElementType.ToString()));
        }
        public override void RestoreCustomDataItems(List<Tuple<string, object>> DataItems)
        {
            Tuple<string, object>? ElementTypeAsString = DataItems.Find((x) => { return x.Item1 == ElementTypeString; });
            if (ElementTypeAsString == null)
                throw new Exception("ForEachNode: ElementType custom data is missing");
            string TypeName = ElementTypeAsString.Item2.ToString()!;

            Type? elementType = Type.GetType(TypeName);
            if (elementType != null)
                initializeInternal(elementType);
            else
                throw new Exception("ForEachNode: Type " + TypeName + " Could not be found");
        }

    }


}
