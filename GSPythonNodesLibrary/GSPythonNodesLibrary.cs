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



	[NodeFunctionLibrary("Python")]
    [MappedFunctionLibraryName("Core.Python")]
	public static class GSPythonConversionsLibrary
	{

		public static List<T> ConvertPyListToList<T>(object pyList)
		{
			PyObject pyObj = (PyObject)pyList;
			List<T>? result = null;
			using (Py.GIL()) {
				T[] values = pyObj.As<T[]>();
				result = [..values];
			}
			return result ?? new List<T>();
		}

        public static T[] ConvertPyListToArray<T>(object pyList)
        {
            PyObject pyObj = (PyObject)pyList;
            T[]? result = null;
            using (Py.GIL()) {
                result = pyObj.As<T[]>();
            }
            return result ?? [];
        }

        public static PyObject ConvertArrayToPyList<T>(T[] array)
		{
			using (Py.GIL())
			{
				int N = array.Length;
				PyList pyList = new PyList();
				for (int i = 0; i < N; ++i)
					pyList.Append(array[i].ToPython());
				return pyList;
			}
		}

        public static PyObject ConvertListToPyList<T>(List<T> list)
        {
            using (Py.GIL()) {
                int N = list.Count;
                PyList pyList = new PyList();
                for (int i = 0; i < N; ++i)
                    pyList.Append(list[i].ToPython());
                return pyList;
            }
        }



        private static void register_pylist_conversion<CSharpType>(DataConversionLibrary library, PythonType pyListType)
        {
            Type pyObjType = typeof(object);
            library.AddConversion(new DataTypeConverterLambda(
                new GraphDataType(pyObjType, EGraphDataFormat.Python, pyListType),
                new GraphDataType(typeof(List<CSharpType>)),
                (object o) => { return ConvertPyListToList<CSharpType>(o); }));
            library.AddConversion(new DataTypeConverterLambda(
                new GraphDataType(pyObjType, EGraphDataFormat.Python, pyListType),
                new GraphDataType(typeof(CSharpType[])),
                (object o) => { return ConvertPyListToArray<CSharpType>(o); }));
            library.AddConversion(new DataTypeConverterLambda(
                new GraphDataType(typeof(List<CSharpType>)),
                new GraphDataType(pyObjType, EGraphDataFormat.Python, pyListType),
                (object o) => { return ConvertListToPyList<CSharpType>((List<CSharpType>)o); }));
            library.AddConversion(new DataTypeConverterLambda(
                new GraphDataType(typeof(CSharpType[])),
                new GraphDataType(pyObjType, EGraphDataFormat.Python, pyListType),
                (object o) => { return ConvertArrayToPyList<CSharpType>((CSharpType[])o); }));

        }


        [GraphDataTypeRegisterFunction]
		public static void RegisterConversions(DataConversionLibrary library)
		{
            // register python/C# list/array converions for standard types
            // (todo actually do all of them)

            register_pylist_conversion<int>(library, PythonType.ListInt);
            register_pylist_conversion<string>(library, PythonType.ListStr);
            register_pylist_conversion<double>(library, PythonType.ListFloat);
            register_pylist_conversion<bool>(library, PythonType.ListBool);
        }


    }

}
