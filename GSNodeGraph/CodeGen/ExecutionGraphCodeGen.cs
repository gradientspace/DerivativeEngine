using System;
using System.Diagnostics;
using System.Xml.Linq;


namespace Gradientspace.NodeGraph
{
    public class ExecutionGraphCodeGen
    {
        ExecutionGraph Graph;

        public string UseNamespace = "GraphGenCode";
        public string UseClassName = "Program";
        public bool AddComments = false;

        public ExecutionGraphCodeGen(ExecutionGraph graph) 
        {
            Graph = graph;
        }


        public string GenerateCode()
        {
            CodeBuilder builder = new CodeBuilder();

            builder.AppendLine($"namespace {UseNamespace}");
            builder.AppendOpenBrace();
            builder.AppendLine($"internal class {UseClassName}");
            builder.AppendOpenBrace();

            // process functions first
            foreach (FunctionDefinitionNode funcDefNode in Graph.EnumerateNodesOfType<FunctionDefinitionNode>()) 
            {
                string FunctionCode = GenerateGraphFunction(funcDefNode);
                builder.AppendBlock(FunctionCode);
            }

            int StartCounter = 0;
            foreach (SequenceStartNode startNode in Graph.EnumerateNodesOfType<SequenceStartNode>()) 
            {
                string MainCode = GenerateMainSequence(builder, startNode, StartCounter++);
                builder.AppendBlock(MainCode);
            }


            builder.AppendCloseBrace();     // close class
            builder.AppendCloseBrace();     // close namespace


            CodeBuilder namespaceBuilder = new CodeBuilder(); 
            foreach (string nameSpace in UsingNames)
                namespaceBuilder.AppendLine($"using {nameSpace};");
            namespaceBuilder.AppendEmptyLine(2);

            return namespaceBuilder.GetCode() + builder.GetCode();
        }


        List<string> UsingNames = new List<string>();

        protected virtual void add_namespace(string Namespace)
        {
            if (UsingNames.Contains(Namespace) == false)
                UsingNames.Add(Namespace);
        }


        protected virtual string GenerateMainSequence(CodeBuilder parentBuilder, SequenceStartNode startNode, int Count)
        {
            string UseMainSignature = (Count == 0) ?
                "static void Main(string[] args)" : $"static void Main_{Count}";

            CodeBuilder builder = new CodeBuilder();
            builder.AppendLine(UseMainSignature);
            builder.AppendOpenBrace();

            if (get_outgoing_seq_connection_single(startNode, out IConnectionInfo nextSeq))
                process_graph_path(builder, startNode, nextSeq, out NodeBase? LastNodeInPath);

            builder.AppendCloseBrace();
            return builder.GetCode();
        }



        protected virtual string GenerateGraphFunction(FunctionDefinitionNode funcDefNode)
        {
            throw new NotImplementedException();
        }



        bool get_outgoing_seq_connection_single(NodeBase node, out IConnectionInfo NextSequenceConnection, string SeqOutputName = "")
        {
            NextSequenceConnection = IConnectionInfo.Invalid;
            List<IConnectionInfo> OutputConnections = new List<IConnectionInfo>();
            Graph.FindConnectionsFrom(node.GraphIdentifier, SeqOutputName, ref OutputConnections, EConnectionType.Sequence);
            if (OutputConnections.Count == 0)
                return false;
            NextSequenceConnection = OutputConnections[0];
            return true;
        }


        int variable_counter = 0;
        protected string allocate_scope_variable(string? baseName)
        {
            if (baseName == null)
                baseName = "var";
            baseName = CodeGenUtils.SanitizeVarName(baseName);
            int num = variable_counter++;
            return baseName + num.ToString();
        }




        protected virtual void process_node(
            CodeBuilder codeBuilder,
            NodeBase NextNode,
            out IConnectionInfo NextConnection)
        {
            // some node types are special and are evaluated differently than "standard" nodes
            // that have only have one sequence output
            if (NextNode is BranchNode branchNode) {
                // branch node recursively calls process_graph_path for each output pin and so it
                // will never return a next-connection
                NextConnection = IConnectionInfo.Invalid;
                handlenode_branch(codeBuilder, branchNode);
                return;
            } 

            // control-flow nodes currently require hardcoded handling...
            // unclear how to abstract then via ICodeGen/etc because they need to
            // be able to recursively evaluate graph paths for their blocks
            // (possibly that could be handled at this level by looking at output pins?)
            if (NextNode is ControlFlowNode flowNode) {
                throw new Exception($"CodeGen - unsupported ControlFlowNode type {flowNode}");
            }

            
            if (NextNode is FunctionCallNode callNode) {
                process_function_call_node(codeBuilder, callNode, out NextConnection);
            } else if (NextNode is FunctionReturnNode retNode) {
                process_function_return_node(codeBuilder, retNode, out NextConnection);
            } else if (NextNode is LibraryFunctionNodeBase libfuncNode) {
                process_library_function_node(codeBuilder, libfuncNode, out NextConnection);
            } else {
                process_basic_node(codeBuilder, NextNode, out NextConnection);
            }
        }


        protected virtual void process_function_call_node(
            CodeBuilder codeBuilder,
            FunctionCallNode funcCallNode,
            out IConnectionInfo NextConnection)
        {
            throw new NotImplementedException();
        }
        protected virtual void process_function_return_node(
            CodeBuilder codeBuilder,
            FunctionReturnNode funcReturnNode,
            out IConnectionInfo NextConnection)
        {
            throw new NotImplementedException();
        }

        protected virtual void process_library_function_node(
            CodeBuilder codeBuilder,
            LibraryFunctionNodeBase libraryFuncNode,
            out IConnectionInfo NextConnection)
        {
            string[] inputTokens = construct_input_tokens(libraryFuncNode, codeBuilder);
            string[]? OutputVarNames = find_output_variable_names(libraryFuncNode);

            string callingCode = libraryFuncNode.GenerateCode(inputTokens, OutputVarNames);

            if (AddComments) codeBuilder.AppendLine( make_node_comment(libraryFuncNode, "[process_library_function_node]") );
            codeBuilder.AppendBlock(callingCode);
            codeBuilder.AppendEmptyLine();

            // remember output variable names
            save_variables(libraryFuncNode, OutputVarNames);

            get_outgoing_seq_connection_single(libraryFuncNode, out NextConnection);

            // note: above code is the same as process_basic_node...

            // handle using...
            // this should maybe be folded into codeBuilder??
            Type usingClass = libraryFuncNode.LibraryClass!;
            if (usingClass.Namespace != null)
                add_namespace(usingClass.Namespace);
        }




        protected virtual void process_basic_node(
            CodeBuilder codeBuilder,
            NodeBase baseNode,
            out IConnectionInfo NextConnection)
        {
            string[]? OutputVarNames = null;
            if (baseNode is ICodeGen codeGen) {

                string[] inputTokens = construct_input_tokens(baseNode, codeBuilder);
                OutputVarNames = find_output_variable_names(codeGen);

                string callingCode = codeGen.GenerateCode(inputTokens, OutputVarNames);

                if (AddComments) codeBuilder.AppendLine( make_node_comment(baseNode, "[process_basic_node]") );
                codeBuilder.AppendBlock(callingCode);
                codeBuilder.AppendEmptyLine();

            } else {
                codeBuilder.AppendOpenBrace();
                if (AddComments) codeBuilder.AppendLine( make_node_comment(baseNode, "[process_basic_node] (ICodeGen missing)") );
                codeBuilder.AppendCloseBrace();
                codeBuilder.AppendEmptyLine();
            }

            // remember output variable names
            save_variables(baseNode, OutputVarNames);

            get_outgoing_seq_connection_single(baseNode, out NextConnection);
        }





        protected struct GeneratedVar
        {
            public string Key;
            public NodeBase Node;
            public string OutputName;

            public string VariableName;

            public GeneratedVar(NodeBase node, string outputName, string varName)
            {
                Node = node;
                OutputName = outputName;
                VariableName = varName;
                Key = MakeKey(node.GraphIdentifier, outputName);
            }

            public static string MakeKey(int NodeID, string outputName)
            {
                return $"Node{NodeID}::{outputName}";
            }
        }

        Dictionary<string, GeneratedVar> CurrentVariables = new Dictionary<string, GeneratedVar>();
        HashSet<string> PureScopeVars = new HashSet<string>();

        void cache_variable(GeneratedVar variable)
        {
            CurrentVariables.Add(variable.Key, variable);
            if (pure_scope_depth > 0)
                PureScopeVars.Add(variable.Key);
        }
        void discard_scope_vars()
        {
            foreach (string s in PureScopeVars)
                CurrentVariables.Remove(s);
            PureScopeVars.Clear();
        }


        int pure_scope_depth = 0;
        void begin_pure_scope(CodeBuilder codeBuilder)
        {
            if (pure_scope_depth == 0) 
                codeBuilder.AppendLine("// -- begin pure scope");
            pure_scope_depth++;
        }
        void end_pure_scope(CodeBuilder codeBuilder)
        {
            if (pure_scope_depth == 1) {
                codeBuilder.AppendLine("// -- end pure scope");
                discard_scope_vars();
            }
            pure_scope_depth = Math.Max(pure_scope_depth-1, 0);
        }


        protected struct ArgumentCode
        {
            public string Code;

            public ArgumentCode() { 
                Code = "/*(undefined)*/"; 
            }

            public void TrySet(string? code)
            {
                if (code != null)
                    Code = code;
            }
        }


        protected bool find_node_input(NodeBase baseNode, INodeInputInfo inputInfo, CodeBuilder codeBuilder, out ArgumentCode argCode)
        {
            argCode = new();

            IConnectionInfo connection = Graph.FindConnectionTo(baseNode.GraphIdentifier, inputInfo.InputName, EConnectionType.Data);

            // if input has no connection, it either has a constant value, or the graph is undefined
            if (connection == IConnectionInfo.Invalid) 
            {
                (object? constVal, bool bExists) = Graph.GetConstantValueForInput(baseNode.Handle, inputInfo.InputName);
                if (bExists)
                    argCode.TrySet(make_constant_token(constVal));
                return bExists;
            } 

            // check if the input value can be found in variables existing at this point
            string VarKey = GeneratedVar.MakeKey(connection.FromNodeIdentifier, connection.FromNodeOutputName);
            if (CurrentVariables.TryGetValue(VarKey, out GeneratedVar variable)) {
                argCode.TrySet(variable.VariableName);
                return true;
            }

            // !!! Block below will always run because every node is NodeBase
            // !!! need to detect if it's actually a valid pure-scope...

            // only option left is that input is from pure nodes that we have to recursively evaluate...
            if ( Graph.FindNodeFromIdentifier(connection.FromNodeIdentifier).Node is NodeBase pureNode ) {

                begin_pure_scope(codeBuilder);
                // this will recurse into any upstream pure nodes
                process_pure_node(pureNode, codeBuilder);

                bool bSuccess = false;
                if (CurrentVariables.TryGetValue(VarKey, out GeneratedVar pureValue)) {
                    argCode.TrySet(pureValue.VariableName);
                    bSuccess = true;
                }
                end_pure_scope(codeBuilder);

                return bSuccess;
            }

            return false;
        }


        void process_pure_node(NodeBase pureNode, CodeBuilder codeBuilder)
        {
            // this will recursively call process_pure_node on inputs
            string[] inputTokens = construct_input_tokens(pureNode, codeBuilder);

            ICodeGen codeGen = (pureNode as ICodeGen)!;
            string[]? OutputVarNames = find_output_variable_names(codeGen);
            string callingCode = codeGen.GenerateCode(inputTokens, OutputVarNames);

            if (AddComments) codeBuilder.AppendLine( make_node_comment(pureNode, "[process_pure_node]") );
            codeBuilder.AppendBlock(callingCode);

            save_variables(pureNode, OutputVarNames);
        }



        string? make_constant_token(object? constVal)
        {
            if (constVal == null) {
                return "null";
            } else if (constVal is float f) {
                return f.ToString() + "f";
            } else if (constVal is double d) {
                return d.ToString();
            } else if (constVal is string s) {
                // todo need to possibly escape string chars?
                return "\"" + s + "\"";
            } else if (constVal is bool b) {
                return (b) ? "true" : "false";
            } else {
                return constVal.ToString();
            }
        }

        // builds a string for each node input (eg to pass as arguments or use in generated statements)
        protected string[] construct_input_tokens(NodeBase baseNode, CodeBuilder codeBuilder)
        {
            List<string> inputs = new List<string>();
            foreach (INodeInputInfo input in baseNode.EnumerateInputs()) 
            {
                bool bFound = find_node_input(baseNode, input, codeBuilder, out ArgumentCode argCode);
                inputs.Add(argCode.Code);
            }
            return inputs.ToArray();
        }

        // get a string for each output variable of a node, to use as a variable name in code
        protected string[]? find_output_variable_names(ICodeGen codeGen)
        {
            string[]? OutputVarNames = null;
            codeGen.GetCodeOutputNames(out OutputVarNames);
            if (OutputVarNames != null) 
            {
                for (int i = 0; i < OutputVarNames.Length; i++)
                    OutputVarNames[i] = allocate_scope_variable(OutputVarNames[i]);
            }
            return OutputVarNames;
        }

        protected void save_variables(NodeBase node, string[]? outputVarNames)
        {
            INodeOutputInfo[] outputs = node.EnumerateOutputs().ToArray();
            if (outputVarNames != null && outputs.Length == outputVarNames.Length) 
            {
                for (int i = 0; i <  outputs.Length; i++) 
                {
                    GeneratedVar newVar = new GeneratedVar(node, outputs[i].OutputName, outputVarNames[i]);
                    cache_variable(newVar);
                }
            }
        }

        string make_node_comment(NodeBase node, string? identifier)
        {
            if (node.GetNodeName().CompareTo(node.LibraryNodeType?.UIName ?? "") == 0) {
                return "// [" + node.GraphIdentifier.ToString() + "] " +
                    (node.LibraryNodeType?.ToString() ?? "(unknown type)") + " // " +
                    (identifier ?? "");
            } else
                return "// [" + node.GraphIdentifier.ToString() + "] " + node.GetNodeName() + " // " +
                    (node.LibraryNodeType?.ToString() ?? "(unknown type)") + " // " +
                    (identifier ?? "");
        }



        protected void process_graph_path(CodeBuilder builder, NodeBase CurrentNode, IConnectionInfo OutgoingConnection, out NodeBase? LastNodeInPath)
        {
            IConnectionInfo NextSequenceConnection = OutgoingConnection;
            Debug.Assert(CurrentNode != null && NextSequenceConnection.IsValid);

            bool bDone = false;
            while (!bDone) {
                NodeHandle NextNodeHandle = new(NextSequenceConnection.ToNodeIdentifier);
                NodeBase? NextNode = Graph.FindNodeFromHandle(NextNodeHandle) as NodeBase;
                if (NextNode == null)
                    throw new Exception("process_graph_path: next node in sequence could not be found in graph!");

                IConnectionInfo NextConnection = IConnectionInfo.Invalid;
                process_node(builder, NextNode, out NextConnection);
                CurrentNode = NextNode;

                if (NextConnection == IConnectionInfo.Invalid) {
                    bDone = true;
                } else {
                    NextSequenceConnection = NextConnection;
                }
            }

            LastNodeInPath = CurrentNode;
        }




        // sequence node handling



        protected virtual void handlenode_branch(
            CodeBuilder codeBuilder,
            BranchNode branchNode)
        {
            string[] inputTokens = construct_input_tokens(branchNode, codeBuilder);
            Debug.Assert(inputTokens.Length == 1);

            bool bHasTrue = get_outgoing_seq_connection_single(branchNode, out IConnectionInfo trueConnection, BranchNode.TrueOutputName);
            bool bHasFalse = get_outgoing_seq_connection_single(branchNode, out IConnectionInfo falseConnection, BranchNode.FalseOutputName);

            CodeBuilder branchBuilder = new CodeBuilder();
            branchBuilder.AppendLine($"if ({inputTokens[0]})");
            branchBuilder.AppendOpenBrace();

            if (bHasTrue) 
                process_graph_path(branchBuilder, branchNode, trueConnection, out NodeBase? lastNode);

            branchBuilder.AppendCloseBrace();
            if (bHasFalse) 
            {
                branchBuilder.AppendLine(" else ");
                branchBuilder.AppendOpenBrace();
                process_graph_path(branchBuilder, branchNode, falseConnection, out NodeBase? lastNode);
                branchBuilder.AppendCloseBrace();
            }

            if (AddComments) codeBuilder.AppendLine(make_node_comment(branchNode, "[handlenode_branch]"));
            codeBuilder.AppendBlock(branchBuilder.GetCode());
            codeBuilder.AppendEmptyLine();
        }



    }
}
