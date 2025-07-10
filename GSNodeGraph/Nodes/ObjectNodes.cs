// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{

    // not clear that we want to support this...
//#if false
    [GraphNodeNamespace("Gradientspace.Core")]
    public class CreateObjectNode : NodeBase
    {
        public override string GetDefaultNodeName() { return "Create Object"; }

        public const string TypeInputName = "Type";
        public const string ObjectOutputName = "Object";

        ClassTypeNodeInput TypeInput;

        public CreateObjectNode()
        {
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
            AddOutput(ObjectOutputName, new StandardNodeOutputBase(activeType));
        }

        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            Type activeType = TypeInput.ConstantValue;
            Func<object>? UseConstructor = TypeUtils.FindParameterlessConstructorForType(activeType);
            if (UseConstructor == null)
                throw new Exception("CreateListNode: cound not find default constructor for " + activeType.FullName);

            object newObject = UseConstructor();
            if (newObject == null)
                throw new Exception("CreateObjectNode: could not create new instance of type " + activeType.FullName);

            RequestedDataOut.SetItemValueChecked(ObjectOutputName, newObject);
        }
    }

//#endif





    [GraphNodeNamespace("Gradientspace.Array")]
    public class CreateArrayNode : NodeBase
    {
        public override string GetDefaultNodeName() { return "Create Array"; }

        public const string TypeInputName = "Type";
        public const string LengthInputName = "Count";
        public const string ArrayOutputName = "Array";

        ClassTypeNodeInput TypeInput;
        Type ActiveArrayType;

        public CreateArrayNode()
        {
            ActiveArrayType = typeof(object[]);

            TypeInput = new ClassTypeNodeInput();
            TypeInput.Flags |= ENodeInputFlags.IsNodeConstant;
            TypeInput.ConstantTypeModifiedEvent += TypeInput_ConstantTypeModifiedEvent;
            AddInput(TypeInputName, TypeInput);

            AddInput(LengthInputName, new StandardNodeInputWithConstant<int>(10));
            
            updateOutputs();
        }

        private void TypeInput_ConstantTypeModifiedEvent(ClassTypeNodeInput input, Type newType)
        {
            updateOutputs();
            PublishNodeModifiedNotification();
        }

        protected virtual void updateOutputs()
        {
            Outputs.Clear();

            Type elementType = TypeInput.ConstantValue;
            ActiveArrayType = elementType.MakeArrayType();

            AddOutput(ArrayOutputName, new StandardNodeOutputBase(ActiveArrayType));
        }

        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int Length = 1;
            DataIn.FindItemValueStrict<int>(LengthInputName, ref Length);

            Type elementType = TypeInput.ConstantValue;

            object? newObject = Activator.CreateInstance(ActiveArrayType, new object[] { Length });
            if (newObject == null)
                throw new Exception("CreateArrayNode: could not create new list of type " + TypeUtils.TypeToString(ActiveArrayType));
            Array newArray = (Array)newObject;

            if (elementType.IsValueType == false)
            {
                Func<object>? UseConstructor = TypeUtils.FindParameterlessConstructorForType(elementType);
                if (UseConstructor != null)
                {
                    for (int i = 0; i < Length; ++i)
                        newArray.SetValue(UseConstructor(), i);
                }
                // can print message here?
            }


            RequestedDataOut.SetItemValueChecked(ArrayOutputName, newArray);
        }
    }
}
