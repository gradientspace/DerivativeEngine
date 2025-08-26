// Copyright Gradientspace Corp. All Rights Reserved.
using System;


namespace Gradientspace.NodeGraph
{
    // dummy type for missing pins
    internal class MissingInputOutputDataType : object
    {
    }

    // input pin type to be used for 'missing' inputs - dynamically spawned as needed?
    public class MissingNodeInput : INodeInput
    {
        public MissingNodeInput() { }

        public virtual GraphDataType GetDataType()
        {
            return new GraphDataType(typeof(MissingInputOutputDataType));
        }

        public virtual ENodeInputFlags GetInputFlags()
        {
            return ENodeInputFlags.None;
        }

        public virtual (object?, bool) GetConstantValue()
        {
            return (null, false);
        }

        public virtual void SetConstantValue(object NewValue)
        {
            throw new NotImplementedException("MissingNodeInput.SetConstantValue: no constant value storage available");
        }
    }


    // output pin type to be used for 'missing' inputs - dynamically spawned as needed?
    public class MissingNodeOutput : INodeOutput
    {
        public MissingNodeOutput() { }

        public virtual GraphDataType GetDataType()
        {
            return new GraphDataType(typeof(MissingInputOutputDataType));
        }
    }

}
