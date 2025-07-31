using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{

	/**
	 * Base class for nodes that define new variables in the graph.
	 * This allows things like static graph analysis to detect those variables
	 * and access their names/types
	 */
	[SystemNode]
	public abstract class DefineVariableBaseNode : NodeBase
	{
		public const string NameInputName = "Name";
		public const string VARIABLE_NAME_UNDEFINED = "::unknown::";

		// subclasses must implement...
		public abstract Type GetVariableType();

		public virtual string GetVariableName()
		{
			return NameInput?.ConstantValue ?? VARIABLE_NAME_UNDEFINED;
		}

		protected StandardStringNodeInput? NameInput = null;

		protected void AddNameInput()
		{
			// todo handle call multiple times?
			NameInput = new VariableNameNodeInput("(name)");
			// variable name needs to be node-constant to allow for static analysis...
			NameInput.Flags |= ENodeInputFlags.IsNodeConstant;
			AddInput(NameInputName, NameInput);
			// todo notification event
		}

	}


	[GraphNodeNamespace("Gradientspace.Core")]
	[GraphNodeUIName("New Global Variable")]
	public class CreateGlobalVariableNode : DefineVariableBaseNode
	{
		public override string GetDefaultNodeName() { return "New Global Variable"; }

		public const string TypeInputName = "Type";
		public const string AllocateObjectInputName = "Make New";
		public const string InitialValueInputName = "InitialValue";

		public const string OutputName = "Value";

		ClassTypeNodeInput TypeInput;
		StandardNodeInputWithConstant<bool>? AllocateObjectInput;
		INodeInput? InitialValueInput = null;

		public CreateGlobalVariableNode()
		{
			base.AddNameInput();

			Type initialType = typeof(bool);
			TypeInput = new ClassTypeNodeInput() { ConstantValue = initialType };
			TypeInput.Flags |= ENodeInputFlags.IsNodeConstant;
			TypeInput.ConstantTypeModifiedEvent += TypeInput_ConstantTypeModifiedEvent;
			AddInput(TypeInputName, TypeInput);

			updateInputsAndOutputs();
		}


		public override Type GetVariableType()
		{
			return TypeInput.ConstantValue;
		}

		private void TypeInput_ConstantTypeModifiedEvent(ClassTypeNodeInput input, Type newType)
		{
			updateInputsAndOutputs();
			PublishNodeModifiedNotification();
		}

		public virtual void Initialize(Type objectType)
		{
			TypeInput.SetConstantValue(objectType);
		}

		protected virtual void updateInputsAndOutputs()
		{
			Type variableType = GetVariableType();
			INodeInput? newInput = FunctionNodeUtils.BuildInputNodeForType(variableType, null);
			if (InitialValueInput == null) 
				AddInput(InitialValueInputName, newInput);
			else
				ReplaceInput(InitialValueInputName, newInput);
			InitialValueInput = newInput;

			bool bCanAllocate = TypeUtils.IsNullableType(variableType)
				&& (TypeUtils.FindParameterlessConstructorForType(variableType) != null);
			// todo should only show this if we don't have connection on input pin...
			if (bCanAllocate && AllocateObjectInput == null)
			{
				AllocateObjectInput = new StandardNodeInputWithConstant<bool>(true);
				AllocateObjectInput.Flags |= ENodeInputFlags.IsNodeConstant;
				AddInput(AllocateObjectInputName, AllocateObjectInput);
			} 
			else if (bCanAllocate == false && AllocateObjectInput != null)
			{
				RemoveInput(AllocateObjectInputName);
				AllocateObjectInput = null;
			}

			Outputs.Clear();
			AddOutput(OutputName, new StandardNodeOutputBase(variableType));
		}

		public override void Evaluate(EvaluationContext EvalContext,  ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
		{
			string VariableName = "";
			DataIn.FindItemValueStrict<string>(NameInputName, ref VariableName, true);      // throws if not found

			if (EvalContext.Variables.CanCreateVariable(VariableName, StandardVariables.GlobalScope) == false)
				throw new Exception($"CreateGlobalVariableNode: global variable named {VariableName} already exists!");

			Type variableType = GetVariableType();

			// try to find defined initial value - may come back null...
			object? initialValue = DataIn.FindItemValueAsType(InitialValueInputName, variableType);

			bool bAllocateIfMissing = (AllocateObjectInput != null) ? AllocateObjectInput.ConstantValue : true;
			if (initialValue == null && bAllocateIfMissing)
			{ 
				Func<object>? UseConstructor = TypeUtils.FindParameterlessConstructorForType(variableType);
				if (UseConstructor != null) {
					initialValue = UseConstructor();
				} else {
					// this works for types that have no parameterless constructor like float, double, etc
					// would it always do what the above does?
					initialValue = Activator.CreateInstance(variableType);
				}
			}

			// create variable in graph...
			EvalContext.Variables.CreateVariable(VariableName, variableType, initialValue, StandardVariables.GlobalScope);

			RequestedDataOut.SetItemValueOrNull_Checked(OutputName, initialValue);
		}
	}



	// base class for variable get/set nodes, that by default have
	// constant Name and Type inputs
	[SystemNode]
	public abstract class AccessVariableNode : NodeBase
	{
		public const string NameInputName = "Name";
		public const string TypeInputName = "Type";
		public const string OutputName = "Value";

		protected bool bMinimalDisplay = false;
		protected StandardStringNodeInput? NameInput = null;
		protected ClassTypeNodeInput? TypeInput;

		public AccessVariableNode()
		{
			AddNameInput();
			AddTypeInput();
			updateInputsAndOutputs();
		}

		public virtual void Initialize(Type VariableType)
		{
			TypeInput?.SetConstantValue(VariableType);
		}

		public virtual void Initialize(string VariableName, Type VariableType, bool bEnableMinimalDisplay)
		{
			NameInput?.SetConstantValue(VariableName);
			TypeInput?.SetConstantValue(VariableType);
			SetMinimalDisplay(bEnableMinimalDisplay);
		}

		public virtual void SetMinimalDisplay(bool bEnabled)
		{
			bMinimalDisplay = bEnabled;
			if (bMinimalDisplay)
			{
				if (TypeInput != null) TypeInput.Flags |= ENodeInputFlags.Hidden;
				if (NameInput != null) NameInput.Flags |= ENodeInputFlags.Hidden;
			} 
			else
			{
				if (TypeInput != null) TypeInput.Flags &= ~ENodeInputFlags.Hidden;
				if (NameInput != null) NameInput.Flags &= ~ENodeInputFlags.Hidden;
			}
		}

        public virtual void UpdateNameAndNotify(string NewName)
        {
            NameInput?.SetConstantValue(NewName);
            PublishNodeModifiedNotification();
        }


		public virtual string GetVariableName()
		{
			return NameInput?.ConstantValue ?? DefineVariableBaseNode.VARIABLE_NAME_UNDEFINED;
		}

		public virtual Type GetVariableType()
		{
			return TypeInput?.ConstantValue ?? typeof(object);
		}

		public virtual bool IsStaticallyDefined() 
		{
			return ((NameInput?.Flags & ENodeInputFlags.IsNodeConstant) != 0)
				&& ((TypeInput?.Flags & ENodeInputFlags.Hidden) != 0);
		}

		public void EnableGraphDefinedInputs()
		{
			// idea here is to clear the IsNodeConstant flags on Name and Type, which would
			// allow them to be defined via the graph (ie for dynamically-defined variables).
			// However this means they need to be ignored in static analysis, etc...needs some other work
			System.Diagnostics.Debug.Assert(false);
		}


		protected void AddNameInput()
		{
			// todo handle call multiple times?
			NameInput = new StandardStringNodeInput("(name)");
			// variable name needs to be node-constant to allow for static analysis...
			NameInput.Flags |= ENodeInputFlags.IsNodeConstant;
			AddInput(NameInputName, NameInput);
			// todo notification event
		}

		protected void AddTypeInput()
		{
			Type initialType = typeof(object);
			TypeInput = new ClassTypeNodeInput() { ConstantValue = initialType };
			TypeInput.Flags |= ENodeInputFlags.IsNodeConstant;
			TypeInput.ConstantTypeModifiedEvent += TypeInput_ConstantTypeModifiedEvent;
			AddInput(TypeInputName, TypeInput);
		}

		protected void TypeInput_ConstantTypeModifiedEvent(ClassTypeNodeInput input, Type newType)
		{
			updateInputsAndOutputs();
			PublishNodeModifiedNotification();
		}

		protected virtual void updateInputsAndOutputs()
		{
			Type variableType = GetVariableType();

			Outputs.Clear();
			AddOutput(OutputName, new StandardNodeOutputBase(variableType));
		}


		public const string MinimizedString = "bMinimalDisplay";
		public override void CollectCustomDataItems(out NodeCustomData? DataItems) {
            DataItems = new NodeCustomData().AddBoolItem(MinimizedString, bMinimalDisplay);
		}
		public override void RestoreCustomDataItems(NodeCustomData DataItems) {
            if (DataItems.FindBoolItemOrDefault(MinimizedString, false) == true)
                SetMinimalDisplay(true);
		}
	}



	[GraphNodeNamespace("Gradientspace.Core")]
	[GraphNodeUIName("Set Global Variable")]
	public class SetGlobalVariableNode : AccessVariableNode
	{
		public override string GetDefaultNodeName() { return "Set Global Variable"; }
		public override string GetCustomNodeName()
		{
			if (base.bMinimalDisplay)
				return $"Set {GetVariableName()}";
			return GetDefaultNodeName();
		}


		public const string ValueInputName = "Value";
		protected INodeInput? ValueInput = null;

		public SetGlobalVariableNode() : base()
		{
		}

		protected override void updateInputsAndOutputs()
		{
			base.updateInputsAndOutputs();

			// add value input
			Type variableType = GetVariableType();
			INodeInput? newInput = FunctionNodeUtils.BuildInputNodeForType(variableType, null);
			if (ValueInput == null) 
				AddInput(ValueInputName, newInput);
			else
				ReplaceInput(ValueInputName, newInput);
			ValueInput = newInput;
		}

		public override void Evaluate(EvaluationContext EvalContext,  ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
		{
			// note if this is node-constant we can just get it from GetVariableName()...
			string VariableName = "";
			DataIn.FindItemValueStrict<string>(NameInputName, ref VariableName, true);      // throws if not found

			// if not node-constant we would have to get it from DataIn here...
			Type variableType = GetVariableType();

			object? newValue = DataIn.FindItemValueAsType(ValueInputName, variableType);
			EvalContext.Variables.SetVariable(VariableName, newValue, StandardVariables.GlobalScope);
			RequestedDataOut.SetItemValueOrNull_Checked(OutputName, newValue);
		}
	}




	[GraphNodeNamespace("Gradientspace.Core")]
	[GraphNodeUIName("Get Global Variable")]
	public class GetGlobalVariableNode : AccessVariableNode
	{
		public override string GetDefaultNodeName() { return "Get Global Variable"; }
		public override string GetCustomNodeName()
		{
			if (base.bMinimalDisplay)
				return $"Get {GetVariableName()}";
			return GetDefaultNodeName();
		}

		public GetGlobalVariableNode() : base()
		{
		}

		public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
		{
			// note if this is node-constant we can just get it from GetVariableName()...
			string VariableName = "";
			DataIn.FindItemValueStrict<string>(NameInputName, ref VariableName, true);      // throws if not found

			object? curValue = EvalContext.Variables.GetVariable(VariableName, StandardVariables.GlobalScope);
			RequestedDataOut.SetItemValueOrNull_Checked(OutputName, curValue);
		}
	}



}
