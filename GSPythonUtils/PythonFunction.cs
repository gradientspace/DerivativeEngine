using System;
using Python.Runtime;

namespace GSPython
{
	public class PythonFunction
	{
		public string ModuleName = "";
		public string FunctionName = "";
		public PythonSymbol ReturnType = PythonSymbol.DefaultReturnType;
		public PythonSymbol[]? Arguments = null;

		public PyObject? FunctionObject = null;

		public override string ToString()
		{
			return $"{ModuleName}.{FunctionName}()";
		}
	}
}
