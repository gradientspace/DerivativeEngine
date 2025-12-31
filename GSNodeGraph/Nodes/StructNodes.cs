// Copyright Gradientspace Corp. All Rights Reserved.
using System.Diagnostics;
using System.Reflection;


namespace Gradientspace.NodeGraph
{
    /// <summary>
    /// Base class for placeholder nodes that take a struct (eg SplitStruct placeholder)
    /// </summary>
    [ClassHierarchyNode]
    public abstract class StructPlaceholderNodeBase : PlaceholderNodeBase
    {
        public const string StructInputName = "Struct";

        protected PlaceholderNodeInput PlaceholderInput;

        public StructPlaceholderNodeBase()
        {
            PlaceholderInput = new PlaceholderNodeInput(DynamicInputDataTypeFilters.StructFilter);
            PlaceholderInput.CustomTypeString = "struct";
            AddInput(StructInputName, PlaceholderInput);
        }

        public override bool GetPlaceholderReplacementNodeInfo(
            string PlaceholderInputName, in GraphDataType incomingType,
            out Type ReplacementNodeClassType, out string ReplacementNodeInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            bool bCanReplace =
                (PlaceholderInputName == StructInputName && PlaceholderInput.IsCompatibleWith(incomingType));
            if (!bCanReplace) {
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


    /// <summary>
    /// Base class for concrete instantiations of struct placeholder nodes. 
    /// Has a fixed-type Struct input, with type defined on construction and saved/restored via CustomDataItems
    /// subclass implements BuildDependentInputsAndOutputs() to add inputs/outputs based on struct type
    /// subclass implements EvaluateInternal() to handle evaluation given the struct input
    /// </summary>
    [ClassHierarchyNode]
    public abstract class StructAccessNodeBase : NodeBase
    {
        public const string StructInputName = "Struct";

        protected Type ActiveStructType = typeof(void);

        public StructAccessNodeBase()
        {
            updateInputsOutputs();
        }

        public virtual void Initialize(Type structType)
        {
            setStructType(structType);
        }

        protected virtual void setStructType(Type structType)
        {
			Debug.Assert(TypeUtils.IsStruct(structType));
			ActiveStructType = structType;
			updateInputsOutputs();
            PublishNodeModifiedNotification();
        }

        protected virtual void updateInputsOutputs()
        {
            Inputs.Clear();
            Outputs.Clear();

            StandardNodeInputBase StructInput = new StandardNodeInputBase(ActiveStructType);
            AddInput(StructInputName, StructInput);

            BuildDependentInputsAndOutputs();
        }

        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            object? FoundStruct = DataIn.FindItemValueAsType(StructInputName, ActiveStructType);
            if ( FoundStruct == null )
                throw new Exception("incoming struct undefined or has incorrect Type");

            EvaluateInternal(in DataIn, RequestedDataOut, FoundStruct);
        }

        protected abstract void BuildDependentInputsAndOutputs();
        protected abstract void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, object StructIn);

        // save/restore the struct type
		public const string StructTypeKey = "StructType";
		public override void CollectCustomDataItems(out NodeCustomData? DataItems)
		{
            DataItems = new NodeCustomData().AddTypeItem(StructTypeKey, ActiveStructType);
		}
		public override void RestoreCustomDataItems(NodeCustomData DataItems)
		{
			Type? structType = DataItems.FindTypeItemChecked(StructTypeKey, out string TypeName, "StructAccessNodeBase: StructType custom data is missing");
			setStructType(structType!);
		}
	}




    /// <summary>
    /// Placeholder for SplitStruct
    /// </summary>
    [GraphNodeNamespace("Core.Struct")]
    public class SplitStructPlaceholderNode : StructPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "Split Struct"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(SplitStructNode);
            ReplacementInputName = SplitStructNode.StructInputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as SplitStructNode)?.Initialize(incomingType.CSType);
            };
        }
    }


    /// <summary>
    /// SplitStructNode adds an output for each public field/property of the incoming struct
    /// </summary>
    [SystemNode]
	public class SplitStructNode : StructAccessNodeBase
    {
        public override string GetDefaultNodeName() { return "Split Struct"; }

        List<(string Name, Type dataType, MemberInfo memberInfo)> ActiveFields = new();

        public SplitStructNode() {
            Flags |= ENodeFlags.IsPure;
        }

        protected override void BuildDependentInputsAndOutputs()
        {
            ActiveFields.Clear();
            foreach (var memberInfo in TypeUtils.EnumerateStructDataMembers(ActiveStructType)) {
                INodeOutput output = new StandardNodeOutputBase(memberInfo.dataType);
                AddOutput(memberInfo.Name, output);
                ActiveFields.Add(memberInfo);
            }
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, object StructIn)
        {
            foreach (var memberInfo in ActiveFields) {
                int idx = RequestedDataOut.IndexOfItem(memberInfo.Name);
                if (idx < 0) continue;  // in pure nodes not all outputs will be requested...

                if (memberInfo.memberInfo is FieldInfo fieldInfo) {
                    object? value = fieldInfo.GetValue(StructIn);
                    RequestedDataOut.SetItemValueOrNull(idx, value);
                } else if (memberInfo.memberInfo is PropertyInfo propInfo) {
                    object? value = propInfo.GetValue(StructIn);
                    RequestedDataOut.SetItemValueOrNull(idx, value);
                }
            }
        }

    }


}

