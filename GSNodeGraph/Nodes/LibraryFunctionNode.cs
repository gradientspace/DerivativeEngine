using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Gradientspace.NodeGraph
{
    [ClassHierarchyNode]
    public abstract class LibraryFunctionNodeBase : NodeBase
    {
        public Type? LibraryClass { get; protected set; } = null;
        public MethodInfo? Function { get; protected set; } = null;
        public FunctionNodeInfo? FunctionInfo { get; protected set; } = null;
        public bool IsInitialized { get; protected set; } = false;

        public LibraryFunctionNodeBase()
        {

        }

        public LibraryFunctionNodeBase(Type libraryClass, MethodInfo function)
        {
            setFunction(libraryClass, function);
        }

        protected virtual void setFunction(Type libraryClass, MethodInfo function)
        {
            LibraryClass = libraryClass;
            Function = function;
            FunctionInfo = new FunctionNodeInfo(function);
            updateInputsOutputs();
            IsInitialized = true;
        }

        protected virtual void clearFunction()
        {
            LibraryClass = null;
            Function = null;
            FunctionInfo = null;
            updateInputsOutputs();
            IsInitialized = false;
        }

        protected virtual void preAddChildInputsOutputs()
        {
        }

        protected virtual void updateInputsOutputs()
        {
            Inputs.Clear();
            Outputs.Clear();

            preAddChildInputsOutputs();

            if (FunctionInfo == null)
                return;

            if (FunctionInfo.ReturnType != typeof(void))
            {
                AddOutput(FunctionInfo.ReturnName, new StandardNodeOutputBase(FunctionInfo.ReturnType));
            }

            for (int i = 0; i < FunctionInfo.OutputArguments.Length; i++)
            {
                ref FuncOutputArgInfo outputArg = ref FunctionInfo.OutputArguments[i];
                INodeOutput nodeOutput = new StandardNodeOutputBase(outputArg.argType);
                AddOutput(outputArg.argName, nodeOutput);
            }

            for (int i = 0; i < FunctionInfo.InputArguments.Length; ++i)
            {
                ref FuncInputArgInfo inputArg = ref FunctionInfo.InputArguments[i];
                string paramName = inputArg.argName;    // why is this passed by ref?
                INodeInput nodeInput = FunctionNodeUtils.BuildInputNodeForMethodArgument(inputArg, ref paramName, Function!, inputArg.paramInfo);
                AddInput(paramName, nodeInput);
            }
        }


        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            if (FunctionInfo == null)
                throw new Exception("LibraryFunctionNode: FunctionInfo is not initialized");

            int ReturnIndex = -1;
            if (FunctionInfo.ReturnType != typeof(void))
            {
                ReturnIndex = RequestedDataOut.IndexOfItem(FunctionInfo.ReturnName);
                if (ReturnIndex == -1)
                    throw new Exception("LibraryFunctionNode: Return output not found");
            }

            object?[] arguments = FunctionNodeUtils.ConstructFuncEvaluationArguments(FunctionInfo, DataIn);

            object? returnObj = Function!.Invoke(null, arguments);

            FunctionNodeUtils.ExtractFuncEvaluationOutputs(FunctionInfo, arguments, RequestedDataOut);

            // take return value output
            if (ReturnIndex != -1 && returnObj != null)
                RequestedDataOut.SetItemValue(ReturnIndex, returnObj);
        }

    }



    //[MappedNodeTypeName("GSNodeGraph.LibraryFunctionNode")]
    [ClassHierarchyNode]
    public class LibraryFunctionNode : LibraryFunctionNodeBase
    {
        public override string GetDefaultNodeName()
        {
            return NodeName;
        }

        public string NodeName { get; }

        public LibraryFunctionNode(Type libraryClass, MethodInfo function, string nodeName) : base(libraryClass, function)
        {
            NodeName = nodeName;
        }

        public LibraryFunctionNode MakeInstance()
        {
            Debug.Assert(LibraryClass != null && Function != null);

            // TODO this is dumb, we should share Arguments construction between copies...
            return new LibraryFunctionNode(LibraryClass!, Function!, NodeName);
        }
    }







    [ClassHierarchyNode]
    public class LibraryFunctionSinkNode : SinkNode
    {
        public override string GetDefaultNodeName()
        {
            return NodeName;
        }

        public Type LibraryClass { get; set; }
        public MethodInfo Function { get; set; }
        public string NodeName { get; }

        internal FuncInputArgInfo[] Arguments;

        public LibraryFunctionSinkNode(Type libraryClass, MethodInfo function, string nodeName)
        {
            // TODO: this class needs to be updated and maybe should be removed entirely.
            // At minimum should share buildArguments code w/ the non-sink variant, and needs 
            // to support output args, etc
            Debug.Assert(false);

            LibraryClass = libraryClass;
            Function = function;
            NodeName = nodeName;

            Arguments = buildArguments();
        }


        public LibraryFunctionSinkNode MakeInstance()
        {
            // TODO this is dumb, we should share Arguments construction between copies...
            return new LibraryFunctionSinkNode(LibraryClass, Function, NodeName);
        }

        internal virtual FuncInputArgInfo[] buildArguments()
        {
            ParameterInfo[] parameters = Function.GetParameters();

            int N = parameters.Length;
            FuncInputArgInfo[] args = new FuncInputArgInfo[N];

            for (int i = 0; i < N; ++i)
            {
                ParameterInfo paramInfo = parameters[i];
                Type paramType = parameters[i].ParameterType;
                string? paramName = parameters[i].Name;
                if (paramName == null)
                    paramName = "arg" + i.ToString();

                args[i].argIndex = i;
                args[i].argName = paramName;
                args[i].argType = paramType;

                INodeInput nodeInput = FunctionNodeUtils.BuildInputNodeForMethodArgument(args[i], ref paramName, Function, paramInfo);
                AddInput(paramName, nodeInput);
            }

            return args;
        }


        public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            object?[] arguments = new object[Arguments.Length];
            for (int j = 0; j < Arguments.Length; ++j)
            {
                object? value = DataIn.FindItemValue(Arguments[j].argName);
                arguments[j] = value;
            }

            Function.Invoke(null, arguments);
        }

    }



}
