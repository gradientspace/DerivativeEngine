using System;
using System.Diagnostics;


namespace Gradientspace.NodeGraph
{
    public class ExecutionGraphCodeGen
    {
        ExecutionGraph Graph;

        public string UseNamespace = "GraphGenCode";
        public string UseClassName = "Program";

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

            return namespaceBuilder.GetString() + builder.GetString();
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
            return builder.GetString();
        }



        protected virtual string GenerateGraphFunction(FunctionDefinitionNode funcDefNode)
        {
            throw new NotImplementedException();
        }



        bool get_outgoing_seq_connection_single(NodeBase node, out IConnectionInfo NextSequenceConnection)
        {
            NextSequenceConnection = IConnectionInfo.Invalid;
            List<IConnectionInfo> OutputConnections = new List<IConnectionInfo>();
            Graph.FindConnectionsFrom(node.GraphIdentifier, "", ref OutputConnections, EConnectionType.Sequence);
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
            if (NextNode is IterationNode iterNode) {
                process_iteration_node(codeBuilder, iterNode, out NextConnection);
            } else if (NextNode is ControlFlowNode flowNode) {
                process_controlflow_node(codeBuilder, flowNode, out NextConnection);
            } else if (NextNode is FunctionCallNode callNode) {
                process_function_call_node(codeBuilder, callNode, out NextConnection);
            } else if (NextNode is FunctionReturnNode retNode) {
                process_function_return_node(codeBuilder, retNode, out NextConnection);
            } else if (NextNode is LibraryFunctionNodeBase libfuncNode) {
                process_library_function_node(codeBuilder, libfuncNode, out NextConnection);
            } else {
                process_basic_node(codeBuilder, NextNode, out NextConnection);
            }
        }

        protected virtual void process_iteration_node(
            CodeBuilder codeBuilder,
            IterationNode iterationNode,
            out IConnectionInfo NextConnection)
        {
            throw new NotImplementedException();
        }

        protected virtual void process_controlflow_node(
            CodeBuilder codeBuilder,
            ControlFlowNode controlFlowNode,
            out IConnectionInfo NextConnection)
        {
            throw new NotImplementedException();
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
            string comment = $"// [{libraryFuncNode.GraphIdentifier}][library_node] {libraryFuncNode.ToString()}";

            string[] inputTokens = find_input_tokens(libraryFuncNode);
            string[]? OutputVarNames = find_output_variable_names(libraryFuncNode);

            string callingCode = libraryFuncNode.GenerateCode(inputTokens, OutputVarNames);

            codeBuilder.AppendLine(comment);
            codeBuilder.AppendBlock(callingCode);
            codeBuilder.AppendEmptyLine();

            Type usingClass = libraryFuncNode.LibraryClass!;
            if (usingClass.Namespace != null)
                add_namespace(usingClass.Namespace);

            get_outgoing_seq_connection_single(libraryFuncNode, out NextConnection);
        }




        protected virtual void process_basic_node(
            CodeBuilder codeBuilder,
            NodeBase baseNode,
            out IConnectionInfo NextConnection)
        {
            string useName = baseNode.GetNodeName();
            string comment = $"// [{baseNode.GraphIdentifier}][basic_node] {useName}";

            string[]? OutputVarNames = null;
            if (baseNode is ICodeGen codeGen) {

                string[] inputTokens = find_input_tokens(baseNode);
                OutputVarNames = find_output_variable_names(codeGen);

                string callingCode = codeGen.GenerateCode(inputTokens, OutputVarNames);

                codeBuilder.AppendLine(comment);
                codeBuilder.AppendBlock(callingCode);
                codeBuilder.AppendEmptyLine();

            } else {
                codeBuilder.AppendOpenBrace();
                codeBuilder.AppendLine(comment);
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


        protected struct InputCode
        {
            public string Code;

            public InputCode() { 
                Code = "/*(undefined)*/"; 
            }

            public void TrySet(string? code)
            {
                if (code != null)
                    Code = code;
            }
        }


        protected bool find_node_input(NodeBase baseNode, INodeInputInfo inputInfo, out InputCode inputCode)
        {
            inputCode = new();

            IConnectionInfo connection = Graph.FindConnectionTo(baseNode.GraphIdentifier, inputInfo.InputName, EConnectionType.Data);
            if (connection == IConnectionInfo.Invalid) 
            {
                (object? constVal, bool bExists) = Graph.GetConstantValueForInput(baseNode.Handle, inputInfo.InputName);
                if (bExists == false)
                    return false;

                if (constVal == null) 
                {
                    inputCode.TrySet("null");
                }
                else if (constVal is float f) 
                {
                    inputCode.TrySet(f.ToString() + "f");
                } 
                else if (constVal is double d) 
                {
                    inputCode.TrySet(d.ToString());
                }
                else if (constVal is string s) 
                {
                    // todo need to possibly escape string chars?
                    inputCode.TrySet("\"" + s + "\"");
                }
                else 
                {
                    inputCode.TrySet(constVal.ToString());
                }
                return true;
            } 
            else 
            {
                string VarKey = GeneratedVar.MakeKey(connection.FromNodeIdentifier, connection.FromNodeOutputName);
                if (CurrentVariables.TryGetValue(VarKey, out GeneratedVar variable)) {
                    inputCode.TrySet(variable.VariableName);
                    return true;
                }
            }

            return false;
        }


        protected string[] find_input_tokens(NodeBase baseNode)
        {
            List<string> inputs = new List<string>();
            foreach (INodeInputInfo input in baseNode.EnumerateInputs()) 
            {
                bool bFound = find_node_input(baseNode, input, out InputCode inputCode);
                inputs.Add(inputCode.Code);
            }
            return inputs.ToArray();
        }

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
                    CurrentVariables.Add(newVar.Key, newVar);
                }
            }
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


    }
}
