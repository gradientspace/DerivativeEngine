using Gradientspace.NodeGraph;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Gradientspace.NodeGraph.CodeNodes
{
    [GraphNodeNamespace("Gradientspace.Code")]
    [GraphNodeUIName("C# Function Node")]
    public class CodeFunctionNode : LibraryFunctionNodeBase, INodeWithInlineCode
	{
        public const string CodeInputName = "Code";
        public const string DefaultNodeName = "NodeFunction";
        public const string CodeNodeClassName = "NodeClass";
        public const string CodeNodeMethodName = "NodeFunction";


        //public static OptimizationLevel UseOptimizationLevel = OptimizationLevel.Release;
        public static OptimizationLevel UseOptimizationLevel = OptimizationLevel.Debug;

        public override string GetDefaultNodeName()
        {
            return NodeName;
        }

        public string NodeName { get; private set; } = DefaultNodeName;
        public SourceCodeDataType SourceCode { get; private set; }

        public CodeFunctionNode()
        {
            SourceCode = SourceCodeDataType.MakeDefaultCSharp();
            OnCodeUpdated();
        }

        public virtual void UpdateSourceCode(SourceCodeDataType NewCode, bool bImmediateRebuild = true)
        {
			if (NewCode.CodeLanguage != SourceCodeDataType.Language.CSharp)
				throw new Exception("CodeFunctionNode.UpdateSourceCode: provided code is not C#");

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
			return CodeNodeMethodName;
		}

		// TODO this will not work need to use Dispose()...
		~CodeFunctionNode() {
            // can we remove this wait?
            UnloadAssembly(true);
        }



        byte[]? CompiledAssembly = null;
        CodeNodeAssemblyLoadContext? LoadedAssembly = null;

        protected void OnCodeUpdated()
        {
            List<string> Errors = new List<string>();
            CompiledAssembly = null;
            using (var peStream = new MemoryStream())
            {
                CSharpCompilation? Compilation = GenerateCode(SourceCode.CodeText, out int NumPrependedLines);
                EmitResult? result = Compilation?.Emit(peStream) ?? null;

                if (result == null) {
                    Errors.Add("Compiler failed without providing error information");
                    Debug.WriteLine("[CodeFunctionNode] GenerateCode failed without error for SourceText: " + SourceCode.CodeText);
                } 
                else if (result.Success == false)
                {
                    Debug.WriteLine("[CodeFunctionNode] Compile Error!!");
                    var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
                    foreach (Diagnostic diagnostic in failures) {
                        FileLinePositionSpan LineSpan = diagnostic.Location.GetLineSpan();
                        int LineNum = LineSpan.StartLinePosition.Line - NumPrependedLines + 1;
                        Debug.WriteLine("Line {0} - {1}: {2}", LineNum, diagnostic.Id, diagnostic.GetMessage());
                        Errors.Add(string.Format("Line {0} - {1}: {2}", LineNum, diagnostic.Id, diagnostic.GetMessage()));
                    }
                }
                else
                {
                    peStream.Seek(0, SeekOrigin.Begin);
                    CompiledAssembly = peStream.ToArray();
                }
            }

            if (LoadedAssembly != null) {
                UnloadAssembly(true);
            }

            if (CompiledAssembly == null ) {
                OnCompileStatusUpdate?.Invoke(false, Errors);
                return;
            }

            LoadedAssembly = LoadAssemblyFromBytes(CompiledAssembly);

            if (LoadedAssembly != null && LoadedAssembly.CodeNodeClass != null && LoadedAssembly.CodeNodeMethod != null)
            {
                setFunction(LoadedAssembly.CodeNodeClass, LoadedAssembly.CodeNodeMethod);
                
                NodeName = LoadedAssembly.CodeNodeMethod.GetCustomAttribute<NodeFunctionUIName>()?.UIName ?? LoadedAssembly.CodeNodeMethod.Name;

                PublishNodeModifiedNotification();
                OnCompileStatusUpdate?.Invoke(true, null);
            }
            else
            {
                if (LoadedAssembly == null) {
                    Debug.WriteLine("[CodeFunctionNode] Compile OK but Assembly could not be loaded");
                    Errors.Add("compiled Assembly could not be loaded");
                }
                else if (LoadedAssembly.CodeNodeClass == null) {
                    Debug.WriteLine("[CodeFunctionNode] Could not find class named NodeClass");
                    Errors.Add("Could not find class named NodeClass");
                } 
                else if (LoadedAssembly.CodeNodeMethod == null) {
                    Debug.WriteLine("[CodeFunctionNode] Could not find public static method in class NodeClass");
                    Errors.Add("Could not find public static method in class NodeClass");
                }
                OnCompileStatusUpdate?.Invoke(false, Errors);
            }
        }


        private void UnloadAssembly(bool bWait)
        {
            if (LoadedAssembly == null) return;

            LoadedAssembly.Unload();
            var weakref = new WeakReference(LoadedAssembly);
            LoadedAssembly = null;

            // try to force GC and finalization...which probably doesn't work...
            // probably need to Dispose() some things?
            for (var i = 0; bWait && i < 8 && weakref.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }


        static int GeneratedAssemblyCounter = 0;


        private static CSharpCompilation? GenerateCode(string sourceCode, out int AddedLines)
        {
            AddedLines = 0;
            if (sourceCode.Length == 0)
                return null;

            // add standard namespaces
            string codeText = string.Join(
                Environment.NewLine,
                "using System;",
                "using System.Diagnostics;",
                "using System.Collections.Generic;",
                "using System.Linq;",
                "using System.Text;",
                sourceCode);
            AddedLines = 5;

            var codeString = SourceText.From(codeText);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp11);

            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            };
            Assembly? NodeAssembly = typeof(CodeFunctionNode).Assembly;
            AssemblyName[] ReferencedAssemblies = NodeAssembly?.GetReferencedAssemblies() ?? new AssemblyName[0];
            ReferencedAssemblies.ToList()                
                .ForEach(a => references.Add(MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

            // TODO this is probably not the way...ideally we would find all namespaces/types referenced in the
            // source text, and then search through these assemblies to find which ones have those namespaces/types in them
            Assembly[] CurrentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly a in CurrentAssemblies)
            {
                bool bSkipAssembly = a.GetName().Name?.StartsWith("CodeFunctionNode_") ?? true;
                if (a.Location.Length == 0)
                    bSkipAssembly = true;       // this happens for dynamically-generated pythonnet assemblies??
                if ( bSkipAssembly ) 
                    continue;
                references.Add(MetadataReference.CreateFromFile(a.Location));
            }

            string NewCompileName = "CodeFunctionNode_" + (GeneratedAssemblyCounter++).ToString() + ".dll";

            return CSharpCompilation.Create(NewCompileName,
                new[] { parsedSyntaxTree },
                references: references,
                //options: new CSharpCompilationOptions(OutputKind.ConsoleApplication,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: CodeFunctionNode.UseOptimizationLevel,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
        }



        [MethodImpl(MethodImplOptions.NoInlining)]
        private static CodeNodeAssemblyLoadContext? LoadAssemblyFromBytes(byte[] compiledAssembly)
        {
            using (var asm = new MemoryStream(compiledAssembly))
            {
                var assemblyLoadContext = new CodeNodeAssemblyLoadContext();
                var assembly = assemblyLoadContext.LoadFromStream(asm);

                // find standard-named code class
                Type? CodeNodeClass = null;
                foreach (Type t in assembly.GetExportedTypes())
                {
                    if ( t.Name == CodeNodeClassName )
                    {
                        CodeNodeClass = t;
                        break;
                    }
                }
                if (CodeNodeClass == null)
                    return assemblyLoadContext;
                assemblyLoadContext.CodeNodeClass = CodeNodeClass;

                // use first public static method...this could go wrong but then the user can make the others private
                MethodInfo? CodeNodeFunction = null;
                MethodInfo[] Methods = CodeNodeClass.GetMethods();
                MethodInfo[] StaticMethods = Array.FindAll<MethodInfo>(Methods, m => m.IsStatic && m.IsPublic);
                if (StaticMethods.Length > 0 ) {
                    CodeNodeFunction = StaticMethods[0];
                }
                if (CodeNodeFunction == null)
                    return assemblyLoadContext;

                assemblyLoadContext.CodeNodeMethod = CodeNodeFunction;
                return assemblyLoadContext;
            }
        }


        public const string CodeTextString = "CodeText";
        public const string CodeLanguageString = "Language";
        public override void CollectCustomDataItems(out List<Tuple<string, object>>? DataItems) 
        {
            DataItems = new List<Tuple<string, object>>();
            DataItems.Add( new(CodeTextString, SourceCode.CodeText) );
            DataItems.Add( new(CodeLanguageString, SourceCode.CodeLanguage.ToString()) );
        }
        public override void RestoreCustomDataItems(List<Tuple<string, object>> DataItems)
        {
            Tuple<string, object>? CodeString = DataItems.Find((x) => { return x.Item1 == CodeTextString; });
            if (CodeString != null)
                UpdateSourceCode(new SourceCodeDataType() { CodeText = (string)CodeString.Item2 });
        }
    }






    internal class CodeNodeAssemblyLoadContext : AssemblyLoadContext
    {
        public Type? CodeNodeClass { get; set; } = null;
        public MethodInfo? CodeNodeMethod { get; set; } = null;

        public CodeNodeAssemblyLoadContext()
            : base(true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName) {
            return null;
        }
    }
}
