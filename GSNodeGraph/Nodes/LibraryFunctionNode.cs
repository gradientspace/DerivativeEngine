// Copyright Gradientspace Corp. All Rights Reserved.
using System.Diagnostics;
using System.Reflection;


namespace Gradientspace.NodeGraph
{
    [ClassHierarchyNode]
    public abstract class LibraryFunctionNodeBase : NodeBase, ICodeGen
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

		public override string ToString()
		{
			return $"{LibraryClass!.Name}.{Function!.Name}";
		}

        public override string? GetNodeNamespace()
        {
            return LibraryNodeType?.UICategory ?? base.GetNodeNamespace();
        }

        public override void CollectDependencies(Action<Assembly> DependencyFunc)
        {
            base.CollectDependencies(DependencyFunc);
            if (LibraryClass != null)
                DependencyFunc(LibraryClass.Assembly);
        }


        // ICodeGen interface
        public void GetCodeOutputNames(out string[]? OutputNames)
        {
            // how to handle ref args... ??

            OutputNames = new string[Outputs.Count];
            for (int i = 0; i < OutputNames.Length; i++)
                OutputNames[i] = CodeGenUtils.SanitizeVarName(Outputs[i].Name);
        }
        public string GenerateCode(string[]? Arguments, string[]? UseOutputNames)
        {
            CodeGenUtils.CheckArgsAndOutputs(Arguments, Inputs.Count, UseOutputNames, Outputs.Count, this.ToString());

            string accumCall = "";
            if (FunctionInfo!.ReturnType != typeof(void)) {
                accumCall = $"{FunctionInfo!.ReturnType.ToString()} {UseOutputNames![0]} = ";
            }
            accumCall += $"{LibraryClass!.Name}.{Function!.Name}(";

            for ( int i = 0; i < Inputs.Count; ++i ) 
            {
                string arg = Arguments![i];
                accumCall += arg;

                if (i < Inputs.Count-1)
                    accumCall += ", ";
            }
            

            accumCall += ");";
            return accumCall;
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
            LibraryFunctionNode newNode = new LibraryFunctionNode(LibraryClass!, Function!, NodeName);
            newNode.Flags = this.Flags;

            return newNode;
        }

		public override string ToString()
		{
            return NodeName + $" ({base.ToString()})";
        }
    }







}
