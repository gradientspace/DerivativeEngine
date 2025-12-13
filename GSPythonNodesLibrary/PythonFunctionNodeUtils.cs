// Copyright Gradientspace Corp. All Rights Reserved.
using GSPython;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Gradientspace.NodeGraph.PythonNodes
{


    public static class PyTypeUtils
    {
        public static Type GetCSharpType(GSPython.EPythonTypes pythonType)
        {
            // void type??
            if (pythonType == EPythonTypes.None)
                return typeof(void);

            if (pythonType == EPythonTypes.Bool)
                return typeof(bool);
            else if (pythonType == EPythonTypes.Int)
                return typeof(int);
            else if (pythonType == EPythonTypes.Float)
                return typeof(double);
            else if (pythonType == EPythonTypes.Str)
                return typeof(string);

            // fall back to object...
            return typeof(object);
        }
    }


	public struct PyFuncInputArgInfo
	{
		public int argIndex;

		public GSPython.PythonSymbol pythonType;
		public Type csharpType;

		public string argName;

        // default value?

		public bool bIsOptional;        // for nullable arguments
	}

	public struct PyFuncOutputArgInfo
	{
		public int argIndex;

		public GSPython.PythonSymbol pythonType;
		public Type csharpType;

		public string argName;

		public bool bIsRefOutput;       // ref output is same object as an input, so does not need a new value like an out-output
	}


    public class PyFunctionNodeInfo
    {
        public PythonFunction pythonFunction;

        public string ReturnName;
		public PythonSymbol pythonReturnType;
		public Type csharpReturnType;

        public int NumArguments;

        public PyFuncInputArgInfo[] InputArguments;
        public PyFuncOutputArgInfo[] OutputArguments;

        public PyFunctionNodeInfo(PythonFunction function)
        {
			pythonFunction = function;

			// todo handle tuples, lists, etc
			pythonReturnType = pythonFunction.ReturnType;
            ReturnName = pythonReturnType.SymbolName;
            if (ReturnName == PythonSymbol.RETURN)
                ReturnName = "Value";
            csharpReturnType = PyTypeUtils.GetCSharpType(pythonReturnType.DataType);

            if (csharpReturnType != typeof(void))
            {
                string? returnName = ReturnName;
                if (returnName == null || returnName.Length == 0) {
                    returnName = "Value";      // default
                    // todo custom name markup or something?
                }
                ReturnName = returnName;
                //AddOutput(ReturnName, new StandardNodeOutputBase(ReturnType));
            }


            PythonSymbol[] parameters = pythonFunction.Arguments ?? Array.Empty<PythonSymbol>();
            NumArguments = parameters.Length;

            int NumInputArgs = 0, NumOutputArgs = 0;
            for (int i = 0; i < NumArguments; ++i)
            {
				// todo: identify in/out nodes? eg objects, lists, etc...
				bool bAddAsOutput = false; // (parameters[i].IsOut || parameters[i].ParameterType.IsByRef);
				if (bAddAsOutput)
					NumOutputArgs++;

				bool bAddAsInput = true;
                if (bAddAsInput)
                    NumInputArgs++;
            }

            InputArguments = new PyFuncInputArgInfo[NumInputArgs];
            OutputArguments = new PyFuncOutputArgInfo[NumOutputArgs];
            int InputI = 0, OutputI = 0;

            for (int i = 0; i < NumArguments; ++i)
            {
                PythonSymbol argInfo = parameters[i];
                Type csharpType = PyTypeUtils.GetCSharpType(argInfo.DataType);

                string? paramName = argInfo.SymbolName;
                if (paramName == null)
                    paramName = "arg" + i.ToString();

                bool bIsRefParam = false; // paramType.IsByRef && (paramInfo.IsOut == false);
                Type baseType = csharpType;     // (bIsRefParam || paramInfo.IsOut) ? paramType.GetElementType()! : paramType;        // strip off &
                Type? realType = System.Nullable.GetUnderlyingType(csharpType);
                bool bIsNullable = (realType != null);
                if (bIsRefParam && bIsNullable)
                    throw new Exception("PyFunctionNodeInfo.buildArguments(): nullable ref parameters are not currently supported.");

                bool bAddAsOutput = false; // (paramInfo.IsOut || bIsRefParam);
                bool bAddAsInput = true; // (paramInfo.IsOut == false);

                if (bAddAsOutput)
                {
                    PyFuncOutputArgInfo outputArg = new PyFuncOutputArgInfo();
					outputArg.argIndex = i;
					outputArg.argName = paramName;
					outputArg.pythonType = argInfo;
                    outputArg.csharpType = baseType;
                    outputArg.bIsRefOutput = bIsRefParam;
                    OutputArguments[OutputI++] = outputArg;
                }

                if (bAddAsInput)
                {
                    PyFuncInputArgInfo inputArg = new PyFuncInputArgInfo();
					inputArg.argIndex = i;
					inputArg.argName = paramName;
					inputArg.pythonType = argInfo;
					inputArg.bIsOptional = bIsRefParam;
					inputArg.csharpType = (bIsNullable) ? realType! : baseType; ;
                    InputArguments[InputI++] = inputArg;
                }
            }
        }

    }








	public class PythonFunctionNodeUtils
	{
        /**
         * Construct an array of input objects, one for each input argument in FunctionInfo.
         * In most cases the input value is looked up in the NamedDataMap
         * For 'out'-style arguments, construct a default value
         *    (note: this doesn't exist in Python...coped this from C# version....probably can remove)
         */
		public static object?[] ConstructFuncEvaluationArguments(in PyFunctionNodeInfo FunctionInfo, in NamedDataMap DataIn)
		{
			object?[] arguments = new object[FunctionInfo.NumArguments];
			for (int j = 0; j < FunctionInfo.InputArguments.Length; ++j)
			{
				ref PyFuncInputArgInfo inputArg = ref FunctionInfo.InputArguments[j];
				object? value = DataIn.FindItemValue(inputArg.argName);
				arguments[inputArg.argIndex] = value;
			}

			// for non-ref output arguments, construct a default object of the necessary type.
			// ref arguments should be been set above
			for (int j = 0; j < FunctionInfo.OutputArguments.Length; ++j)
			{
                Debug.Assert(false);        // this shouldn't occur in Python because it doesn't have this type of output?

				ref PyFuncOutputArgInfo outputArg = ref FunctionInfo.OutputArguments[j];
				if (outputArg.bIsRefOutput == false)
					arguments[outputArg.argIndex] = Activator.CreateInstance(outputArg.csharpType);
				else
					Debug.Assert(arguments[outputArg.argIndex] != null);
			}

			return arguments;
		}


		/**
         * Populate the output NamedDataMap with argument values returned by evaluating FunctionInfo (ie 'out' or 'ref' arguments).
         * The arguments parameter would have been constructed by ConstructFuncEvaluationArguments() and passed to the function evaluation.
         * This function finds the returned values and moves them to the NamedDataMap.
         */
		public static void ExtractFuncEvaluationOutputs(in PyFunctionNodeInfo FunctionInfo, in object?[] arguments, NamedDataMap RequestedDataOut)
		{
			for (int j = 0; j < FunctionInfo.OutputArguments.Length; ++j)
			{
				ref PyFuncOutputArgInfo outputArg = ref FunctionInfo.OutputArguments[j];
				int idx = outputArg.argIndex;
				if (arguments[idx] != null)
				{
					int outputIndex = RequestedDataOut.IndexOfItem(outputArg.argName);
					if (outputIndex >= 0)
						RequestedDataOut.SetItemValue(outputIndex, arguments[idx]!);
				}
			}
		}



        /**
         * Create an INodeInput for a specific argument of a function
         */
        public static INodeInput BuildInputNodeForFunctionArgument(PyFuncInputArgInfo argInfo, ref string paramName
			/*, PythonFunction sourceFunction, ParameterInfo paramInfo*/)
		{
            if (argInfo.bIsOptional)
            {
                return new StandardNullableNodeInput(argInfo.csharpType);
            }
            else if (argInfo.csharpType == typeof(bool)) {
                bool defaultValue = false;
                //if (paramInfo.DefaultValue != null && paramInfo.DefaultValue is bool)
                //    defaultValue = (bool)paramInfo.DefaultValue;
                return new StandardNodeInputBaseWithConstant(argInfo.csharpType, defaultValue);
            }
            else if (argInfo.csharpType == typeof(double)) {
                double defaultValue = 0.0;
                //if (paramInfo.DefaultValue != null && paramInfo.DefaultValue is double)
                //    defaultValue = (double)paramInfo.DefaultValue;
                return new StandardNodeInputBaseWithConstant(argInfo.csharpType, defaultValue);
            }
            else if (argInfo.csharpType == typeof(int)) {
                int defaultValue = 0;
                //if (paramInfo.DefaultValue != null && paramInfo.DefaultValue is int)
                //    defaultValue = (int)paramInfo.DefaultValue;
                return new StandardNodeInputBaseWithConstant(argInfo.csharpType, defaultValue);
            }
            else if (argInfo.csharpType == typeof(string)) {
                string defaultValue = "";
                //if (paramInfo.DefaultValue != null && paramInfo.DefaultValue is string)
                //    defaultValue = (string)paramInfo.DefaultValue;
                //if (foundParamInfo != null && foundParamInfo.DefaultValue != null && foundParamInfo.DefaultValue is string)
                //    defaultValue = (string)foundParamInfo.DefaultValue;
                return new StandardNodeInputBaseWithConstant(argInfo.csharpType, defaultValue);
            }

            return new PythonNodeInputBase(argInfo);
        }



        //! build a suitable output node for the given python type (will convert to standard C# types where possible)
        public static INodeOutput BuildOutputNodeForPythonType(PythonType PyType, ref string paramName)
		{
            paramName = PyType.GetTypeString();

			if (PyType.DataType == EPythonTypes.Bool) {
                return new StandardNodeOutput<bool>();
            } else if (PyType.DataType == EPythonTypes.Int) {
                return new StandardNodeOutput<int>();
            } else if (PyType.DataType == EPythonTypes.Float) {
				return new StandardNodeOutput<double>();
            } else if (PyType.DataType == EPythonTypes.Str) {
				return new StandardNodeOutput<string>();
			} else if (PyType.DataType == EPythonTypes.Complex) {
                return new StandardNodeOutput<Complex>();   // todo is this the right way to go?
            }

            return new PythonNodeOutputBase(typeof(object), PyType);
        }



	}
}
