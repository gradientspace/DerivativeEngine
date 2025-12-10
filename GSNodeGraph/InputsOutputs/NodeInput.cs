// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;


namespace Gradientspace.NodeGraph
{

    public class StandardNodeInputBase : INodeInput
    {
        Type ValueType { get; set; }
        public ENodeInputFlags Flags { get; set; }
        public string? DisplayName { get; set; } = null;

        public StandardNodeInputBase(Type valueType)
        {
            ValueType = valueType;
        }

        public virtual GraphDataType GetDataType()
        {
            return new GraphDataType(ValueType);
        }

        public virtual ENodeInputFlags GetInputFlags()
        {
            return Flags;
        }

        public virtual (object?,bool) GetConstantValue()
        {
            return (null,false);
        }

        public virtual void SetConstantValue(object NewValue)
        {
            throw new NotImplementedException("StandradNodeInputBase.SetConstantValue: no constant value storage available");
        }
    }



    public class StandardNullableNodeInput : StandardNodeInputBase
    {
        public StandardNullableNodeInput(Type valueType) : base(valueType)
        { }

        public override (object?, bool) GetConstantValue() {
            return (null, true);
        }

        public override void SetConstantValue(object NewValue) {
            throw new Exception("StandardNullableNodeInput: constant value on Nullable input not supported");
        }
    }


    public class StandardNodeInputBaseWithConstant : StandardNodeInputBase
    {
        public object ConstantValue { get; set; }

        public StandardNodeInputBaseWithConstant(Type valueType, object initialValue) : base(valueType)
        {
            ConstantValue = initialValue;
        }

        public override (object?,bool) GetConstantValue()
        {
            return (ConstantValue, true);
        }

        public override void SetConstantValue(object NewValue)
        {
            // this will throw if conversion is invalid...
            ConstantValue = NewValue;
        }
    }



    public class StandardNodeInput<T> : StandardNodeInputBase
    {
        public StandardNodeInput() : base(typeof(T))
        {
        }
    }



    public class StandardNodeInputWithConstant<T> : StandardNodeInput<T>
        where T : struct
    {
        public T ConstantValue;

        public override (object?, bool) GetConstantValue()
        {
            return (ConstantValue, true);
        }

        public override void SetConstantValue(object NewValue)
        {
            // this will throw if conversion is invalid...
            ConstantValue = (T)NewValue;
        }

        public StandardNodeInputWithConstant(T initialValue = default(T))
        {
            ConstantValue = initialValue;
        }
    }


	// StandardNodeInputWithConstant<T> does not work with string because it's not a struct
    // but has value semantics...
	public class StandardStringNodeInput : StandardNodeInput<string>
	{
		public string ConstantValue;

		public override (object?, bool) GetConstantValue()
		{
			return (ConstantValue, true);
		}

		public override void SetConstantValue(object NewValue)
		{
			ConstantValue = (string)NewValue;
		}

		public StandardStringNodeInput(string initialValue = "")
		{
			ConstantValue = initialValue;
		}
	}

    public class VariableNameNodeInput : StandardStringNodeInput
    {
        public VariableNameNodeInput(string initialValue = "") : base(initialValue) { }
    }


    // this is a special type so we can identify it in input pin and make a multiline text box
    public class TextBlockNodeInput : StandardStringNodeInput
    {
        public TextBlockNodeInput(string initialValue = "") : base(initialValue) { }

        public int UIWidthHint { get; set; } = -1;
    }

}
