// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Diagnostics;
using Python.Runtime;

namespace GSPython
{
	public static class PythonParsing
	{
		/**
		 * 
		 * @param annotation_string type annotation that comes from Python inspect module, eg inspect.signature(func).return_annotation or signature.parameters[]
		 * @param local_module optional PyModule for local/module scope
		 * @param inspect_module optional inspect module returned by Py.Import() / PyModule.Import(). More efficient than looking up every time.
		 * 
		 */
		public static PythonSymbol ParseAnnotation(
			string? symbol_name,
			string annotation_string,
			PyModule? local_module,
			dynamic? inspect_module)
		{
			PythonSymbol symbol;
			if (annotation_string.StartsWith("typing."))
			{
				throw new Exception("typing module unsupported for now...");
			} else if (annotation_string.StartsWith("<class \'"))
			{
				symbol = PythonParsing.ParseAnnotation_Class(symbol_name, annotation_string, local_module, inspect_module);
			} else if (annotation_string.StartsWith("(") && annotation_string.EndsWith(")"))
			{
				symbol = PythonParsing.ParseAnnotation_Tuple(symbol_name, annotation_string, local_module, inspect_module);
			} 
			else if (annotation_string.StartsWith("tuple["))
			{
				symbol = PythonParsing.ParseAnnotation_Tuple2(symbol_name, annotation_string, local_module, inspect_module);
			} 
			else if (annotation_string.StartsWith("list["))
			{
				symbol = PythonParsing.ParseAnnotation_List(symbol_name, annotation_string, local_module, inspect_module);
			} else
			{
				symbol = new PythonSymbol(symbol_name);
				TrySetBuiltinOrClassType(ref symbol, annotation_string, local_module, inspect_module);
			}
			return symbol;
		}



		public static PythonSymbol ParseAnnotation_Tuple(
			string? symbol_name,
			string annotation_string,
			PyModule? local_module,
			dynamic? inspect_module)
		{
			PythonSymbol variable = new PythonSymbol(symbol_name);

			if (inspect_module == null)
				inspect_module = (local_module == null) ? Py.Import("inspect") : local_module.Import("inspect");

			string tuple_string = annotation_string.Substring(1, annotation_string.Length - 2);

			List<PythonSymbol> tuple_symbols = new List<PythonSymbol>();

			int next_token_start = 0;
			int i = 0;
			int nest_count = 0;
			while (i < tuple_string.Length)
			{
				if (tuple_string[i] == '[' || tuple_string[i] == '(')
					nest_count++;
				else if (tuple_string[i] == ']' || tuple_string[i] == ')')
					nest_count--;
				else if (tuple_string[i] == ',' && nest_count == 0)
				{
					string token = tuple_string.Substring(next_token_start, i-next_token_start ).Trim();
					PythonSymbol token_variable = ParseAnnotation(PythonSymbol.ANONYMOUS, token, local_module, inspect_module);
					tuple_symbols.Add(token_variable);
					next_token_start = i+1;
				}

				i++;
			}

			string last_token = tuple_string.Substring(next_token_start, tuple_string.Length-next_token_start).Trim();
			PythonSymbol last_token_variable = ParseAnnotation(PythonSymbol.ANONYMOUS, last_token, local_module, inspect_module);
			tuple_symbols.Add(last_token_variable);

			variable.SetToTuple(tuple_symbols);

			return variable;
		}


		// parse annotation in tuple[x,x] style
		public static PythonSymbol ParseAnnotation_Tuple2(
			string? symbol_name,
			string annotation_string,
			PyModule? local_module,
			dynamic? inspect_module)
		{
			PythonSymbol variable = new PythonSymbol(symbol_name);

			if (inspect_module == null)
				inspect_module = (local_module == null) ? Py.Import("inspect") : local_module.Import("inspect");

			// strip of opening tuple[ and trailing ]
			string tuple_string = annotation_string.Substring(6, annotation_string.Length - 7);

			List<PythonSymbol> tuple_symbols = new List<PythonSymbol>();

			int next_token_start = 0;
			int i = 0;
			int nest_count = 0;
			while (i < tuple_string.Length)
			{
				if (tuple_string[i] == '[' || tuple_string[i] == '(')
					nest_count++;
				else if (tuple_string[i] == ']' || tuple_string[i] == ')')
					nest_count--;
				else if (tuple_string[i] == ',' && nest_count == 0)
				{
					string token = tuple_string.Substring(next_token_start, i - next_token_start).Trim();
					PythonSymbol token_variable = ParseAnnotation(PythonSymbol.ANONYMOUS, token, local_module, inspect_module);
					tuple_symbols.Add(token_variable);
					next_token_start = i + 1;
				}

				i++;
			}

			string last_token = tuple_string.Substring(next_token_start, tuple_string.Length - next_token_start).Trim();
			PythonSymbol last_token_variable = ParseAnnotation(PythonSymbol.ANONYMOUS, last_token, local_module, inspect_module);
			tuple_symbols.Add(last_token_variable);

			variable.SetToTuple(tuple_symbols);

			return variable;
		}



		// parse annotation in list[x] style
		public static PythonSymbol ParseAnnotation_List(
			string? symbol_name,
			string annotation_string,
			PyModule? local_module,
			dynamic? inspect_module)
		{
			PythonSymbol variable = new PythonSymbol(symbol_name);

			if (inspect_module == null)
				inspect_module = (local_module == null) ? Py.Import("inspect") : local_module.Import("inspect");

			// strip of opening list[ and trailing ]
			string list_string = annotation_string.Substring(5, annotation_string.Length - 6);
			PythonSymbol list_symbol = ParseAnnotation(PythonSymbol.ANONYMOUS, list_string, local_module, inspect_module);
			variable.SetToList(list_symbol);
			return variable;
		}



		public static PythonSymbol ParseAnnotation_Class(
			string? symbol_name,
			string annotation_string,
			PyModule? local_module,
			dynamic? inspect_module)
		{
			PythonSymbol variable = new PythonSymbol(symbol_name);

			if (inspect_module == null)
				inspect_module = (local_module == null) ? Py.Import("inspect") : local_module.Import("inspect");

			try
			{
				string typename = annotation_string.Substring(8);
				int last_tick = typename.LastIndexOf('\'');
				if (last_tick >= 0)
				{
					typename = typename.Substring(0, last_tick);
					TrySetBuiltinOrClassType(ref variable, typename, local_module, inspect_module);

					//if (variable.TrySetBuiltInType(typename) == false)
					//{
					//	string modName = typename.Substring(0, typename.LastIndexOf('.'));
					//	string className = typename.Substring(typename.LastIndexOf('.') + 1);

					//	if (local_module != null && local_module.HasAttr(className))
					//	{
					//		PyObject tmpResult = inspect_module.isclass(local_module.GetAttr(className));
					//		if (tmpResult.IsTrue())
					//			variable.SetToDefinedClass(modName, className);
					//	} 
					//	else
					//	{
					//		PyObject tempModule = PyModule.Import(modName);
					//		if (tempModule.HasAttr(className))
					//		{
					//			PyObject tmpResult = inspect_module.isclass(tempModule.GetAttr(className));
					//			if (tmpResult.IsTrue())
					//				variable.SetToDefinedClass(modName, className);
					//		}
					//	}
					//}
				}
			} catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
				System.Diagnostics.Debugger.Break();
				Debug.Assert(false);
				// ignore...
			}

			return variable;
		}





		internal static void TrySetBuiltinOrClassType(
			ref PythonSymbol Symbol,
			string typename,
			PyModule? local_module,
			dynamic inspect_module )
		{
			if (inspect_module == null)
			{
				Debug.Assert(false);
				return;
			}

			if (Symbol.TrySetBuiltInType(typename) == false)
			{
				string modName = typename.Substring(0, typename.LastIndexOf('.'));
				string className = typename.Substring(typename.LastIndexOf('.') + 1);

				if (local_module != null && local_module.HasAttr(className))
				{
					PyObject tmpResult = inspect_module.isclass(local_module.GetAttr(className));
					if (tmpResult.IsTrue())
						Symbol.SetToDefinedClass(modName, className);
				} else
				{
					PyObject tempModule = PyModule.Import(modName);
					if (tempModule.HasAttr(className))
					{
						PyObject tmpResult = inspect_module.isclass(tempModule.GetAttr(className));
						if (tmpResult.IsTrue())
							Symbol.SetToDefinedClass(modName, className);
					}
				}
			}
		}



	} // end PythonParsing
}
