using Gradientspace.NodeGraph;
using GSPython;
using Python.Runtime;
using System.Diagnostics;


namespace Gradientspace.NodeGraph.PythonNodes
{
	[ClassHierarchyNode]
	public abstract class PythonFunctionNodeBase : NodeBase
	{
		protected PythonLibrary? PyLibrary = null;
		protected PythonFunction? PyFunction = null;
		public PyFunctionNodeInfo? FunctionInfo { get; protected set; } = null;

		public bool IsInitialized { get; protected set; } = false;


		public PythonFunctionNodeBase()
		{
		}

		public PythonFunctionNodeBase(PythonLibrary library, PythonFunction function)
		{
			setFunction(library, function);
		}

		protected virtual void setFunction(PythonLibrary library, PythonFunction function)
		{
			PyLibrary = library;
			PyFunction = function;
			FunctionInfo = new PyFunctionNodeInfo(function);
			updateInputsOutputs();
			IsInitialized = true;
		}

		protected virtual void clearFunction()
		{
			PyLibrary = null;
			PyFunction = null;
			FunctionInfo = null;
			updateInputsOutputs();
			IsInitialized = false;
		}



		protected virtual void updateInputsOutputs()
		{
			Inputs.Clear();
			Outputs.Clear();

			//preAddChildInputsOutputs();

			if (FunctionInfo == null)
				return;

			if (FunctionInfo.csharpReturnType != typeof(void))
			{
				AddOutput(FunctionInfo.ReturnName, new PythonNodeOutputBase(FunctionInfo.csharpReturnType, FunctionInfo.pythonReturnType.GetPyType()));
			}

			for (int i = 0; i < FunctionInfo.OutputArguments.Length; i++)
			{
				ref PyFuncOutputArgInfo outputArg = ref FunctionInfo.OutputArguments[i];
				INodeOutput nodeOutput = new PythonNodeOutputBase(outputArg);
				AddOutput(outputArg.argName, nodeOutput);
			}

			for (int i = 0; i < FunctionInfo.InputArguments.Length; ++i)
			{
				ref PyFuncInputArgInfo inputArg = ref FunctionInfo.InputArguments[i];
				string paramName = inputArg.argName;    // why is this passed by ref?
				INodeInput nodeInput = PythonFunctionNodeUtils.BuildInputNodeForFunctionArgument(inputArg, ref paramName);
				AddInput(paramName, nodeInput);
			}
		}



		public override void Evaluate(
			ref readonly NamedDataMap DataIn,
			NamedDataMap RequestedDataOut)
		{
			if (PyLibrary == null || PyFunction == null)
				throw new Exception("PythonFunctionNodeBase: PyLibrary/PyFunction is not initialized");
			if (FunctionInfo == null)
				throw new Exception("PythonFunctionNodeBase: FunctionInfo is not initialized");

			int ReturnIndex = -1;
			if (FunctionInfo.csharpReturnType != typeof(void))
			{
				ReturnIndex = RequestedDataOut.IndexOfItem(FunctionInfo.ReturnName);
				if (ReturnIndex == -1)
					throw new Exception("PythonFunctionNodeBase: Return output not found");
			}

			object?[] CSharpArgs = PythonFunctionNodeUtils.ConstructFuncEvaluationArguments(FunctionInfo, DataIn);

			// convert C# arguments to PyObject arguments (does this have to happen inside GIL?)
			PyObject[] PythonArgs = new PyObject[CSharpArgs.Length];
			using (Py.GIL())
			{
				for (int i = 0; i < CSharpArgs.Length; ++i)
					PythonArgs[i] = CSharpArgs[i].ToPython();
			}

			PyObject? PythonReturnObj = PyLibrary.EvaluateFunction(PyFunction, PythonArgs);

			// TODO: can this ever happen? python doesn't support out or reference arguments...and object references
			// will automatically be handled? Or do they still explicitly need to be copied to RequestedDataOut?
			//PythonFunctionNodeUtils.ExtractFuncEvaluationOutputs(FunctionInfo, arguments, RequestedDataOut);

			// take return value output
			if (ReturnIndex != -1 && PythonReturnObj != null)
			{
				object? CSharpReturnObj = PythonReturnObj.AsManagedObject(FunctionInfo.csharpReturnType);
				if ( CSharpReturnObj != null )
					RequestedDataOut.SetItemValue(ReturnIndex, CSharpReturnObj);
			}
		}
	}



	[ClassHierarchyNode]
	public class PythonLibraryFunctionNode : PythonFunctionNodeBase
	{
		public override string GetDefaultNodeName()
		{
			return NodeName;
		}

		public string NodeName { get; }

		public PythonLibraryFunctionNode(PythonLibrary library, PythonFunction function, string nodeName) : base(library, function)
		{
			NodeName = nodeName;
		}

		public PythonLibraryFunctionNode MakeInstance()
	{
			Debug.Assert(PyLibrary != null && PyFunction != null);

			// TODO this is dumb, we should share Arguments construction between copies...
			return new PythonLibraryFunctionNode(PyLibrary!, PyFunction!, NodeName);
		}
	}

}
