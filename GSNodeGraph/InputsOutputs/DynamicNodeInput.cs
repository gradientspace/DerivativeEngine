// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
    public sealed class DynamicInputDataTypeFilters
    {
        private DynamicInputDataTypeFilters() { }

        public readonly static Func<Type, bool> AnyObjectFilter = (Type) => { return true; };
        public readonly static Func<Type, bool> ClassFilter = (Type t) => { return t.IsClass; };
        public readonly static Func<Type, bool> ArrayFilter = (Type t) => { return t.IsArray; };
        public readonly static Func<Type, bool> EnumerableFilter = (Type t) => { return TypeUtils.IsEnumerable(t); };

        public readonly static Func<Type, bool> ListFilter = (Type t) => { return TypeUtils.IsList(t); };
    }

    public interface IDynamicNodeInputFilter
    {
        bool IsTypeCompatible(in GraphDataType otherType);
    }

    public class DynamicNodeInput : INodeInput, IDynamicNodeInputFilter, IExtendedGraphDataTypeInfo
    {
        protected Func<Type, bool> IsTypeCompatibleFunc = DynamicInputDataTypeFilters.AnyObjectFilter;
        public ENodeInputFlags Flags { get; set; }
        public string? CustomTypeString { get; set; } = null;

        public DynamicNodeInput() {
        }

        public DynamicNodeInput(Func<Type, bool> TypeFilterFunc)
        {
            IsTypeCompatibleFunc = TypeFilterFunc;
        }

        public virtual bool IsTypeCompatible(in GraphDataType IncomingType)
        { 
            return IsTypeCompatibleFunc(IncomingType.CSType); 
        }

        public virtual GraphDataType GetDataType() {
            return GraphDataType.MakeDynamic(typeof(object), this);
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
            return IsTypeCompatibleFunc(incomingType.CSType);
        }
        public virtual string? GetCustomTypeString() {
            return CustomTypeString;
        }
    }
}
