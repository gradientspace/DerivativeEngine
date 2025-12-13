// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GSPython;
using Python.Runtime;

namespace Gradientspace.NodeGraph.PythonNodes
{

	/**
	 * This is similar to DynamicNodeInput used for (eg) placeholder enumerable/array/etc inputs,
	 * however that uses Type and here we need to have a full GraphDataType so that we can access
	 * extended python types.
	 * 
	 * This is hardcoded for python tuple, but it could be generalized to any GraphDataType...
	 */
    public class TupleNodeInput : INodeInput, IDynamicNodeInputFilter, IExtendedGraphDataTypeInfo
    {
        public ENodeInputFlags Flags { get; set; }

        public TupleNodeInput() {
        }

		internal bool IsTypeCompatibleFunc(in GraphDataType IncomingType)
		{
			// match with any tuple input
			return IncomingType.DataFormat == EGraphDataFormat.Python
				&& IncomingType.ExtendedType != null
				&& ((PythonType)IncomingType.ExtendedType).DataType == EPythonTypes.Tuple;
		}

        public virtual bool IsTypeCompatible(in GraphDataType IncomingType)
        {
            return IsTypeCompatibleFunc(IncomingType); 
        }

        public virtual GraphDataType GetDataType() {
			// matches with any tuple
            return GraphDataType.MakeDynamic(typeof(object), EGraphDataFormat.Python, new PythonType(EPythonTypes.Tuple), this);
        }

        public virtual ENodeInputFlags GetInputFlags() {
            return Flags;
        }

        public virtual (object?, bool) GetConstantValue() {
            return (null, false);
        }
        public virtual void SetConstantValue(object NewValue) {
            throw new NotImplementedException("TupleNodeInput.SetConstantValue: constant value not supported on dynamic inputs");
        }

        // IExtendedGraphDataTypeInfo implementation
        public virtual bool IsCompatibleWith(in GraphDataType incomingType) {
            return IsTypeCompatibleFunc(incomingType);
        }
        public virtual string? GetCustomTypeString() {
            return "tuple [Python]";
        }
    }



	// this is the base class for nodes the user actually places in the graph, 
	// that have a single input that will allow any python tuple connection
	[ClassHierarchyNode]
	public abstract class PythonTuplePlaceholderNodeBase : PlaceholderNodeBase
	{
		public const string TupleInputName = "Tuple";

		protected TupleNodeInput PlaceholderTupleInput;

		public PythonTuplePlaceholderNodeBase()
		{
			PlaceholderTupleInput = new TupleNodeInput();
			AddInput(TupleInputName, PlaceholderTupleInput);
		}

		public override bool GetPlaceholderReplacementNodeInfo(
			string PlaceholderInputName, in GraphDataType incomingType,
			out Type ReplacementNodeClassType, out string ReplacementNodeInputName,
			out Action<INode, GraphDataType>? ReplacementNodeInitializer)
		{
			bool bCanReplace =
				(PlaceholderInputName == TupleInputName && PlaceholderTupleInput.IsCompatibleWith(incomingType));
			if (!bCanReplace)
			{
				ReplacementNodeClassType = typeof(void);
				ReplacementNodeInputName = "";
				ReplacementNodeInitializer = null;
				return false;
			}
			GetReplacementInfo(out ReplacementNodeClassType, out ReplacementNodeInputName, out ReplacementNodeInitializer);
			return true;
		}

		protected abstract void GetReplacementInfo(
			out Type ReplacementNodeType, out string ReplacementInputName,
			out Action<INode, GraphDataType>? ReplacementNodeInitializer);
	}


	/**
	 * placeholder Split node for python tuples - will expand to concrete SplitTupleNode
	 * with explicit input type and outputs
	 */
	[GraphNodeNamespace("Python")]
    [MappedNodeTypeName("Core.Python.SplitTuplePlaceholderNode")]
    public class SplitTuplePlaceholderNode : PythonTuplePlaceholderNodeBase
	{
		public override string GetDefaultNodeName() { return "Split PyTuple"; }

		protected override void GetReplacementInfo(
			out Type ReplacementNodeType, out string ReplacementInputName,
			out Action<INode, GraphDataType>? ReplacementNodeInitializer)
		{
			ReplacementNodeType = typeof(SplitTupleNode);
			ReplacementInputName = SplitTupleNode.TupleInputName;
			ReplacementNodeInitializer = (INode newNode, GraphDataType incomingType) => {
				(newNode as SplitTupleNode)?.Initialize(incomingType);
			};
		}
	}


	/**
	 *  SplitTupleNode is spawned via SplitTuplePlaceholderNode when user makes a connection
	 *  to the generic tuple input pin. Adds an output for each tuple element.
	 */
	[SystemNode]
	public class SplitTupleNode : NodeBase
	{
		public const string TupleInputName = "Tuple";

		public override string GetDefaultNodeName() { return "Split PyTuple"; }

		protected PythonType InputTupleType;

		public SplitTupleNode()
		{
			updateInputsOutputs();
		}

		public virtual void Initialize(GraphDataType dataType)
		{
			Debug.Assert(dataType.DataFormat == EGraphDataFormat.Python);
			Debug.Assert(dataType.ExtendedType != null && dataType.ExtendedType.GetType() == typeof(PythonType));
			setTupleType( (PythonType)dataType.ExtendedType);
		}

		protected virtual void setTupleType(PythonType tupleType)
		{
			InputTupleType = tupleType;
			updateInputsOutputs();
			PublishNodeModifiedNotification();
		}

		protected virtual void updateInputsOutputs()
		{
			Inputs.Clear();
			Outputs.Clear();

			AddInput(TupleInputName, new PythonNodeInputBase(InputTupleType));

			if (InputTupleType.NestedTypes != null) {
				int N = InputTupleType.NestedTypes.Length;
				for (int i = 0; i < N; ++i) {
					PythonType elemType = InputTupleType.NestedTypes[i];
					string paramName = "";
					INodeOutput newOutput = PythonFunctionNodeUtils.BuildOutputNodeForPythonType(elemType, ref paramName);
					paramName = $"{i}_{paramName}";
					// todo this may break due to non-unique type names?
					AddOutput(paramName, newOutput);
				}
			}
		}


		public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
		{
			int N = InputTupleType.NestedTypes!.Length;
			object? foundObj = DataIn.FindItemValueAsType(TupleInputName, typeof(object));
			PyObject? FoundTuple = (foundObj != null) ? foundObj as PyObject : null;
			if (FoundTuple == null)
				throw new Exception("SplitPyTupleNode: incoming object was not a PyObject tuple");

			using (Py.GIL())
			{
				var tuple = PyTuple.AsTuple(FoundTuple);
				for (int i = 0; i < N; ++i)
				{
					object tuple_elem = tuple[i];
					RequestedDataOut.SetItemValue(i, tuple_elem);
				}
			}
		}







		private const string TupleTypeString = "TupleType";
		public override void CollectCustomDataItems(out NodeCustomData? DataItems)
		{
            DataItems = new NodeCustomData()
                .AddItem(TupleTypeString, InputTupleType); // json-serialize the PythonType, should use PythonTypeJsonConverter
		}
		public override void RestoreCustomDataItems(NodeCustomData DataItems)
		{
            object? ElementTypeTuple = DataItems.FindItem(TupleTypeString);
            if (ElementTypeTuple == null)
                throw new Exception("SplitTupleNode: TupleType custom data is missing");

            if (ElementTypeTuple is JsonElement) {      // save/load case
                JsonElement theThing = (JsonElement)ElementTypeTuple;
                PythonType pyType = theThing.Deserialize<PythonType>();         // should use should use PythonTypeJsonConverter
                setTupleType(pyType);
            } else if (ElementTypeTuple is PythonType) {    // undo/redo case
                setTupleType((PythonType)ElementTypeTuple);
            } else
                throw new Exception("SplitTupleNode: TupleType custom data is invalid");
        }




    }

}
