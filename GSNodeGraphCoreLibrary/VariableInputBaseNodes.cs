// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using System.Diagnostics;

namespace Gradientspace.NodeGraph
{


    /// <summary>
    /// VariableObjectsInputNode provides a variable number of object? inputs.
    /// The subclass implements BuildStandardInputsOutputs() and sets up it's non-variable
    /// input and output pins there.
    /// Subclass Evaluate() can use ConstructObjectArray() to fetch the object values
    /// </summary>
    [ClassHierarchyNode]
    public abstract class VariableObjectsInputNode : NodeBase, INode_VariableInputs
    {
        public int NumObjectInputs { get; set; } = 1;
        protected int NumStandardInputs = 0;

        public VariableObjectsInputNode()
        {
            BuildStandardInputsOutputs();
            NumStandardInputs = Inputs.Count;
            updateVariableInputs();
        }

        // subclass must implements this to set up node
        protected abstract void BuildStandardInputsOutputs();

        protected virtual string ElementBaseName { get { return "Object"; } }
        public virtual string MakeObjectInputName(int Index) { return $"{ElementBaseName}{Index}"; }

        public bool AddInput()
        {
            add_object_input();
            PublishNodeModifiedNotification();
            return true;
        }
        protected void add_object_input()
        {
            INodeInput newInput = new StandardNodeInputBase(typeof(object));
            AddInput(MakeObjectInputName(NumObjectInputs), newInput);
            NumObjectInputs = NumObjectInputs + 1;
        }

        public bool RemoveInput(int SpecifiedIndex = -1)
        {
            if (NumObjectInputs <= 1) return false;
            Inputs.RemoveAt(Inputs.Count-1);
            NumObjectInputs--;
            PublishNodeModifiedNotification();
            return true;
        }


        protected virtual void updateVariableInputs()
        {
            // remove existing variable inputs
            // note: must handle on-load where NumArrayInputs is initialized but the Inputs don't exist
            while (Inputs.Count > NumStandardInputs)
                Inputs.RemoveAt(Inputs.Count - 1);

            // rebuild array inputs
            int want_num_inputs = NumObjectInputs;
            NumObjectInputs = 0;
            while (NumObjectInputs < want_num_inputs)
                add_object_input();
        }


        protected object[] ConstructObjectArray(ref readonly NamedDataMap DataIn)
        {
            object?[] objects = new object[NumObjectInputs];
            for (int i = 0; i < NumObjectInputs; ++i) {
                objects[i] = DataIn.FindItemValue(MakeObjectInputName(i));
            }
            return objects;
        }


        public const string NumInputsKey = "NumInputs";
        public override void CollectCustomDataItems(out NodeCustomData? DataItems)
        {
            DataItems = new NodeCustomData()
                .AddIntItem(NumInputsKey, NumObjectInputs);
        }
        public override void RestoreCustomDataItems(NodeCustomData DataItems)
        {
            NumObjectInputs = DataItems.FindIntItemOrDefault(NumInputsKey, 0);
            updateVariableInputs();
        }
    }







    /// <summary>
    /// VariableStringsInputNode provides a variable number of string inputs.
    /// The subclass implements BuildStandardInputsOutputs() and sets up it's non-variable
    /// input and output pins there.
    /// Subclass Evaluate() can use ConstructStringArray() to fetch the string values
    /// </summary>
    [ClassHierarchyNode]
    public abstract class VariableStringsInputNode : NodeBase, INode_VariableInputs
    {
        public int NumStringInputs { get; set; } = 1;
        protected int NumStandardInputs = 0;

        public VariableStringsInputNode()
        {
            BuildStandardInputsOutputs();
            NumStandardInputs = Inputs.Count;
            updateVariableInputs();
        }

        // subclass must implements this to set up node
        protected abstract void BuildStandardInputsOutputs();

        protected virtual string ElementBaseName { get { return "Element"; } }
        public virtual string MakeStringInputName(int Index) { return $"{ElementBaseName}{Index}"; }

        public bool AddInput()
        {
            add_array_input();
            PublishNodeModifiedNotification();
            return true;
        }
        protected void add_array_input()
        {
            StandardStringNodeInput newInput = new StandardStringNodeInput();
            AddInput(MakeStringInputName(NumStringInputs), newInput);
            NumStringInputs = NumStringInputs + 1;
        }

        public bool RemoveInput(int SpecifiedIndex = -1)
        {
            if (NumStringInputs <= 1) return false;
            Inputs.RemoveAt(Inputs.Count-1);
            NumStringInputs--;
            PublishNodeModifiedNotification();
            return true;
        }


        protected virtual void updateVariableInputs()
        {
            // remove existing variable inputs
            // note: must handle on-load where NumArrayInputs is initialized but the Inputs don't exist
            while (Inputs.Count > NumStandardInputs)
                Inputs.RemoveAt(Inputs.Count - 1);

            // rebuild array inputs
            int want_num_inputs = NumStringInputs;
            NumStringInputs = 0;
            while (NumStringInputs < want_num_inputs)
                add_array_input();
        }


        protected string[] ConstructStringArray(ref readonly NamedDataMap DataIn)
        {
            string[] strings = new string[NumStringInputs];
            for (int i = 0; i < NumStringInputs; ++i) {
                string? itemValue = DataIn.FindItemValueAsType(MakeStringInputName(i), typeof(string)) as string;
                if (itemValue == null)
                    itemValue = "";
                strings[i] = itemValue;
            }
            return strings;
        }


        public const string NumInputsKey = "NumInputs";
        public override void CollectCustomDataItems(out NodeCustomData? DataItems)
        {
            DataItems = new NodeCustomData()
                .AddIntItem(NumInputsKey, NumStringInputs);
        }
        public override void RestoreCustomDataItems(NodeCustomData DataItems)
        {
            NumStringInputs = DataItems.FindIntItemOrDefault(NumInputsKey, 0);
            updateVariableInputs();
        }
    }



}