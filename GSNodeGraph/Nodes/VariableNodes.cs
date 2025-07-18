using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{


	[GraphNodeNamespace("Gradientspace.Core")]
	[GraphNodeUIName("New Global Variable")]
	public class CreateGlobalVariableNode : NodeBase
	{
		public override string GetDefaultNodeName() { return "New Global Variable"; }

		public const string NameInputName = "Name";
		public const string TypeInputName = "Type";
		public const string ObjectOutputName = "Object";

		ClassTypeNodeInput TypeInput;

		public CreateGlobalVariableNode()
		{
			AddInput(NameInputName, new StandardNodeInputBaseWithConstant(typeof(string), ""));

			TypeInput = new ClassTypeNodeInput() { ConstantValue = typeof(bool) };
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
			AddOutput(ObjectOutputName, new StandardNodeOutputBase(activeType));
		}

		public override void Evaluate(EvaluationContext EvalContext,  ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
		{
			string VariableName = "";
			DataIn.FindItemValueStrict<string>(NameInputName, ref VariableName, true);      // throws if not found

			if (EvalContext.Variables.CanCreateVariable(VariableName, StandardVariables.GlobalScope) == false)
				throw new Exception($"CreateGlobalVariableNode: global variable named {VariableName} already exists!");

			object? newObject = null;

			Type activeType = TypeInput.ConstantValue;
			Func<object>? UseConstructor = TypeUtils.FindParameterlessConstructorForType(activeType);
			if (UseConstructor != null) {
				newObject = UseConstructor();
			} else {
				// this works for types that have no parameterless constructor like float, double, etc
				// would it always do what the above does?
				newObject = Activator.CreateInstance(activeType);
			}

			if (newObject == null)
				throw new Exception("CreateGlobalVariableNode: could not create new instance of type " + activeType.FullName);

			// create variable in graph...
			EvalContext.Variables.CreateVariable(VariableName, activeType, newObject, StandardVariables.GlobalScope);

			RequestedDataOut.SetItemValueChecked(ObjectOutputName, newObject);
		}
	}



}
