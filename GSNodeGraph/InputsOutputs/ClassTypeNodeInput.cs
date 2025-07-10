// Copyright Gradientspace Corp. All Rights Reserved.
using System;

namespace Gradientspace.NodeGraph
{
    public class ClassTypeNodeInput : INodeInput
    {
        public Type ConstantValue { get; set; }

        public ENodeInputFlags Flags { get; set; }

        public ClassTypeNodeInput()
        {
            ConstantValue = typeof(object);
        }

        public virtual GraphDataType GetDataType()
        {
            return new GraphDataType(typeof(Type));
        }

        public virtual ENodeInputFlags GetInputFlags()
        {
            return Flags;
        }

        public virtual (object?, bool) GetConstantValue()
        {
            return (ConstantValue, true);
        }

        public virtual void SetConstantValue(object NewValue)
        {
            Type newType = (Type)NewValue;
            if ( ConstantValue != newType )
            {
                ConstantValue = newType;
                ConstantTypeModifiedEvent?.Invoke(this, newType);
            }
        }

        public delegate void OnConstantTypeModifiedHandler(ClassTypeNodeInput input, Type newType);
        public event OnConstantTypeModifiedHandler? ConstantTypeModifiedEvent;

    }
}
