// Copyright Gradientspace Corp. All Rights Reserved.
using System.Reflection;
using System.Diagnostics;

namespace Gradientspace.NodeGraph
{
    public struct FuncInputArgInfo
    {
        public ParameterInfo paramInfo;

        public int argIndex;
        public Type argType;
        public string argName;

        public bool bIsRefInput;        // ref inputs are mirrored as an output
        public bool bIsOptional;        // for nullable arguments
    }

    public struct FuncOutputArgInfo
    {
        public ParameterInfo paramInfo;

        public int argIndex;
        public Type argType;
        public string argName;

        public bool bIsRefOutput;       // ref output is same object as an input, so does not need a new value like an out-output
    }


    public class FunctionNodeInfo
    {
        public MethodInfo Function;

        public string ReturnName;
        public Type ReturnType;

        public int NumArguments;

        public FuncInputArgInfo[] InputArguments;
        public FuncOutputArgInfo[] OutputArguments;

        public FunctionNodeInfo(MethodInfo function)
        {
            Function = function;

            // todo handle tuples?
            ParameterInfo returnInfo = Function.ReturnParameter;
            ReturnType = returnInfo.ParameterType;
            ReturnName = "";

            if (ReturnType != typeof(void))
            {
                string? returnName = returnInfo.Name;
                if (returnName == null || returnName.Length == 0)
                {
                    returnName = "Result";      // default

                    NodeFunction? FunctionInfo = Function.GetCustomAttribute<NodeFunction>();
                    if ( FunctionInfo != null && FunctionInfo.ReturnName != null && FunctionInfo.ReturnName.Length > 0 ) {
                        returnName = FunctionInfo.ReturnName;
                    }
                    else 
                    { 
                        NodeReturnValue? ReturnValueInfo = Function.GetCustomAttribute<NodeReturnValue>();
                        if (ReturnValueInfo != null) {
                            if (ReturnValueInfo.DisplayName.Length > 0)
                                returnName = ReturnValueInfo.DisplayName;
                        }
                    }
                }
                ReturnName = returnName;
                //AddOutput(ReturnName, new StandardNodeOutputBase(ReturnType));
            }


            ParameterInfo[] parameters = Function.GetParameters();
            NumArguments = parameters.Length;

            int NumInputArgs = 0, NumOutputArgs = 0;
            for (int i = 0; i < NumArguments; ++i)
            {
                bool bAddAsOutput = (parameters[i].IsOut || parameters[i].ParameterType.IsByRef);
                if (bAddAsOutput)
                    NumOutputArgs++;

                bool bAddAsInput = (parameters[i].IsOut == false);
                if (bAddAsInput)
                    NumInputArgs++;
            }

            InputArguments = new FuncInputArgInfo[NumInputArgs];
            OutputArguments = new FuncOutputArgInfo[NumOutputArgs];
            int InputI = 0, OutputI = 0;

            for (int i = 0; i < NumArguments; ++i)
            {
                ParameterInfo paramInfo = parameters[i];
                Type paramType = paramInfo.ParameterType;
                string? paramName = paramInfo.Name;
                if (paramName == null)
                    paramName = "arg" + i.ToString();

                bool bIsRefParam = paramType.IsByRef && (paramInfo.IsOut == false);
                Type baseType = (bIsRefParam || paramInfo.IsOut) ? paramType.GetElementType()! : paramType;        // strip off &
                Type? realType = System.Nullable.GetUnderlyingType(paramType);
                bool bIsNullable = (realType != null);      // todo can use TypeUtils.IsNullableType(paramType) here? but need to check realType behavior...
                if (bIsRefParam && bIsNullable)
                    throw new Exception("LibraryFunctionNode.buildArguments(): nullable ref parameters are not currently supported.");

                bool bAddAsOutput = (paramInfo.IsOut || bIsRefParam);
                bool bAddAsInput = (paramInfo.IsOut == false);

                if (bAddAsOutput)
                {
                    FuncOutputArgInfo outputArg = new FuncOutputArgInfo();
                    outputArg.paramInfo = paramInfo;
                    outputArg.argIndex = i;
                    outputArg.argName = paramName;
                    outputArg.argType = baseType;
                    outputArg.bIsRefOutput = bIsRefParam;
                    OutputArguments[OutputI++] = outputArg;
                    //INodeOutput nodeOutput = new StandardNodeOutputBase(baseType);
                    //AddOutput(paramName, nodeOutput);
                }

                if (bAddAsInput)
                {
                    FuncInputArgInfo inputArg = new FuncInputArgInfo();
                    inputArg.paramInfo = paramInfo;
                    inputArg.argIndex = i;
                    inputArg.argName = paramName;
                    inputArg.bIsOptional = bIsNullable;
                    inputArg.bIsRefInput = bIsRefParam;
                    inputArg.argType = (bIsNullable) ? realType! : baseType;
                    InputArguments[InputI++] = inputArg;
                    //INodeInput nodeInput = LibraryFunctionBuilderUtils.BuildInputNodeForType(inputArg, ref paramName, Function, paramInfo);
                    //AddInput(paramName, nodeInput);
                }
            }
        }

    }



    public static class FunctionNodeUtils
    {
        public static object?[] ConstructFuncEvaluationArguments(in FunctionNodeInfo FunctionInfo, in NamedDataMap DataIn)
        {
            object?[] arguments = new object[FunctionInfo.NumArguments];
            for (int j = 0; j < FunctionInfo.InputArguments.Length; ++j)
            {
                ref FuncInputArgInfo inputArg = ref FunctionInfo.InputArguments[j];
                object? value = DataIn.FindItemValue(inputArg.argName);
                arguments[inputArg.argIndex] = value;
            }

            // for non-ref output arguments, construct a default object of the necessary type.
            // ref arguments should be been set above
            for (int j = 0; j < FunctionInfo.OutputArguments.Length; ++j)
            {
                ref FuncOutputArgInfo outputArg = ref FunctionInfo.OutputArguments[j];
                if (outputArg.bIsRefOutput == false) 
                {
                    // for out args, try to create an instance? Not clear we actually need to do this,
                    // as the function won't compile unless it defines a value for the out argument...
                    arguments[outputArg.argIndex] = null;
                    Func<object>? Constructor = TypeUtils.FindParameterlessConstructorForType(outputArg.argType);
                    if (Constructor != null)
                        arguments[outputArg.argIndex] = Constructor();

                } else
                    Debug.Assert(arguments[outputArg.argIndex] != null);
            }

            return arguments;
        }


        public static void ExtractFuncEvaluationOutputs(in FunctionNodeInfo FunctionInfo, in object?[] arguments, NamedDataMap RequestedDataOut)
        {
            for (int j = 0; j < FunctionInfo.OutputArguments.Length; ++j)
            {
                ref FuncOutputArgInfo outputArg = ref FunctionInfo.OutputArguments[j];
                int idx = outputArg.argIndex;
                if (arguments[idx] != null)
                {
                    int outputIndex = RequestedDataOut.IndexOfItem(outputArg.argName);
                    if (outputIndex >= 0)
                        RequestedDataOut.SetItemValue(outputIndex, arguments[idx]!);
                }
            }
        }




        public static StandardNodeInputBase BuildInputNodeForMethodArgument(FuncInputArgInfo argInfo, ref string paramName, MethodInfo sourceFunction, ParameterInfo paramInfo)
        {
            // todo cache this and pass to this function
            NodeParameter? foundParamInfo = null;
            foreach ( NodeParameter nodeParamInfo in sourceFunction.GetCustomAttributes<NodeParameter>() )
            {
                if (nodeParamInfo.ArgumentName == paramInfo.Name)
                {
                    foundParamInfo = nodeParamInfo;
                    break;
                }
            }

            StandardNodeInputBase? NewInput = null;

            if (argInfo.bIsOptional)
            {
                NewInput = new StandardNullableNodeInput(argInfo.argType);
            }
            else if (argInfo.argType == typeof(bool)) {
                bool defaultValue = false;
                if (paramInfo.DefaultValue != null && paramInfo.DefaultValue is bool)
                    defaultValue = (bool)paramInfo.DefaultValue;
                NewInput = new StandardNodeInputBaseWithConstant(argInfo.argType, defaultValue);
            }
            else if (argInfo.argType == typeof(float)) {
                float defaultValue = 0.0f;
                if (paramInfo.DefaultValue != null && paramInfo.DefaultValue is float)
                    defaultValue = (float)paramInfo.DefaultValue;
                NewInput = new StandardNodeInputBaseWithConstant(argInfo.argType, defaultValue);
            }
            else if (argInfo.argType == typeof(double)) {
                double defaultValue = 0.0;
                if (paramInfo.DefaultValue != null && paramInfo.DefaultValue is double)
                    defaultValue = (double)paramInfo.DefaultValue;
                NewInput = new StandardNodeInputBaseWithConstant(argInfo.argType, defaultValue);
            }
            else if (argInfo.argType == typeof(int)) {
                int defaultValue = 0;
                if (paramInfo.DefaultValue != null && paramInfo.DefaultValue is int)
                    defaultValue = (int)paramInfo.DefaultValue;
                NewInput = new StandardNodeInputBaseWithConstant(argInfo.argType, defaultValue);
            }
            else if (argInfo.argType == typeof(short)) {
                short defaultValue = 0;
                if (paramInfo.DefaultValue != null && paramInfo.DefaultValue is short)
                    defaultValue = (short)paramInfo.DefaultValue;
                NewInput = new StandardNodeInputBaseWithConstant(argInfo.argType, defaultValue);
            }
            else if (argInfo.argType == typeof(long)) {
                long defaultValue = 0;
                if (paramInfo.DefaultValue != null && paramInfo.DefaultValue is long)
                    defaultValue = (long)paramInfo.DefaultValue;
                NewInput = new StandardNodeInputBaseWithConstant(argInfo.argType, defaultValue);
            }
            else if (argInfo.argType == typeof(string)) {
                string defaultValue = "";
                if (paramInfo.DefaultValue != null && paramInfo.DefaultValue is string)
                    defaultValue = (string)paramInfo.DefaultValue;
                if (foundParamInfo != null && foundParamInfo.DefaultValue != null && foundParamInfo.DefaultValue is string)
                    defaultValue = (string)foundParamInfo.DefaultValue;
                NewInput = new StandardNodeInputBaseWithConstant(argInfo.argType, defaultValue);
            }
            else if (argInfo.argType.IsEnum)
            {
                //this doesn't work properly if 0 is not one of the enum values
                //object? defaultValue = Activator.CreateInstance(paramType);
                object? defaultValue = System.Enum.GetValues(argInfo.argType).GetValue(0);
                if (paramInfo.DefaultValue != null && paramInfo.DefaultValue.GetType() == argInfo.argType)
                    defaultValue = paramInfo.DefaultValue;
                if (foundParamInfo != null && foundParamInfo.DefaultValue != null && foundParamInfo.DefaultValue.GetType().IsEnum)
                    defaultValue = foundParamInfo.DefaultValue;
                if (defaultValue == null)
                    NewInput = new StandardNodeInputBase(argInfo.argType);
                else
                    NewInput = new StandardNodeInputBaseWithConstant(argInfo.argType, defaultValue);
            }

            // external libraries can register types for input-constant support
            if (NewInput == null && DefaultTypeInfoLibrary.TypeSupportsInputConstant(argInfo.argType)) 
            {
                object? defaultValue = DefaultTypeInfoLibrary.GetDefaultConstantValueForType(argInfo.argType);
                // use default argument if one could be provided
                if (paramInfo.DefaultValue != null && paramInfo.DefaultValue.GetType() == argInfo.argType)
                    defaultValue = paramInfo.DefaultValue;
                if (defaultValue == null)
                    NewInput = new StandardNodeInputBase(argInfo.argType);
                else
                    NewInput = new StandardNodeInputBaseWithConstant(argInfo.argType, defaultValue);
            }

            if (NewInput == null) {
                NewInput = new StandardNodeInputBase(argInfo.argType);
            }

            if (argInfo.bIsRefInput)
                NewInput.Flags |= ENodeInputFlags.IsInOut;

            return NewInput;
        }




        public static INodeInput BuildInputNodeForType(Type inputType, object? DefaultValue = null)
        {
            if (inputType == typeof(bool))
            {
                bool defaultValue = false;
                if (DefaultValue != null && DefaultValue is bool)
                    defaultValue = (bool)DefaultValue;
                return new StandardNodeInputBaseWithConstant(inputType, defaultValue);
            }
            else if (inputType == typeof(float))
            {
                float defaultValue = 0.0f;
                if (DefaultValue != null && DefaultValue is float)
                    defaultValue = (float)DefaultValue;
                return new StandardNodeInputBaseWithConstant(inputType, defaultValue);
            }
            else if (inputType == typeof(double))
            {
                double defaultValue = 0.0;
                if (DefaultValue != null && DefaultValue is double)
                    defaultValue = (double)DefaultValue;
                return new StandardNodeInputBaseWithConstant(inputType, defaultValue);
            }
            else if (inputType == typeof(int))
            {
                int defaultValue = 0;
                if (DefaultValue != null && DefaultValue is int)
                    defaultValue = (int)DefaultValue;
                return new StandardNodeInputBaseWithConstant(inputType, defaultValue);
            }
            else if (inputType == typeof(short))
            {
                short defaultValue = 0;
                if (DefaultValue != null && DefaultValue is short)
                    defaultValue = (short)DefaultValue;
                return new StandardNodeInputBaseWithConstant(inputType, defaultValue);
            }
            else if (inputType == typeof(long))
            {
                long defaultValue = 0;
                if (DefaultValue != null && DefaultValue is long)
                    defaultValue = (long)DefaultValue;
                return new StandardNodeInputBaseWithConstant(inputType, defaultValue);
            }
            else if (inputType == typeof(string))
            {
                string defaultValue = "";
                if (DefaultValue != null && DefaultValue is string)
                    defaultValue = (string)DefaultValue;
                //if (foundParamInfo != null && foundParamInfo.DefaultValue != null && foundParamInfo.DefaultValue is string)
                //    defaultValue = (string)foundParamInfo.DefaultValue;
                return new StandardNodeInputBaseWithConstant(inputType, defaultValue);
            }
            else if (inputType.IsEnum)
            {
                //this doesn't work properly if 0 is not one of the enum values
                //object? defaultValue = Activator.CreateInstance(paramType);
                object? defaultValue = System.Enum.GetValues(inputType).GetValue(0);
                if (DefaultValue != null && DefaultValue.GetType() == inputType)
                    defaultValue = DefaultValue;
                //if (foundParamInfo != null && foundParamInfo.DefaultValue != null && foundParamInfo.DefaultValue.GetType().IsEnum)
                //    defaultValue = foundParamInfo.DefaultValue;
                if (defaultValue == null)
                    return new StandardNodeInputBase(inputType);
                else
                    return new StandardNodeInputBaseWithConstant(inputType, defaultValue);
            }

            if ( TypeUtils.IsNullableType(inputType) )
				return new StandardNullableNodeInput(inputType);

			return new StandardNodeInputBase(inputType);
        }

    }

}
