// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using GSPython;

namespace Gradientspace.NodeGraph.PythonNodes
{
	// todo this should be cached based on the PythonType...no need to create unique instance for each input?
	internal class PythonType_Helper : IExtendedGraphDataTypeInfo
	{
		public GraphDataType DataType { get; init; }

		public PythonType_Helper(Type csharpType, GSPython.PythonType pyType)
		{
			DataType = new GraphDataType(csharpType, EGraphDataFormat.Python, pyType);
		}

		public PythonType PyType { get { return (PythonType)DataType.ExtendedType!; } }

		public virtual bool IsCompatibleWith(in GraphDataType incomingType)
		{
			if (incomingType.ExtendedTypeInfo != null && incomingType.ExtendedTypeInfo is PythonType_Helper )
			{
				PythonType_Helper incoming = (PythonType_Helper)incomingType.ExtendedTypeInfo;
				//return PyType.IsSameType(incoming.PyType);
				return incoming.PyType.IsCompatibleType(this.PyType);
			}

			return false;
		}
		public virtual string? GetCustomTypeString()
		{
			return PyType.GetTypeString() + " (python)";
		}
	}





	public class PythonNodeInputBase : INodeInput
	{
		Type CSharpType { get; set; }
		GSPython.PythonType PyType { get; set; }
		public ENodeInputFlags Flags { get; set; }
		PythonType_Helper? helper = null;

		public PythonNodeInputBase(PyFuncInputArgInfo argInfo)
		{
			CSharpType = argInfo.csharpType;
			PyType = argInfo.pythonType.GetPyType();
			helper = new PythonType_Helper(CSharpType, PyType);
		}

		public PythonNodeInputBase(PythonType pythonType)
		{
			CSharpType = typeof(object);
			PyType = pythonType;
			helper = new PythonType_Helper(CSharpType, PyType);
		}

		public virtual GraphDataType GetDataType()
		{
			return (helper != null) ? 
				  GraphDataType.MakeDynamic(CSharpType, EGraphDataFormat.Python, PyType, helper) 
				: new GraphDataType(CSharpType);
		}

		public virtual ENodeInputFlags GetInputFlags()
		{
			return Flags;
		}

		public virtual (object?, bool) GetConstantValue()
		{
			return (null, false);
		}

		public virtual void SetConstantValue(object NewValue)
		{
			throw new NotImplementedException("PythonNodeInputBase.SetConstantValue: no constant value storage available");
		}

	}



	public class PythonNodeOutputBase : INodeOutput
	{
		Type CSharpType { get; set; } = null!;
		GSPython.PythonType PyType { get; set; }
		PythonType_Helper? helper = null;

		public PythonNodeOutputBase(PyFuncOutputArgInfo argInfo)
		{
			initialize(argInfo.csharpType, argInfo.pythonType.GetPyType());
		}

		public PythonNodeOutputBase(Type csharpType, PythonType pythonType)
		{
			initialize(csharpType, pythonType);
		}

		private void initialize(Type csharpType, PythonType pythonType)
		{
			CSharpType = csharpType;
			PyType = pythonType;

			// if python type is directly convertible to standard C# type, skip this overhead
			if (PythonTypeUtil.IsStandardCSharpType(PyType.DataType) == false) {
				helper = new PythonType_Helper(CSharpType, PyType);
			}
		}


		public virtual GraphDataType GetDataType()
		{
			return (helper != null) ?
				  GraphDataType.MakeDynamic(CSharpType, EGraphDataFormat.Python, PyType, helper)
				: new GraphDataType(CSharpType);
		}
	}



}
