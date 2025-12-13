// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.CodeNodes;
using GSPython;
using Python.Runtime;
using System.Diagnostics;


namespace Gradientspace.NodeGraph.PythonNodes
{
	[GraphNodeNamespace("Python")]
	[GraphNodeUIName("Python Function Node")]
    [MappedNodeTypeName("Core.Python.PythonFunctionCodeNode")]
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
			// NOTE: NodeLibrary.Build() creates an instance of this type (as nodeArchetype), which
			// forces a code update, which forces PythonEngine to be loaded/initialized (which is slow). 
			// Maybe we could avoid OnCodeUpdated() here?? Like set a bPendingCodeUpdate flag or something?

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
				if (PythonSetup.IsPythonAvailable)
				{
					using (Py.GIL())
					{
						CurrentPyLib = PythonLibrary.ParseModuleSource(SourceCode.CodeText);
					}

					// maybe should take last function, not first? does python require definitions in-order?
					NodeFunction = CurrentPyLib.PythonFunctions[0];
				}
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
		public override void CollectCustomDataItems(out NodeCustomData? DataItems)
		{
            DataItems = new NodeCustomData().
                AddStringItem(CodeTextString, SourceCode.CodeText).
                AddEnumItem(CodeLanguageString, SourceCode.CodeLanguage);
        }
		public override void RestoreCustomDataItems(NodeCustomData DataItems)
		{
            var Language = DataItems.FindEnumItemOrDefault(CodeLanguageString, SourceCodeDataType.Language.CSharp);
            Debug.Assert(Language == SourceCodeDataType.Language.Python);

            string? FoundCode = DataItems.FindStringItem(CodeTextString);
			if (FoundCode != null)
				UpdateSourceCode(new SourceCodeDataType() { CodeText = FoundCode, CodeLanguage = SourceCodeDataType.Language.Python});
		}

	}
}
