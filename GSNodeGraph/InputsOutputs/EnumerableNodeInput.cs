// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
    /**
     * EnumerableNodeInput can be used for pins that take an IEnumerable<T> input,
     * eg like a ForEach node. The inner type T is defined at construction time.
     * 
     * a dynamic GraphDataType is returned, and matching is done via the IsTypeCompatible function
     */
    public class EnumerableNodeInput : INodeInput, IExtendedGraphDataTypeInfo
    {
        public Type ElementType = typeof(object);
        public ENodeInputFlags Flags { get; set; } = ENodeInputFlags.None;

        public EnumerableNodeInput()
        {
        }

        public EnumerableNodeInput(Type elementType)
        {
            ElementType = elementType;
        }

        public virtual bool IsTypeCompatible(in GraphDataType IncomingType)
        {
            bool bIsEnumerable = (TypeUtils.IsEnumerable(IncomingType.DataType));
            Type? ElementType = TypeUtils.GetEnumerableElementType(IncomingType.DataType);

            // TODO this should use smarter type resolution, eg be able to cast/etc
            return bIsEnumerable && ElementType == this.ElementType;
        }

        //! this is just an internal type used for the Type of EnumerableNodeInput. Nothing
        //! will directly match with the Type, but it's marked as a dynamic GraphDataType so
        //! the IsCompatibleWith function is what will actually be used (ie it's just a dummy type)
        public struct EnumerableDataType
        {
        }

        public virtual GraphDataType GetDataType()
        {
            return GraphDataType.MakeDynamic(typeof(EnumerableDataType), this);
        }

        public virtual ENodeInputFlags GetInputFlags() {
            return Flags;
        }

        public virtual (object?, bool) GetConstantValue() {
            return (null, false);
        }
        public virtual void SetConstantValue(object NewValue) {
            throw new NotImplementedException("DynamicNodeInputBase.SetConstantValue: constant value not supported on dynamic inputs");
        }

        // IExtendedGraphDataTypeInfo implementation
        public virtual bool IsCompatibleWith(in GraphDataType incomingType) {
            return IsTypeCompatible(incomingType);
        }
        public virtual string? GetCustomTypeString() {
            return "IEnumerable<" + TypeUtils.TypeToString(this.ElementType) + ">";
        }
    }
}
