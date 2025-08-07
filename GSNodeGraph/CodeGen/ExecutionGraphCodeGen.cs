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


        protected string allocate_scope_variable(string baseName)
        {
            return baseName + "0";
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
            string returnString = "";
            FunctionNodeInfo funcInfo = libraryFuncNode.FunctionInfo!;
            if ( funcInfo.ReturnType != typeof(void)) {
                string returnVarName = (funcInfo.ReturnName.Length > 0) ? funcInfo.ReturnName : "return";
                returnVarName = allocate_scope_variable(returnVarName);
                returnString = $"{funcInfo.ReturnType.ToString()} {returnVarName} = ";
            }

            string argsString = "...";

            Type usingClass = libraryFuncNode.LibraryClass!;
            if (usingClass.Namespace != null)
                add_namespace(usingClass.Namespace);
            string functionCall = $"{usingClass.Name}.{libraryFuncNode.Function!.Name}( {argsString} );";

            codeBuilder.AppendLine($"// [{libraryFuncNode.GraphIdentifier}][library_node] {libraryFuncNode.ToString()}");
            codeBuilder.AppendLine(returnString + functionCall);
            codeBuilder.AppendEmptyLine();


            get_outgoing_seq_connection_single(libraryFuncNode, out NextConnection);
        }


        protected virtual void process_basic_node(
            CodeBuilder codeBuilder,
            NodeBase baseNode,
            out IConnectionInfo NextConnection)
        {
            string useName = baseNode.GetNodeName();
            codeBuilder.AppendOpenBrace();
            codeBuilder.AppendLine($"// [{baseNode.GraphIdentifier}][basic_node] {useName}");
            codeBuilder.AppendCloseBrace();
            codeBuilder.AppendEmptyLine();

            get_outgoing_seq_connection_single(baseNode, out NextConnection);
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
