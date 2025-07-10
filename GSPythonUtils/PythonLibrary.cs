// Copyright Gradientspace Corp. All Rights Reserved.
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSPython
{
	public class PythonLibrary
	{
		public string SourceCode { get; protected set; } = "";
		public PythonFunction[] PythonFunctions;
		public string[] ClassNames;

		// scope created by parsing, that can be used to evaluate functions, eg PythonModuleScope.Eval("TestFunction1(7,3.0,True)");
		public PyModule? ModuleScope { get; protected set; }


		public PythonLibrary()
		{
			PythonFunctions = new PythonFunction[0];
			ClassNames = new string[0];
		}


		public static PythonLibrary ParseModuleFile(string PyFilePath)
		{
			string pythonScript = System.IO.File.ReadAllText(PyFilePath);
			return ParseModuleSource(pythonScript);
		}


		public static PythonLibrary ParseModuleSource(string sourceCode)
		{
			// todo ensure this is called inside using(py.GIL()) ... 

			List<PythonFunction> ParsedFunctions = new List<PythonFunction>();
			List<string> class_names = new List<string>();
			List<string> function_names = new List<string>();

			string useModuleName = "localModule";       // make an argument? infer from filename?

			//using (PyModule scopeModule = Py.CreateScope(useModuleName))
			PyModule scopeModule = Py.CreateScope(useModuleName);		// we are going to hold onto this after parsing is done, to allow exec
			{
				// load these two modules into the scope so they can be called by parsing code
				dynamic builtins_module = scopeModule.Import("builtins");
				dynamic inspect_module = scopeModule.Import("inspect");

				//PyDict? locals = new PyDict();
				PyDict? locals = null;
				scopeModule.Exec(sourceCode, locals);

				// this should be useModuleName
				PyObject moduleNameObj = scopeModule.GetAttr("__name__");
				string moduleName = moduleNameObj.ToString() ?? useModuleName;

				// iterate through all the names in the module and try to
				// find names that are classes or functions
				List<string> module_names = new List<string>(scopeModule.GetDynamicMemberNames());
				foreach (string name in module_names)
				{
					PyObject attr = scopeModule.GetAttr(name);

					if (inspect_module.isclass(attr))
						class_names.Add(name);
					else if (inspect_module.isfunction(attr))
						function_names.Add(name);
				}

				// iterate through functions and try to parse them
				foreach (string functionName in function_names)
				{
					if (scopeModule.HasAttr(functionName) == false) {       
						Debug.Assert(false);    // should never happen...
						continue;
					}

					// find the function's object and get its signature (this is a PyObject...)
					PyObject functionObject = scopeModule.GetAttr(functionName);
					dynamic signature = inspect_module.signature(functionObject);

					// parse the return type annotation
					dynamic return_annotation = signature.return_annotation;
					PythonSymbol returnType = PythonParsing.ParseAnnotation(PythonSymbol.RETURN, return_annotation.ToString(), scopeModule, inspect_module);

					// convert the signature.parameters OrderedDict into two lists because we can't talk directly to an OrderedDict
					dynamic param_keys = builtins_module.list(signature.parameters.keys());
					dynamic param_values = builtins_module.list(signature.parameters.values());
					int NumParams = builtins_module.len(param_keys);
					Debug.Assert(NumParams == (int)builtins_module.len(param_values));

					// parse the arguments
					List<PythonSymbol> Arguments = new List<PythonSymbol>();
					for (int i = 0; i < NumParams; ++i)
					{
						PyObject argname = param_keys[i];
						string? argname_string = argname.ToString();

						// TODO HOW TO GET FULL TYPE FOR ANNOTATION LIKE list[int] ??

						// parse the annotation for this parameter, if it exists
						dynamic argtype = param_values[i];
						string argtype_string = argtype.annotation.ToString();

						PythonSymbol variable = PythonParsing.ParseAnnotation(argname_string, argtype_string, scopeModule, inspect_module);

						// argtype.kind - positional or keyword...?

						Arguments.Add(variable);
					}

					PythonFunction PyFunction = new PythonFunction();
					PyFunction.FunctionName = functionName;
					PyFunction.ModuleName = moduleName;
					PyFunction.ReturnType = returnType;
					PyFunction.Arguments = Arguments.ToArray();
					PyFunction.FunctionObject = functionObject;
					ParsedFunctions.Add(PyFunction);
				}
			}


			return new PythonLibrary() { 
				SourceCode = sourceCode,
				PythonFunctions = ParsedFunctions.ToArray(),
				ClassNames = class_names.ToArray(),

				ModuleScope = scopeModule
			};

		}




		public PythonFunction? FindFunctionByName(string Name)
		{
			foreach (PythonFunction func in PythonFunctions)
				if (func.FunctionName == Name)
					return func;
			return null;
		}



		public PyObject? EvaluateFunction(PythonFunction function, PyObject[]? arguments)
		{
			if (PythonFunctions.Contains(function) == false)
				throw new Exception("function does not exist in this module...");

			if (arguments == null)
				throw new Exception("todo");

			if (ModuleScope == null)
				throw new Exception("todo");

			PyObject? returnValue = null;
			using (Py.GIL())
			{
				returnValue = ModuleScope.InvokeMethod(
					function.FunctionName,
					arguments);
			}

			return returnValue;
		}

	}
}
