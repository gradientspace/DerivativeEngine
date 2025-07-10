// Copyright Gradientspace Corp. All Rights Reserved.
using GSPython;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.PythonNodes
{
	public static class GSPythonNodesLibrary
	{
		public static void Initialize()
		{
		}
	}



	[GraphNodeFunctionLibrary("PythonConversions")]
	public static class GSPythonConversionsLibrary
	{

		public static List<T> ConvertPyListToList<T>(object obj)
		{
			PyObject pyObj = (PyObject)obj;
			List<T>? result = null;
			using (Py.GIL()) {
				T[] values = pyObj.As<T[]>();
				result = new List<T>(values);
			}
			return result ?? new List<T>();
		}


		public static PyObject ConvertArrayToPyList<T>(T[] array)
		{
			using (Py.GIL())
			{
				int N = array.Length;
				PyList list = new PyList();
				for (int i = 0; i < N; ++i)
					list.Append(array[i].ToPython());
				return list;
			}
		}


		[GraphDataTypeRegisterFunction]
		public static void RegisterConversions(DataConversionLibrary library)
		{
			//Type pyObjType = typeof(PyObject);		// why isn't it this??
			Type pyObjType = typeof(object);

			// register python/C# list/array converions for standard types
			// (todo actually do all of them)

			library.AddConversion( new DataTypeConverterLambda(
				new GraphDataType(pyObjType, EGraphDataFormat.Python, PythonType.ListInt), 
				new GraphDataType(typeof(List<int>)),
				(object o) => { return ConvertPyListToList<int>(o); } ) );

			library.AddConversion(new DataTypeConverterLambda(
				new GraphDataType(pyObjType, EGraphDataFormat.Python, PythonType.ListStr),
				new GraphDataType(typeof(List<string>)),
				(object o) => { return ConvertPyListToList<string>(o); }));

			library.AddConversion(new DataTypeConverterLambda(
				new GraphDataType(typeof(string[])),
				new GraphDataType(pyObjType, EGraphDataFormat.Python, PythonType.ListStr),
				(object o) => { return ConvertArrayToPyList<string>((string[])o); }));
		}



	}

}
