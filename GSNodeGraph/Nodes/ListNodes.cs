// Copyright Gradientspace Corp. All Rights Reserved.
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
    public abstract class ListAccessPlaceholderNodeBase : PlaceholderNodeBase
    {
        public const string ListInputOutputName = "List";

        protected PlaceholderNodeInput PlaceholderInput;

        public ListAccessPlaceholderNodeBase()
        {
            PlaceholderInput = new PlaceholderNodeInput(DynamicInputDataTypeFilters.ListFilter);
            PlaceholderInput.CustomTypeString = "List<>";
            AddInput(ListInputOutputName, PlaceholderInput);
        }

        public override bool GetPlaceholderReplacementNodeInfo(
            string PlaceholderInputName, in GraphDataType incomingType,
            out Type ReplacementNodeClassType, out string ReplacementNodeInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            bool bCanReplace =
                (PlaceholderInputName == ListInputOutputName && PlaceholderInput.IsCompatibleWith(incomingType));
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
    public abstract class ListElementNodeBase : NodeBase
    {
        public const string ListInputOutputName = "List";
        public const string IndexInputName = "Index";

        protected Type ListType = typeof(IList);
        protected Type ListElementType = typeof(object);

        protected bool bEnableIndexInput = true;

        public ListElementNodeBase()
        {
            updateInputsOutputs();
        }

        public virtual void Initialize(Type listType)
        {
            setListType(listType);
        }

        protected virtual void setListType(Type listType)
        {
            if (listType == typeof(IList))
            {
                listType = typeof(IList);
                ListElementType = typeof(object);
            } 
            else
            {
				Debug.Assert(TypeUtils.IsList(listType));
				this.ListType = listType;
                Type? elemType = TypeUtils.GetEnumerableElementType(listType);
                Debug.Assert(elemType != null);
				ListElementType = elemType;
			}
			updateInputsOutputs();
            PublishNodeModifiedNotification();
        }

        protected virtual void updateInputsOutputs()
        {
            Inputs.Clear();
            Outputs.Clear();

            StandardNodeInputBase ListInput = new StandardNodeInputBase(ListType);
            ListInput.Flags |= ENodeInputFlags.IsInOut;
            AddInput(ListInputOutputName, ListInput);
            AddOutput(ListInputOutputName, new StandardNodeOutputBase(ListType));

            if (bEnableIndexInput)
                AddInput(IndexInputName, new StandardNodeInputWithConstant<int>(0));

            UpdateElementField();
        }

        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int ListOutputIndex = RequestedDataOut.IndexOfItem(ListInputOutputName);
            IList? FoundList = DataIn.FindItemValueAsType(ListInputOutputName, ListType) as IList;
            Debug.Assert(FoundList != null);

            int Index = 0;
            if (bEnableIndexInput)
                DataIn.FindItemValueStrict<int>(IndexInputName, ref Index);

            EvaluateInternal(in DataIn, RequestedDataOut, FoundList, Index);

            if (ListOutputIndex >= 0)
                RequestedDataOut.SetItemValue(ListOutputIndex, FoundList);
        }

        protected abstract void UpdateElementField();
        protected abstract void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, IList ListIn, int Index);



        // save/restore the List type

		public const string ListTypeKey = "ListType";
		public override void CollectCustomDataItems(out NodeCustomData? DataItems)
		{
            DataItems = new NodeCustomData().
                AddTypeItem(ListTypeKey, ListType);
		}
		public override void RestoreCustomDataItems(NodeCustomData DataItems)
		{
			Type? ListType = DataItems.FindTypeItemChecked(ListTypeKey, out string TypeName, "ListElementNodeBase: ListType custom data is missing");
            if (ListType != null)
				setListType(ListType);
			else
				throw new Exception("ListElementNodeBase: ListType " + TypeName + " Could not be found");
		}

	}




    [GraphNodeNamespace("Core.List")]
    public class GetListElementPlaceholderNode : ListAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "Get"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(GetListElementNode);
            ReplacementInputName = GetListElementNode.ListInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as GetListElementNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class GetListElementNode : ListElementNodeBase
    {
        public const string ValueOutputName = "Value";

        public override string GetDefaultNodeName() { return "Get"; }

        protected override void UpdateElementField() {
            AddOutput(ValueOutputName, new StandardNodeOutputBase(ListElementType));
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, IList ListIn, int Index) {
            object? ListElement = ListIn[Index];
            RequestedDataOut.SetItemValueChecked(ValueOutputName, ListElement!);
        }
    }





    [GraphNodeNamespace("Core.List")]
    public class AddListElementPlaceholderNode : ListAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "Add"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(AddListElementNode);
            ReplacementInputName = AddListElementNode.ListInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as AddListElementNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class AddListElementNode : ListElementNodeBase
    {
        public const string ValueInputName = "Value";

        public override string GetDefaultNodeName() { return "Add"; }

        public AddListElementNode() {
            bEnableIndexInput = false;
        }

        protected override void UpdateElementField() {
            AddInput(ValueInputName, FunctionNodeUtils.BuildInputNodeForType(ListElementType));
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, IList ListIn, int Index) {
            object? NewValue = DataIn.FindItemValueAsType(ValueInputName, ListElementType);
            Debug.Assert(NewValue != null);
            ListIn.Add(NewValue);
        }
    }




    [GraphNodeNamespace("Core.List")]
    public class InsertListElementPlaceholderNode : ListAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "Insert"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(InsertListElementNode);
            ReplacementInputName = InsertListElementNode.ListInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as InsertListElementNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class InsertListElementNode : ListElementNodeBase
    {
        public const string ValueInputName = "Value";

        public override string GetDefaultNodeName() { return "Insert"; }

        protected override void UpdateElementField() {
            AddInput(ValueInputName, FunctionNodeUtils.BuildInputNodeForType(ListElementType));
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, IList ListIn, int Index) {
            object? NewValue = DataIn.FindItemValueAsType(ValueInputName, ListElementType);
            Debug.Assert(NewValue != null);
            ListIn.Insert(Index, NewValue);
        }
    }




    [GraphNodeNamespace("Core.List")]
    public class RemoveListElementPlaceholderNode : ListAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "Remove"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(RemoveListElementNode);
            ReplacementInputName = RemoveListElementNode.ListInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as RemoveListElementNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class RemoveListElementNode : ListElementNodeBase
    {
        public const string ValueInputName = "Value";
        public const string FoundOutputName = "Found";

        public override string GetDefaultNodeName() { return "Remove"; }

        public RemoveListElementNode() : base() {
            bEnableIndexInput = false;
        }

        protected override void UpdateElementField() {
            AddInput(ValueInputName, FunctionNodeUtils.BuildInputNodeForType(ListElementType));
            AddOutput(FoundOutputName, new StandardNodeOutput<bool>());
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, IList ListIn, int Index) {
            object? Value = DataIn.FindItemValueAsType(ValueInputName, ListElementType);
            bool bFound = false;
            if ( ListIn.Contains(Value)) {
                ListIn.Remove(Value);
                bFound = true;
            }
            RequestedDataOut.SetItemValueChecked(FoundOutputName, bFound);
        }
    }





    [GraphNodeNamespace("Core.List")]
    public class RemoveAtListElementPlaceholderNode : ListAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "RemoveAt"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(RemoveAtListElementNode);
            ReplacementInputName = RemoveAtListElementNode.ListInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as RemoveAtListElementNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class RemoveAtListElementNode : ListElementNodeBase
    {
        public override string GetDefaultNodeName() { return "RemoveAt"; }

        protected override void UpdateElementField() { }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, IList ListIn, int Index) {
            ListIn.RemoveAt(Index);
        }
    }





    [GraphNodeNamespace("Core.List")]
    public class ContainsListElementPlaceholderNode : ListAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "Contains"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(ContainsListElementNode);
            ReplacementInputName = ContainsListElementNode.ListInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as ContainsListElementNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class ContainsListElementNode : ListElementNodeBase
    {
        public const string ValueInputName = "Value";
        public const string FoundOutputName = "Found";

        public override string GetDefaultNodeName() { return "Contains"; }

        public ContainsListElementNode() : base() {
            bEnableIndexInput = false;
        }

        protected override void UpdateElementField() {
            AddInput(ValueInputName, FunctionNodeUtils.BuildInputNodeForType(ListElementType));
            AddOutput(FoundOutputName, new StandardNodeOutput<bool>());
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, IList ListIn, int Index)
        {
            object? Value = DataIn.FindItemValueAsType(ValueInputName, ListElementType);
            bool bFound = ListIn.Contains(Value);
            RequestedDataOut.SetItemValueChecked(FoundOutputName, bFound);
        }
    }



    [GraphNodeNamespace("Core.List")]
    public class IndexOfListElementPlaceholderNode : ListAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "IndexOf"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(IndexOfListElementNode);
            ReplacementInputName = IndexOfListElementNode.ListInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as IndexOfListElementNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class IndexOfListElementNode : ListElementNodeBase
    {
        public const string ValueInputName = "Value";
        public const string IndexOutputName = "Index";
        public const string FoundOutputName = "Found";

        public override string GetDefaultNodeName() { return "IndexOf"; }

        public IndexOfListElementNode() : base() {
            bEnableIndexInput = false;
        }

        protected override void UpdateElementField() {
            AddInput(ValueInputName, FunctionNodeUtils.BuildInputNodeForType(ListElementType));
            AddOutput(IndexOutputName, new StandardNodeOutput<int>());
            AddOutput(FoundOutputName, new StandardNodeOutput<bool>());
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, IList ListIn, int Index)
        {
            object? Value = DataIn.FindItemValueAsType(ValueInputName, ListElementType);
            int idx = ListIn.IndexOf(Value);
            RequestedDataOut.SetItemValueChecked(IndexOutputName, idx);
            RequestedDataOut.SetItemValueChecked(FoundOutputName, idx == -1 ? false : true);
        }
    }




    [GraphNodeNamespace("Core.List")]
    public class GetListLengthPlaceholderNode : ListAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "Length"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(GetListLengthNode);
            ReplacementInputName = GetListLengthNode.ListInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as GetListLengthNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class GetListLengthNode : ListElementNodeBase
    {
        public const string LengthOutputName = "Length";
        public override string GetDefaultNodeName() { return "Length"; }

        public GetListLengthNode() { bEnableIndexInput = false; }

        protected override void UpdateElementField() {
            AddOutput(LengthOutputName, new StandardNodeOutput<int>());
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, IList ListIn, int Index) {
            RequestedDataOut.SetItemValueChecked(LengthOutputName, ListIn.Count);
        }
    }




    [GraphNodeNamespace("Core.List")]
    public class ClearListPlaceholderNode : ListAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "Clear"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(ClearListNode);
            ReplacementInputName = ClearListNode.ListInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as ClearListNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class ClearListNode : ListElementNodeBase
    {
        public override string GetDefaultNodeName() { return "Clear"; }

        public ClearListNode() { bEnableIndexInput = false; }

        protected override void UpdateElementField() {
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, IList ListIn, int Index) {
            ListIn.Clear();
        }
    }




    [GraphNodeNamespace("Core.List")]
    public class ListToStringPlaceholderNode : ListAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "List To String"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(ListToStringNode);
            ReplacementInputName = ListToStringNode.ListInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as ListToStringNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class ListToStringNode : ListElementNodeBase
    {
        public const string StringOutputName = "String";
        public override string GetDefaultNodeName() { return "List To String"; }

        public const string IndicesInputName = "Indices";
        protected StandardNodeInputWithConstant<bool> IndicesInput;

        public ListToStringNode() { 
            bEnableIndexInput = false;
            IndicesInput = new StandardNodeInputWithConstant<bool>(false);
            IndicesInput.Flags |= ENodeInputFlags.IsNodeConstant;
        }

        protected override void UpdateElementField()
        {
            AddInput(IndicesInputName, IndicesInput);
            AddOutput(StringOutputName, new StandardNodeOutput<string>());
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, IList ListIn, int Index)
        {
            bool bIndices = DataIn.FindStructValueOrDefault<bool>(IndicesInputName, false, false);

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append('(');
            for ( int i = 0; i < ListIn.Count; ++i) {
                if (bIndices)
                    stringBuilder.Append($"{i}:");
                stringBuilder.Append(ListIn[i]?.ToString() ?? "(null)");
                if ( i < ListIn.Count - 1 )
                    stringBuilder.Append(",");
            }
            stringBuilder.Append(')');

            RequestedDataOut.SetItemValueChecked(StringOutputName, stringBuilder.ToString());
        }
    }




    [GraphNodeNamespace("Core.List")]
    public class ListToArrayPlaceholderNode : ListAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() { return "List To Array"; }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(ListToArrayNode);
            ReplacementInputName = ListToArrayNode.ListInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as ListToArrayNode)?.Initialize(incomingType.CSType);
            };
        }
    }

    [SystemNode]
    public class ListToArrayNode : ListElementNodeBase
    {
        public const string ArrayOutputName = "Array";
        public override string GetDefaultNodeName() { return "List To Array"; }

        public ListToArrayNode() {
            bEnableIndexInput = false;
        }

        protected override void UpdateElementField()
        {
            Type ArrayType = ListElementType.MakeArrayType();
            AddOutput(ArrayOutputName, new StandardNodeOutputBase(ArrayType));
        }

        protected override void EvaluateInternal(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut, IList ListIn, int Index)
        {
            int N = ListIn.Count;
            Array result = Array.CreateInstance(ListElementType, N);
            ListIn.CopyTo(result, 0);
            RequestedDataOut.SetItemValueChecked(ArrayOutputName, result);
        }
    }





    [GraphNodeNamespace("Core.List")]
    public class CreateListNode : NodeBase
    {
        public override string GetDefaultNodeName() { return "Create List"; }

        public const string TypeInputName = "Type";
        public const string ListOutputName = "List";

        ClassTypeNodeInput TypeInput;
        Type ActiveListType;

        public CreateListNode()
        {
            ActiveListType = typeof(List<object>);

            TypeInput = new ClassTypeNodeInput();
            TypeInput.Flags |= ENodeInputFlags.IsNodeConstant;
            TypeInput.ConstantTypeModifiedEvent += TypeInput_ConstantTypeModifiedEvent;
            AddInput(TypeInputName, TypeInput);

            updateOutputs();
        }

        private void TypeInput_ConstantTypeModifiedEvent(ClassTypeNodeInput input, Type newType)
        {
            updateOutputs();
            PublishNodeModifiedNotification();
        }

        public virtual void Initialize(Type objectType)
        {
            TypeInput.SetConstantValue(objectType);
        }

        protected virtual void updateOutputs()
        {
            Outputs.Clear();

            Type activeType = TypeInput.ConstantValue;
            Type genericListType = typeof(List<>);
            ActiveListType = genericListType.MakeGenericType(activeType);

            AddOutput(ListOutputName, new StandardNodeOutputBase(ActiveListType));
        }

        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            object? newList = Activator.CreateInstance(ActiveListType);
            if (newList == null)
                throw new Exception("CreateListNode: could not create new list of type " + TypeUtils.TypeToString(ActiveListType) );

            RequestedDataOut.SetItemValueChecked(ListOutputName, newList);
        }
    }


}
