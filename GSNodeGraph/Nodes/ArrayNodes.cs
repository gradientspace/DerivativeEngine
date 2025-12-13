// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
    [ClassHierarchyNode]
    public abstract class ArrayAccessPlaceholderNodeBase : PlaceholderNodeBase
    {
        public const string ArrayInputOutputName = "Array";

        protected PlaceholderNodeInput PlaceholderInput;

        public ArrayAccessPlaceholderNodeBase()
        {
            PlaceholderInput = new PlaceholderNodeInput(DynamicInputDataTypeFilters.ArrayFilter);
            PlaceholderInput.CustomTypeString = "object[]";
            AddInput(ArrayInputOutputName, PlaceholderInput);
        }

        public override bool GetPlaceholderReplacementNodeInfo(
            string PlaceholderInputName, in GraphDataType incomingType,
            out Type ReplacementNodeClassType, out string ReplacementNodeInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            bool bCanReplace =
                (PlaceholderInputName == ArrayInputOutputName && PlaceholderInput.IsCompatibleWith(incomingType));
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





    [ClassHierarchyNode]
    public abstract class ArrayElementNodeBase : NodeBase
    {
        public const string ArrayInputOutputName = "Array";
        public const string IndexInputName = "Index";

        protected Type ArrayType = typeof(Array);
        protected Type ArrayElementType = typeof(object);

        protected bool bEnableIndexInput = true;

        public ArrayElementNodeBase()
        {
            updateInputsOutputs();
        }

        public virtual void Initialize(Type arrayType)
        {
            setArrayType(arrayType);
        }

        protected virtual void setArrayType(Type arrayType)
        {
            if (arrayType == typeof(Array))
            {
                ArrayType = typeof(Array);
                ArrayElementType = typeof(object);
            } 
            else
            {
				Debug.Assert(arrayType.IsArray);
				ArrayType = arrayType;
				Debug.Assert(ArrayType.GetElementType() != null);
				ArrayElementType = ArrayType.GetElementType()!;
			}
			updateInputsOutputs();
            PublishNodeModifiedNotification();
        }

        protected virtual void updateInputsOutputs()
        {
            Inputs.Clear();
            Outputs.Clear();

            StandardNodeInputBase ArrayInput = new StandardNodeInputBase(ArrayType);
            ArrayInput.Flags |= ENodeInputFlags.IsInOut;
            AddInput(ArrayInputOutputName, ArrayInput);
            AddOutput(ArrayInputOutputName, new StandardNodeOutputBase(ArrayType));

            if (bEnableIndexInput)
                AddInput(IndexInputName, new StandardNodeInputWithConstant<int>(0));

            UpdateElementField();
        }

        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int ArrayOutputIndex = RequestedDataOut.IndexOfItem(ArrayInputOutputName);
            Array? FoundArray = DataIn.FindItemValueAsType(ArrayInputOutputName, ArrayType) as Array;
            Debug.Assert(FoundArray != null);

            int Index = 0;
            if (bEnableIndexInput)
                DataIn.FindItemValueStrict<int>(IndexInputName, ref Index);

            EvaluateInternal(in DataIn, RequestedDataOut, FoundArray, Index);

            if (ArrayOutputIndex >= 0)
                RequestedDataOut.SetItemValue(ArrayOutputIndex, FoundArray);
        }

        protected abstract void UpdateElementField();
        protected abstract void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, Array ArrayIn, int Index);



        // save/restore the array type

		public const string ArrayTypeKey = "ArrayType";
		public override void CollectCustomDataItems(out NodeCustomData? DataItems)
		{
            DataItems = new NodeCustomData().
                AddTypeItem(ArrayTypeKey, ArrayType);
		}
		public override void RestoreCustomDataItems(NodeCustomData DataItems)
		{
			Type? arrayType = DataItems.FindTypeItemChecked(ArrayTypeKey, out string TypeName, "ArrayElementNodeBase: ArrayType custom data is missing");
            if (arrayType != null)
				setArrayType(arrayType);
			else
				throw new Exception("ArrayElementNodeBase: ArrayType " + TypeName + " Could not be found");
		}

	}



    [GraphNodeNamespace("Core.Array")]
    public class GetArrayElementPlaceholderNode : ArrayAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "Get Element"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(GetArrayElementNode);
            ReplacementInputName = GetArrayElementNode.ArrayInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as GetArrayElementNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class GetArrayElementNode : ArrayElementNodeBase
    {
        public const string ValueOutputName = "Value";

        public override string GetDefaultNodeName() { return "Get Element"; }

        protected override void UpdateElementField() {
            AddOutput(ValueOutputName, new StandardNodeOutputBase(ArrayElementType));
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, Array ArrayIn, int Index)
        {
            object? arrayElement = ArrayIn.GetValue(Index);
            RequestedDataOut.SetItemValueChecked(ValueOutputName, arrayElement!);
        }
    }


    [GraphNodeNamespace("Core.Array")]
    public class SetArrayElementPlaceholderNode : ArrayAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "Set Element"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(SetArrayElementNode);
            ReplacementInputName = SetArrayElementNode.ArrayInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as SetArrayElementNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class SetArrayElementNode : ArrayElementNodeBase
    {
        public const string ValueInputName = "Value";

        public override string GetDefaultNodeName() { return "Set Element"; }

        protected override void UpdateElementField() {
            AddInput(ValueInputName, FunctionNodeUtils.BuildInputNodeForType(ArrayElementType));
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, Array ArrayIn, int Index)
        {
            object? NewValue = DataIn.FindItemValueAsType(ValueInputName, ArrayElementType);
            Debug.Assert(NewValue != null);
            ArrayIn.SetValue(NewValue, Index);
        }
    }




    [GraphNodeNamespace("Core.Array")]
    public class GetArrayLengthPlaceholderNode : ArrayAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "Get Length"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(GetArrayLengthNode);
            ReplacementInputName = GetArrayLengthNode.ArrayInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as GetArrayLengthNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class GetArrayLengthNode : ArrayElementNodeBase
    {
        public const string LengthOutputName = "Length";
        public override string GetDefaultNodeName() { return "Get Length"; }

        public GetArrayLengthNode() { bEnableIndexInput = false; }

        protected override void UpdateElementField() {
            AddOutput(LengthOutputName, new StandardNodeOutput<int>());
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, Array ArrayIn, int Index)
        {
            RequestedDataOut.SetItemValueChecked(LengthOutputName, ArrayIn.Length);
        }
    }



    [GraphNodeNamespace("Core.Array")]
    public class ArrayToStringPlaceholderNode : ArrayAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "Array To String"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(ArrayToStringNode);
            ReplacementInputName = ArrayToStringNode.ArrayInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as ArrayToStringNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class ArrayToStringNode : ArrayElementNodeBase
    {
        public const string StringOutputName = "String";
        public override string GetDefaultNodeName() { return "Array To String"; }

        public const string IndicesInputName = "Indices";
        protected StandardNodeInputWithConstant<bool> IndicesInput;

        public ArrayToStringNode() { 
            bEnableIndexInput = false;
            IndicesInput = new StandardNodeInputWithConstant<bool>(false);
            IndicesInput.Flags |= ENodeInputFlags.IsNodeConstant;
        }

        protected override void UpdateElementField()
        {
            AddInput(IndicesInputName, IndicesInput);
            AddOutput(StringOutputName, new StandardNodeOutput<string>());
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, Array ArrayIn, int Index)
        {
            bool bIndices = DataIn.FindStructValueOrDefault<bool>(IndicesInputName, false, false);

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append('[');
            for ( int i = 0; i < ArrayIn.Length; ++i) {
                if (bIndices)
                    stringBuilder.Append($"{i}:");
                stringBuilder.Append(ArrayIn.GetValue(i)?.ToString() ?? "(null)");
                if ( i < ArrayIn.Length - 1 )
                    stringBuilder.Append(",");
            }
            stringBuilder.Append(']');

            RequestedDataOut.SetItemValueChecked(StringOutputName, stringBuilder.ToString());
        }
    }




    [GraphNodeNamespace("Core.Array")]
    public class ArrayToListPlaceholderNode : ArrayAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "Array To List"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(ArrayToListNode);
            ReplacementInputName = ArrayToListNode.ArrayInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as ArrayToListNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class ArrayToListNode : ArrayElementNodeBase
    {
        public const string ListOutputName = "List";
        public override string GetDefaultNodeName() { return "Array To List"; }

        public ArrayToListNode() { 
            bEnableIndexInput = false;
        }

        protected override void UpdateElementField()
        {
            Type genericListType = typeof(List<>).MakeGenericType(ArrayElementType);
            AddOutput(ListOutputName, new StandardNodeOutputBase(genericListType));
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, Array ArrayIn, int Index)
        {
            Type genericListType = typeof(List<>).MakeGenericType(ArrayElementType);
            IList? newList = (IList?)Activator.CreateInstance(genericListType);
            for (int i = 0; i < ArrayIn.Length; ++i)
                newList!.Add(ArrayIn.GetValue(i));
            RequestedDataOut.SetItemValueChecked(ListOutputName, newList!);
        }
    }






    [GraphNodeNamespace("Core.Array")]
	[GraphNodeUIName("Make Array")]
	public class MakeArrayFromValuesNode : NodeBase, INode_VariableInputs
	{
        public override string GetDefaultNodeName() { return "Make Array"; }

		public static string ElementBaseName { get { return "Element"; } }
		public const string TypeInputName = "Type";
		public const string AllocateObjectsInputName = "Alloc Objects";
		public const string ArrayOutputName = "Array";

		public int NumArrayInputs { get; set; } = 2;

		ClassTypeNodeInput TypeInput;
        StandardNodeInputWithConstant<bool>? AllocateObjectsInput;
        Type ActiveArrayType;

        public MakeArrayFromValuesNode()
        {
            ActiveArrayType = typeof(object[]);

            TypeInput = new ClassTypeNodeInput();
            TypeInput.Flags |= ENodeInputFlags.IsNodeConstant;
            TypeInput.ConstantTypeModifiedEvent += TypeInput_ConstantTypeModifiedEvent;
            AddInput(TypeInputName, TypeInput);

            updateInputsAndOutputs();
        }

		private string MakeInputName(int Index) { return $"{ElementBaseName}{Index}"; }

		private void TypeInput_ConstantTypeModifiedEvent(ClassTypeNodeInput input, Type newType)
        {
            updateInputsAndOutputs();
            PublishNodeModifiedNotification();
        }


		public bool AddInput()
		{
            add_array_input();
			PublishNodeModifiedNotification();
			return true;
		}
        protected void add_array_input()
        {
			Type elementType = TypeInput.ConstantValue;
            INodeInput newInput = FunctionNodeUtils.BuildInputNodeForType(elementType, null);
            AddInput(MakeInputName(NumArrayInputs), newInput);
			NumArrayInputs = NumArrayInputs + 1;
		}

		public bool RemoveInput(int SpecifiedIndex = -1)
		{
			if (NumArrayInputs <= 1) return false;
			Inputs.RemoveAt(Inputs.Count-1);
            NumArrayInputs--;
			PublishNodeModifiedNotification();
			return true;
		}


		protected virtual void updateInputsAndOutputs()
        {
			Type elementType = TypeInput.ConstantValue;
            bool bShowMakeDefaultsToggle = TypeUtils.IsNullableType(elementType)
                && (TypeUtils.FindParameterlessConstructorForType(elementType) != null);
            int NumCurrentStandardInputs = 1 + (AllocateObjectsInput != null ? 1 : 0);

			// remove existing array inputs
            // note: must handle on-load where NumArrayInputs is initialized but the Inputs don't exist
			while (Inputs.Count > NumCurrentStandardInputs)
			    Inputs.RemoveAt(Inputs.Count - 1);

            // add or remove make-defaults toggle
            if (bShowMakeDefaultsToggle == false && AllocateObjectsInput != null) {
                Inputs.RemoveAt(1);     // should always be at this slot
                AllocateObjectsInput = null;
            }
            if ( bShowMakeDefaultsToggle == true && AllocateObjectsInput == null )
            {
				AllocateObjectsInput = new StandardNodeInputWithConstant<bool>(true);
                AllocateObjectsInput.Flags |= ENodeInputFlags.IsNodeConstant;
				AddInput(AllocateObjectsInputName, AllocateObjectsInput);
			}

			// rebuild array inputs
            int want_num_inputs = NumArrayInputs;
            NumArrayInputs = 0;
            while (NumArrayInputs < want_num_inputs)
                add_array_input();

            // rebuild output
			Outputs.Clear();
            ActiveArrayType = elementType.MakeArrayType();
            AddOutput(ArrayOutputName, new StandardNodeOutputBase(ActiveArrayType));
        }


        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            Type elementType = TypeInput.ConstantValue;
			Func<object>? UseConstructor = TypeUtils.FindParameterlessConstructorForType(elementType);

            // create output array
			object? newObject = Activator.CreateInstance(ActiveArrayType, new object[] { NumArrayInputs });
            if (newObject == null)
                throw new Exception("MakeArrayFromValuesNode: could not create new array of type " + TypeUtils.TypeToString(ActiveArrayType));
            Array newArray = (Array)newObject;

            // populate output array from input pins
            bool bCreateDefaults = (AllocateObjectsInput != null) ? AllocateObjectsInput.ConstantValue : true;
			for (int i = 0; i < NumArrayInputs; ++i)
			{
                object? itemValue = DataIn.FindItemValueAsType(MakeInputName(i), elementType);
                //if ( itemValue == null && elementType.IsValueType == false && UseConstructor != null)
                if (itemValue == null && UseConstructor != null && bCreateDefaults)
                {
                    itemValue = UseConstructor();
                }
                if (itemValue != null)
                    newArray.SetValue(itemValue, i);
			}

            RequestedDataOut.SetItemValueChecked(ArrayOutputName, newArray);
        }




		public const string NumInputsKey = "NumInputs";
		public override void CollectCustomDataItems(out NodeCustomData? DataItems)
		{
            DataItems = new NodeCustomData()
                .AddIntItem(NumInputsKey, NumArrayInputs);
		}
		public override void RestoreCustomDataItems(NodeCustomData DataItems)
		{
            NumArrayInputs = DataItems.FindIntItemOrDefault(NumInputsKey, 0);
            updateInputsAndOutputs();
		}

	}





    [GraphNodeNamespace("Core.Array")]
	[GraphNodeUIName("Alloc Array")]
	public class AllocateArrayNode : NodeBase
	{
        public override string GetDefaultNodeName() { return "Alloc Array"; }

		public const string TypeInputName = "Type";
        public const string NumInputName = "Num";
		public const string AllocateObjectsInputName = "Alloc Objects";
		public const string ArrayOutputName = "Array";

		ClassTypeNodeInput TypeInput;
        StandardNodeInputWithConstant<int> NumInput;
        StandardNodeInputWithConstant<bool>? AllocateObjectsInput;
        Type ActiveArrayType;

        public AllocateArrayNode()
        {
            ActiveArrayType = typeof(object[]);

            TypeInput = new ClassTypeNodeInput();
            TypeInput.Flags |= ENodeInputFlags.IsNodeConstant;
            TypeInput.ConstantTypeModifiedEvent += TypeInput_ConstantTypeModifiedEvent;
            AddInput(TypeInputName, TypeInput);

            NumInput = new(1);
            AddInput(NumInputName, NumInput);

            updateInputsAndOutputs();
        }

		private void TypeInput_ConstantTypeModifiedEvent(ClassTypeNodeInput input, Type newType)
        {
            updateInputsAndOutputs();
            PublishNodeModifiedNotification();
        }


		protected virtual void updateInputsAndOutputs()
        {
			Type elementType = TypeInput.ConstantValue;
            bool bShowMakeDefaultsToggle = TypeUtils.IsNullableType(elementType)
                && (TypeUtils.FindParameterlessConstructorForType(elementType) != null);

            // add or remove make-defaults toggle
            if (bShowMakeDefaultsToggle == false && AllocateObjectsInput != null) {
                Inputs.RemoveAt(2);     // should always be at this slot
                AllocateObjectsInput = null;
            }
            if ( bShowMakeDefaultsToggle == true && AllocateObjectsInput == null )
            {
				AllocateObjectsInput = new StandardNodeInputWithConstant<bool>(false);
                AllocateObjectsInput.Flags |= ENodeInputFlags.IsNodeConstant;
				AddInput(AllocateObjectsInputName, AllocateObjectsInput);
			}

            // rebuild output
			Outputs.Clear();
            ActiveArrayType = elementType.MakeArrayType();
            AddOutput(ArrayOutputName, new StandardNodeOutputBase(ActiveArrayType));
        }


        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            Type elementType = TypeInput.ConstantValue;
			Func<object>? UseConstructor = TypeUtils.FindParameterlessConstructorForType(elementType);

            int NumToAlloc = 0;
            DataIn.FindItemValueStrict(NumInputName, ref NumToAlloc);

            // create output array
			object? newObject = Activator.CreateInstance(ActiveArrayType, new object[] { NumToAlloc });
            if (newObject == null)
                throw new Exception("AllocateArrayNode: could not create new array of type " + TypeUtils.TypeToString(ActiveArrayType));
            Array newArray = (Array)newObject;

            // populate output array from input pins
            bool bCreateDefaults = (AllocateObjectsInput != null) ? AllocateObjectsInput.ConstantValue : true;
			for (int i = 0; i < NumToAlloc; ++i)
			{
                if (UseConstructor != null && bCreateDefaults)
                {
                    newArray.SetValue(UseConstructor(), i);
                }
			}

            RequestedDataOut.SetItemValueChecked(ArrayOutputName, newArray);
        }
	}



}
