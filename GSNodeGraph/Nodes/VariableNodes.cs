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
			NameInput = new StandardStringNodeInput("(name)");
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





	[GraphNodeNamespace("Gradientspace.Core")]
	[GraphNodeUIName("Set Global Variable")]
	public class SetGlobalVariableNode : NodeBase
	{
		public override string GetDefaultNodeName() { return "Set Global Variable"; }

		public const string NameInputName = "Name";
		public const string TypeInputName = "Type";
		public const string ValueInputName = "Value";

		public const string OutputName = "Value";

		ClassTypeNodeInput TypeInput;
		INodeInput? InitialValueInput = null;

		public SetGlobalVariableNode()
		{
			AddInput(NameInputName, new StandardNodeInputBaseWithConstant(typeof(string), ""));

			Type initialType = typeof(object);
			TypeInput = new ClassTypeNodeInput() { ConstantValue = initialType };
			TypeInput.Flags |= ENodeInputFlags.IsNodeConstant;
			TypeInput.ConstantTypeModifiedEvent += TypeInput_ConstantTypeModifiedEvent;
			AddInput(TypeInputName, TypeInput);

			updateInputsAndOutputs();
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
			Type variableType = TypeInput.ConstantValue;
			INodeInput? newInput = FunctionNodeUtils.BuildInputNodeForType(variableType, null);
			if (InitialValueInput == null) 
				AddInput(ValueInputName, newInput);
			else
				ReplaceInput(ValueInputName, newInput);
			InitialValueInput = newInput;

			Outputs.Clear();
			Type activeType = TypeInput.ConstantValue;
			AddOutput(OutputName, new StandardNodeOutputBase(activeType));
		}

		public override void Evaluate(EvaluationContext EvalContext,  ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
		{
			string VariableName = "";
			DataIn.FindItemValueStrict<string>(NameInputName, ref VariableName, true);      // throws if not found

			Type variableType = TypeInput.ConstantValue;

			object? newValue = DataIn.FindItemValueAsType(ValueInputName, variableType);
			EvalContext.Variables.SetVariable(VariableName, newValue, StandardVariables.GlobalScope);
			RequestedDataOut.SetItemValueOrNull_Checked(OutputName, newValue);
		}
	}




	[GraphNodeNamespace("Gradientspace.Core")]
	[GraphNodeUIName("Get Global Variable")]
	public class GetGlobalVariableNode : NodeBase
	{
		public override string GetDefaultNodeName() { return "Get Global Variable"; }

		public const string NameInputName = "Name";
		public const string TypeInputName = "Type";

		public const string OutputName = "Value";

		ClassTypeNodeInput TypeInput;

		public GetGlobalVariableNode()
		{
			AddInput(NameInputName, new StandardNodeInputBaseWithConstant(typeof(string), ""));

			Type initialType = typeof(object);
			TypeInput = new ClassTypeNodeInput() { ConstantValue = initialType };
			TypeInput.Flags |= ENodeInputFlags.IsNodeConstant;
			TypeInput.ConstantTypeModifiedEvent += TypeInput_ConstantTypeModifiedEvent;
			AddInput(TypeInputName, TypeInput);

			updateInputsAndOutputs();
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
			Type variableType = TypeInput.ConstantValue;

			Outputs.Clear();
			Type activeType = TypeInput.ConstantValue;
			AddOutput(OutputName, new StandardNodeOutputBase(activeType));
		}

		public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
		{
			string VariableName = "";
			DataIn.FindItemValueStrict<string>(NameInputName, ref VariableName, true);      // throws if not found

			object? curValue = EvalContext.Variables.GetVariable(VariableName, StandardVariables.GlobalScope);
			RequestedDataOut.SetItemValueOrNull_Checked(OutputName, curValue);
		}
	}



}
