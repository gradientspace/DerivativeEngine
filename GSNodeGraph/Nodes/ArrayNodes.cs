// Copyright Gradientspace Corp. All Rights Reserved.
using System;
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

            AddInput(ArrayInputOutputName, new StandardNodeInputBase(ArrayType));
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
		public override void CollectCustomDataItems(out List<Tuple<string, object>>? DataItems)
		{
			DataItems = new List<Tuple<string, object>>();
			DataItems.Add(new(ArrayTypeKey, ArrayType.ToString()));
		}
		public override void RestoreCustomDataItems(List<Tuple<string, object>> DataItems)
		{
			Tuple<string, object>? ArrayTypeAsString = DataItems.Find((x) => { return x.Item1 == ArrayTypeKey; });
			if (ArrayTypeAsString == null)
				throw new Exception("ArrayElementNodeBase: ArrayType custom data is missing");
			string TypeName = ArrayTypeAsString.Item2.ToString()!;

			Type? arrayType = Type.GetType(TypeName);
			if (arrayType != null)
				setArrayType(arrayType);
			else
				throw new Exception("ArrayElementNodeBase: ArrayType " + TypeName + " Could not be found");
		}

	}



    [GraphNodeNamespace("Gradientspace.Array")]
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
                (newNode as GetArrayElementNode)?.Initialize(incomingType.DataType);
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


    [GraphNodeNamespace("Gradientspace.Array")]
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
                (newNode as SetArrayElementNode)?.Initialize(incomingType.DataType);
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




    [GraphNodeNamespace("Gradientspace.Array")]
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
                (newNode as GetArrayLengthNode)?.Initialize(incomingType.DataType);
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




    [GraphNodeNamespace("Gradientspace.Array")]
	[GraphNodeUIName("Make Array")]
	public class MakeArrayFromValuesNode : NodeBase, INode_VariableInputs
	{
        public override string GetDefaultNodeName() { return "Make Array"; }

		public static string ElementBaseName { get { return "Element"; } }
		public const string TypeInputName = "Type";
        public const string ArrayOutputName = "Array";

		public int NumArrayInputs { get; set; } = 2;

		ClassTypeNodeInput TypeInput;
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
			AddInput(MakeInputName(NumArrayInputs), new StandardNodeInputBase(elementType));
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
            // rebuild inputs
            Inputs.RemoveRange(1, Inputs.Count - 1);
            int want_num_inputs = NumArrayInputs;
            NumArrayInputs = 0;
            while (NumArrayInputs < want_num_inputs)
                add_array_input();

            // rebuild output
            Outputs.Clear();
            Type elementType = TypeInput.ConstantValue;
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
			for (int i = 0; i < NumArrayInputs; ++i)
			{
                object? itemValue = DataIn.FindItemValueAsType(MakeInputName(i), elementType);
				//if ( itemValue == null && elementType.IsValueType == false && UseConstructor != null)
				if (itemValue == null && UseConstructor != null)
				{
                    itemValue = UseConstructor();
                }
                if (itemValue != null)
                    newArray.SetValue(itemValue, i);
			}

            RequestedDataOut.SetItemValueChecked(ArrayOutputName, newArray);
        }




		public const string NumInputsKey = "NumInputs";
		public override void CollectCustomDataItems(out List<Tuple<string, object>>? DataItems)
		{
			DataItems = new List<Tuple<string, object>>();
			DataItems.Add(new(NumInputsKey, NumArrayInputs));
		}
		public override void RestoreCustomDataItems(List<Tuple<string, object>> DataItems)
		{
			Tuple<string, object>? NumInputsFound = DataItems.Find((x) => { return x.Item1 == NumInputsKey; });
			if (NumInputsFound != null) {
                NumArrayInputs = ((JsonElement)NumInputsFound.Item2).GetInt32();
                updateInputsAndOutputs();
			}
		}

	}



}
