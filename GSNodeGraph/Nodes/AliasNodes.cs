// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
    /// <summary>
    /// Reroute node just forwards input to output. This is intended to be used w/ a custom
    /// Reroute NodeWidget, ie to simplify graph.
    /// Doing it via an actual Node seems a bit expensive...however it avoids a bunch of
    /// complicated custom code that would be necessary otherwise.
    /// (may revisit in future)
    /// </summary>
    [SystemNode]
    [GraphNodeNamespace("Gradientspace.Core")]
    public class RerouteNode : NodeBase
    {
        public override string GetDefaultNodeName() { return "(Reroute)"; }

        public const string TypeInputName = "Type";
        public const string ValueInputName = "ValueIn";
        public const string ValueOutputName = "ValueOut";

        ClassTypeNodeInput TypeInput;
        INodeInput? ValueInput = null;
        INodeOutput? ValueOutput = null;

        public RerouteNode()
        {
            Type initialType = typeof(object);
            TypeInput = new ClassTypeNodeInput() { ConstantValue = initialType };
            TypeInput.Flags |= ENodeInputFlags.IsNodeConstant;
            TypeInput.Flags |= ENodeInputFlags.Hidden;
            TypeInput.ConstantTypeModifiedEvent += TypeInput_ConstantTypeModifiedEvent;
            AddInput(TypeInputName, TypeInput);

            // this is a special node...maybe need some other way to handle it?
            Flags |= ENodeFlags.Hidden;
            Flags |= ENodeFlags.IsPure;
        }

        public Type GetRerouteType()
        {
            return TypeInput.ConstantValue;
        }

        public virtual void Initialize(Type dataType)
        {
            TypeInput.SetConstantValue(dataType);
        }

        // note this is called on interactive change as well as restore-input-constant during graph load
        private void TypeInput_ConstantTypeModifiedEvent(ClassTypeNodeInput input, Type newType)
        {
            initializeInputsAndOutputs();
            PublishNodeModifiedNotification();
        }


        protected virtual void initializeInputsAndOutputs()
        {
            Type dataType = GetRerouteType();
            INodeInput? newInput = FunctionNodeUtils.BuildInputNodeForType(dataType, null);
            if (ValueInput == null)
                AddInput(ValueInputName, newInput);
            else
                ReplaceInput(ValueInputName, newInput);
            ValueInput = newInput;

            Outputs.Clear();
            ValueOutput = new StandardNodeOutputBase(dataType);
            AddOutput(ValueOutputName, ValueOutput);

            if (ValueInput is StandardNodeInputBase baseInput)
                baseInput.Flags |= ENodeInputFlags.HiddenLabel;
            if (ValueOutput is StandardNodeOutputBase baseOutput)
                baseOutput.Flags |= ENodeOutputFlags.HiddenLabel;
        }


        public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            // it's just a pass-through
            Type dataType = GetRerouteType();
            object? value = DataIn.FindItemValueAsType(ValueInputName, dataType);
            RequestedDataOut.SetItemValueOrNull_Checked(ValueOutputName, value);
        }

    }


    [SystemNode]
    [GraphNodeNamespace("Gradientspace.Core")]
    public abstract class AliasNodeBase : NodeBase
    {
        public const string TypeInputName = "Type";
        public const string NameInputName = "Name";
        public const string ALIAS_NAME_UNDEFINED = "::unknown::";

        protected ClassTypeNodeInput TypeInput;
        protected StandardStringNodeInput? NameInput = null;

        public AliasNodeBase()
        {
            Type initialType = typeof(object);
            TypeInput = new ClassTypeNodeInput() { ConstantValue = initialType };
            TypeInput.Flags |= ENodeInputFlags.IsNodeConstant;
            TypeInput.Flags |= ENodeInputFlags.Hidden;
            TypeInput.ConstantTypeModifiedEvent += TypeInput_ConstantTypeModifiedEvent;
            AddInput(TypeInputName, TypeInput);

            // todo handle call multiple times?
            NameInput = new VariableNameNodeInput("(name)");
            // variable name needs to be node-constant to allow for static analysis...
            NameInput.Flags |= ENodeInputFlags.IsNodeConstant;
            AddInput(NameInputName, NameInput);

            // this is a special node...maybe need some other way to handle it?
            Flags |= ENodeFlags.Hidden;
            Flags |= ENodeFlags.IsPure;
        }

        public Type GetAliasDataType()
        {
            return TypeInput.ConstantValue;
        }

        public string GetAliasName()
        {
            return NameInput?.ConstantValue ?? ALIAS_NAME_UNDEFINED;
        }

        public virtual void Initialize(Type dataType, string initialName)
        {
            TypeInput.SetConstantValue(dataType);
            NameInput?.SetConstantValue(initialName);
        }


        // note this is called on interactive change as well as restore-input-constant during graph load
        private void TypeInput_ConstantTypeModifiedEvent(ClassTypeNodeInput input, Type newType)
        {
            initializeInputOutput();
            PublishNodeModifiedNotification();
        }

        protected virtual void initializeInputOutput()
        {
            throw new NotImplementedException();
        }
    }


    [SystemNode]
    [GraphNodeNamespace("Gradientspace.Core")]
    public class CreateAliasNode : AliasNodeBase
    {
        public override string GetDefaultNodeName() { return "(Alias)"; }

        public const string ValueInputName = "ValueIn";
        INodeInput? ValueInput = null;

        public CreateAliasNode() : base()
        {
        }

        protected override void initializeInputOutput()
        {
            Type dataType = GetAliasDataType();
            // we don't want default values available on aliases!
            //INodeInput? newInput = FunctionNodeUtils.BuildInputNodeForType(dataType, null);
            INodeInput? newInput = new StandardNodeInputBase(dataType);
            if (ValueInput == null)
                AddInput(ValueInputName, newInput);
            else
                ReplaceInput(ValueInputName, newInput);
            ValueInput = newInput;

            if (ValueInput is StandardNodeInputBase baseInput)
                baseInput.Flags |= ENodeInputFlags.HiddenLabel;
        }
    }




    [SystemNode]
    [GraphNodeNamespace("Gradientspace.Core")]
    public class GetAliasNode : AliasNodeBase
    {
        public override string GetDefaultNodeName() { return "Get Alias"; }

        public const string ValueOutputName = "Value";
        INodeOutput? ValueOutput = null;

        public GetAliasNode() : base()
        {
            NameInput!.Flags |= ENodeInputFlags.Hidden;
        }

        public override string GetCustomNodeName()
        {
            return NameInput?.ConstantValue ?? "(get alias)";
        }

        protected override void initializeInputOutput()
        {
            Type dataType = GetAliasDataType();
            Outputs.Clear();
            ValueOutput = new StandardNodeOutputBase(dataType);
            AddOutput(ValueOutputName, ValueOutput);
            if (ValueOutput is StandardNodeOutputBase baseOutput)
                baseOutput.Flags |= ENodeOutputFlags.HiddenLabel;
        }
    }

}
