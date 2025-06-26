using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.CodeNodes;
using GSPython;
using Python.Runtime;
using System.Diagnostics;


namespace Gradientspace.NodeGraph.PythonNodes
{
	[GraphNodeFunctionLibrary("Gradientspace.Python")]
	[GraphNodeUIName("Python Function Node")]
	public class PythonFunctionCodeNode : PythonFunctionNodeBase, INodeWithInlineCode
	{
		public const string CodeInputName = "PythonCode";
		public const string DefaultNodeName = "PythonFunction";

		public override string GetDefaultNodeName()
		{
			return NodeName;
		}

		public string NodeName { get; private set; } = DefaultNodeName;
		public SourceCodeDataType SourceCode { get; private set; }

		public PythonFunctionCodeNode()
		{
			SourceCode = SourceCodeDataType.MakeDefaultPython();
			OnCodeUpdated();
		}


		public virtual void UpdateSourceCode(SourceCodeDataType NewCode, bool bImmediateRebuild = true)
		{
			if (NewCode.CodeLanguage != SourceCodeDataType.Language.Python)
				throw new Exception("PythonFunctionCodeNode.UpdateSourceCode: provided code is not Python");

			if (NewCode.CodeText != SourceCode.CodeText)
			{
				SourceCode = NewCode.MakeDuplicate();
				if (bImmediateRebuild)
					OnCodeUpdated();
			}
		}

		public virtual void Rebuild()
		{
			OnCodeUpdated();
		}


		// INodeWithInlineCode API
		public SourceCodeDataType GetInlineSourceCode() { return SourceCode; }
		public void SetInlineSourceCode(SourceCodeDataType NewSourceCode) { UpdateSourceCode(NewSourceCode, true); }
		public event ICodeNodeCompileStatusEvent? OnCompileStatusUpdate;
		public virtual string GetCodeNameHint() {
			return (NodeFunction != null) ? NodeFunction.FunctionName : "PythonNode";
		}


		protected PythonLibrary? CurrentPyLib = null;
		protected PythonFunction? NodeFunction = null;

		protected void OnCodeUpdated()
		{
			List<string> Errors = new List<string>();

			try
			{
				PythonSetup.InitializePython();
				using (Py.GIL()) {
					CurrentPyLib = PythonLibrary.ParseModuleSource(SourceCode.CodeText);
				}

				// maybe should take last function, not first? does python require definitions in-order?
				NodeFunction = CurrentPyLib.PythonFunctions[0];

			} 
			catch(Exception ex)
			{
				Debug.WriteLine("[PythonFunctionCodeNode] Compile Error!!");
				// tood can we get more feedback here?
				Debug.WriteLine(ex.Message);
				Errors.Add(ex.Message);

				CurrentPyLib = null;
				NodeFunction = null;
			}

			if (CurrentPyLib != null && NodeFunction != null)
			{
				setFunction(CurrentPyLib, NodeFunction);
				NodeName = NodeFunction.FunctionName;
				PublishNodeModifiedNotification();
				OnCompileStatusUpdate?.Invoke(true, null);
			} else { 
				clearFunction();
				OnCompileStatusUpdate?.Invoke(false, Errors);
			}
		}



		// load/save

		public const string CodeTextString = "CodeText";
		public const string CodeLanguageString = "Language";
		public override void CollectCustomDataItems(out List<Tuple<string, object>>? DataItems)
		{
			DataItems = new List<Tuple<string, object>>();
			DataItems.Add(new(CodeTextString, SourceCode.CodeText));
			DataItems.Add(new(CodeLanguageString, SourceCode.CodeLanguage.ToString()));
		}
		public override void RestoreCustomDataItems(List<Tuple<string, object>> DataItems)
		{
			Tuple<string, object>? CodeString = DataItems.Find((x) => { return x.Item1 == CodeTextString; });

			Tuple<string, object>? LanguageString = DataItems.Find((x) => { return x.Item1 == CodeLanguageString; });
			SourceCodeDataType.Language langType = SourceCodeDataType.Language.Python;
			if (Enum.TryParse<SourceCodeDataType.Language>((string)LanguageString.Item2, out SourceCodeDataType.Language result))
				langType = result;

			if (CodeString != null && LanguageString != null)
				UpdateSourceCode(new SourceCodeDataType() { 
					CodeText = (string)CodeString.Item2, CodeLanguage = langType });
		}

	}
}