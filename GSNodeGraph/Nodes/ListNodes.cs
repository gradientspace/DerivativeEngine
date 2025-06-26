using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{

    [GraphNodeFunctionLibrary("Gradientspace.List")]
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
