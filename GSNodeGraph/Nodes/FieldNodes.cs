// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace Gradientspace.NodeGraph
{
    [ClassHierarchyNode]
    public abstract class FieldAccessPlaceholderNodeBase : PlaceholderNodeBase
    {
        public const string ObjectInputOutputName = "Object";

        protected PlaceholderNodeInput PlaceholderInput;

        public FieldAccessPlaceholderNodeBase()
        {
            PlaceholderInput = new PlaceholderNodeInput(DynamicInputDataTypeFilters.ClassFilter);
            AddInput(ObjectInputOutputName, PlaceholderInput);
        }

        public override bool GetPlaceholderReplacementNodeInfo(
            string PlaceholderInputName, in GraphDataType incomingType,
            out Type ReplacementNodeClassType, out string ReplacementNodeInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            bool bCanReplace =
                (PlaceholderInputName == ObjectInputOutputName && PlaceholderInput.IsCompatibleWith(incomingType));
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


    [GraphNodeNamespace("Gradientspace.Placeholders")]
    public class SetFieldPlaceholderNode : FieldAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() {
            return "Set Field";
        }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(SetFieldNode);
            ReplacementInputName = SetFieldNode.ObjectInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as SetFieldNode)?.Initialize(incomingType.DataType);
            };
        }
    }

    [GraphNodeNamespace("Gradientspace.Placeholders")]
    public class GetFieldPlaceholderNode : FieldAccessPlaceholderNodeBase
    {
        public override string GetDefaultNodeName() {
            return "Get Field";
        }

        protected override void GetReplacementInfo(
            out Type ReplacementNodeType, out string ReplacementInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer)
        {
            ReplacementNodeType = typeof(GetFieldNode);
            ReplacementInputName = GetFieldNode.ObjectInputOutputName;
            ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
                (newNode as GetFieldNode)?.Initialize(incomingType.DataType);
            };
        }
    }





    [ClassHierarchyNode]
    public abstract class FieldAccessNodeBase : NodeBase
    {
        public const string ObjectInputOutputName = "Object";
        public const string FieldInputName = "Field";

        protected Type ObjectType = typeof(object);
        protected bool bOnlyWritable = true;

        public FieldAccessNodeBase()
        {
            updateObjectInputsOutputs();
        }

        public virtual void Initialize(Type objectType)
        {
            setObjectType(objectType);
        }

        protected virtual void setObjectType(Type objectType)
        {
            ObjectType = objectType;
            updateFieldList();
            updateObjectInputsOutputs();
            if (SelectedFieldIndex >= 0)
                UpdateField();
            PublishNodeModifiedNotification();
        }

        protected struct FieldListItem
        {
            public string Label;
            // only one of the two below can be non-null
            public FieldInfo? Field;
            public PropertyInfo? Property;
            public FieldListItem(PropertyInfo property) { this.Property = property; Field = null; Label = property.Name; }
            public FieldListItem(FieldInfo field) { Property = null; this.Field = field; Label = field.Name; }
        }
        protected List<FieldListItem> AvailableFields = new List<FieldListItem>();
        protected EnumOptionSet? FieldEnumList = null;
        protected EnumOptionSetNodeInput? FieldListInput = null;
        protected int SelectedFieldIndex = -1;

        protected virtual void updateFieldList()
        {
            string CurrentSelectedField = "";
            SelectedFieldIndex = -1;
            if (FieldListInput != null) {
                CurrentSelectedField = ((EnumOptionItem)FieldListInput?.GetConstantValue().Item1!).ItemString ?? "";
                FieldListInput.OnSelectedValueChanged -= OnFieldListSelectionUpdated;
                FieldListInput = null;
            }

            AvailableFields.Clear();
            foreach (PropertyInfo property in ObjectType.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                bool bIsWritable = property.CanWrite && TypeUtils.IsWritableProperty(property);
                if (bOnlyWritable == false || bIsWritable)
                    AvailableFields.Add(new FieldListItem(property));
            }
            foreach (FieldInfo field in ObjectType.GetFields())
            {
                if (field.IsPublic == false || field.IsStatic || field.IsLiteral) continue;
                bool bIsWritable = (field.IsInitOnly == false);
                if (bOnlyWritable == false || bIsWritable)
                    AvailableFields.Add(new FieldListItem(field));
            }
            FieldEnumList = new EnumOptionSet(AvailableFields.Count, (int idx) => { return new Tuple<string, int, object?>( AvailableFields[idx].Label, idx, null); });

            // if there are not settable fields the node will stay in an invalid state
            if (FieldEnumList.Count == 0)
                return;

            SelectedFieldIndex = 0;
            if (FieldEnumList.FindIndexFromLabel(CurrentSelectedField, out SelectedFieldIndex) == false) 
                SelectedFieldIndex = 0;

            CurrentSelectedField = FieldEnumList[SelectedFieldIndex];
            FieldListInput = new EnumOptionSetNodeInput(FieldEnumList, CurrentSelectedField);
            FieldListInput.Flags |= ENodeInputFlags.IsNodeConstant;
            FieldListInput.OnSelectedValueChanged += OnFieldListSelectionUpdated;
        }

        protected virtual void updateObjectInputsOutputs()
        {
            Inputs.Clear();
            Outputs.Clear();

            AddInput(ObjectInputOutputName, new StandardNodeInputBase(ObjectType));
            AddOutput(ObjectInputOutputName, new StandardNodeOutputBase(ObjectType));

            if (ObjectType == typeof(object) || FieldListInput == null)
                return;

            AddInput(FieldInputName, FieldListInput);
        }

        protected void OnFieldListSelectionUpdated(EnumOptionSetNodeInput Input)
        {
            Debug.Assert(Input == FieldListInput && FieldEnumList != null);

            string CurrentSelectedField = ((EnumOptionItem)FieldListInput?.GetConstantValue().Item1!).ItemString ?? "";
            if ( FieldEnumList.FindIndexFromLabel(CurrentSelectedField, out int NewSelectedIndex) )
            {
                SelectedFieldIndex = NewSelectedIndex;
                UpdateField();
                PublishNodeModifiedNotification();
            }
        }

        protected virtual void UpdateField()
        {
            throw new NotImplementedException("FieldAccessNodeBase subclass must implement UpdateField");
        }
    }



    [SystemNode]
    public class GetFieldNode : FieldAccessNodeBase
    {
        public const string ValueOutputName = "Value";

        public GetFieldNode() : base() {
            bOnlyWritable = false;
        }

        public override string GetDefaultNodeName() {
            return "Get Field";
        }

        INodeOutput? CurrentValueOutput = null;

        protected override void UpdateField()
        {
            Debug.Assert(SelectedFieldIndex >= 0);
            FieldListItem SelectedFieldItem = AvailableFields[SelectedFieldIndex];

            INodeOutput? newOutput = null;
            if (SelectedFieldItem.Property != null)
            {
                Type propertyType = SelectedFieldItem.Property.PropertyType;
                newOutput = new StandardNodeOutputBase(propertyType);
            }
            else if (SelectedFieldItem.Field != null)
            {
                Type fieldType = SelectedFieldItem.Field.FieldType;
                newOutput = new StandardNodeOutputBase(fieldType);
            }
            Debug.Assert(newOutput != null);

            if (CurrentValueOutput == null)
                AddOutput(ValueOutputName, newOutput);
            else
                ReplaceOutput(ValueOutputName, newOutput);
            CurrentValueOutput = newOutput;
        }

        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int ObjOutputIndex = RequestedDataOut.IndexOfItem(ObjectInputOutputName);
            int FieldOutputIndex = RequestedDataOut.IndexOfItem(ValueOutputName);

            object? FoundObject = DataIn.FindItemValueAsType(ObjectInputOutputName, ObjectType);

            object? fieldValue = null;
            if (FoundObject != null && FieldOutputIndex >= 0)
            {
                Debug.Assert(SelectedFieldIndex >= 0);
                FieldListItem SelectedFieldItem = AvailableFields[SelectedFieldIndex];

                if (SelectedFieldItem.Field != null)
                    fieldValue = SelectedFieldItem.Field.GetValue(FoundObject);
                else if (SelectedFieldItem.Property != null)
                    fieldValue = SelectedFieldItem.Property.GetValue(FoundObject);
            }

            if (FoundObject != null)
                RequestedDataOut.SetItemValue(ObjOutputIndex, FoundObject);
            if (fieldValue != null)
                RequestedDataOut.SetItemValue(FieldOutputIndex, fieldValue);
        }
    }



    [SystemNode]
    public class SetFieldNode : FieldAccessNodeBase
    {
        public const string ValueInputName = "Value";

        public override string GetDefaultNodeName() {
            return "Set Field";
        }

        INodeInput? CurrentValueInput = null;

        // create input for current selected field/property
        // note that to determine a default value from the object definition, it is necessary to create 
        // an instance of the object, which seems like it is overkill here...
        protected override void UpdateField()
        {
            Debug.Assert(SelectedFieldIndex >= 0);
            FieldListItem SelectedFieldItem = AvailableFields[SelectedFieldIndex];

            INodeInput? newInput = null;
            if (SelectedFieldItem.Property != null)
            {
                Type propertyType = SelectedFieldItem.Property.PropertyType;
                newInput = FunctionNodeUtils.BuildInputNodeForType(propertyType, null);
            }
            else if (SelectedFieldItem.Field != null)
            {
                Type fieldType = SelectedFieldItem.Field.FieldType;
                newInput = FunctionNodeUtils.BuildInputNodeForType(fieldType, null);
            }
            Debug.Assert(newInput != null);

            if (CurrentValueInput == null)
                AddInput(ValueInputName, newInput);
            else
                ReplaceInput(ValueInputName, newInput);
            CurrentValueInput = newInput;
        }


        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ObjectInputOutputName);

            object? FoundObject = DataIn.FindItemValueAsType(ObjectInputOutputName, ObjectType);
            object? FoundValue = DataIn.FindItemValue(ValueInputName);
            Debug.Assert(FoundObject != null & FoundValue != null);     // should not be possible...

            if (FoundObject != null && FoundValue != null)
            {
                Debug.Assert(SelectedFieldIndex >= 0);
                FieldListItem SelectedFieldItem = AvailableFields[SelectedFieldIndex];

                if (SelectedFieldItem.Field != null)
                    SelectedFieldItem.Field.SetValue(FoundObject, FoundValue);
                else if (SelectedFieldItem.Property != null)
                    SelectedFieldItem.Property.SetValue(FoundObject, FoundValue);
            }

            if (FoundObject != null)
                RequestedDataOut.SetItemValue(OutputIndex, FoundObject);
        }
    }


}
