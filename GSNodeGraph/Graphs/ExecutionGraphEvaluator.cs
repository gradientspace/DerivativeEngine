using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Gradientspace.NodeGraph.BaseGraph;
using static System.Net.Mime.MediaTypeNames;

namespace Gradientspace.NodeGraph
{
    public class ExecutionGraphEvaluator
    {
        ExecutionGraph Graph;

        public ExecutionGraphEvaluator(ExecutionGraph graph)
        {
            Graph = graph;
        }

        public bool EnableDebugPrinting { get; set; } = false;
        public bool EnableDebugging { get; set; } = false;

        public delegate void EvaluationErrorEvent(string Error, NodeBase? ErrorAtNode);
        public event EvaluationErrorEvent? OnEvaluationErrorEvent;


        int LastNumEvaluatedNodes = 0;

        public void EvaluateGraph()
        {
            init_cache();

            if (EnableDebugging)
                DebugManager.Instance.BeginGraphExecutionDebugSession();

            try
            {
                EvaluateGraph_Internal();
            } 
            catch (EvaluationAbortedException e)
            {
                OnEvaluationErrorEvent?.Invoke("Exception: " + e.Message, e.FailedNode);
            }
            catch (Exception e)
            {
                OnEvaluationErrorEvent?.Invoke( (e.Message.Length > 0) ? e.Message : "(Unspecified Error)", null);
                System.Diagnostics.Debug.WriteLine("ExecutionGraphEvaluator.EvaluateGraph - Caught Exception!!!");
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            if (EnableDebugging)
                DebugManager.Instance.EndGraphExecutionDebugSession();

            reset_cache();
        }



        struct CachedPinKey
        {
            public int NodeIdentifier;
            public string PinName;
        }

        Dictionary<CachedPinKey, object?>? CachedPinData;

        void init_cache()
        {
            CachedPinData = new Dictionary<CachedPinKey, object?>();
        }
        void reset_cache()
        {
            CachedPinData = null;
        }
        void update_cached_pin_data(NodeHandle NodeHandle, NodeBase Node, NamedDataMap outputDatas)
        {
            // if we do not have a sequence-in, this is a pure node?
            IConnectionInfo sequenceConnection = Graph.FindConnectionTo(NodeHandle.Identifier, "", EConnectionType.Sequence);
            if (sequenceConnection.IsValid == false)
                return;

            List<IConnectionInfo> tmp = new List<IConnectionInfo>();
            foreach (DataItem item in outputDatas.Items)
            {
                string OutputPinName = item.Name;
                CachedPinKey pinKey = new() { NodeIdentifier = NodeHandle.Identifier, PinName = OutputPinName };

                if ( item.Value == null )
                {
                    CachedPinData!.Remove(pinKey);
                    continue;
                }

                Graph.FindConnectionsFrom(NodeHandle.Identifier, OutputPinName, ref tmp, EConnectionType.Data);

                // TODO: a lot of improvements possible here...
                bool bOutputInUse = (tmp.Count > 0);

                if (bOutputInUse)
                {
                    if (CachedPinData!.ContainsKey(pinKey))
                        CachedPinData![pinKey] = item.Value;
                    else
                        CachedPinData.Add(pinKey, item.Value);
                }
            }
        }
        object? find_cached_pin_data(NodeHandle FromNodeHandle, string FromNodeOutputPinName)
        {
            CachedPinKey pinKey = new() { NodeIdentifier = FromNodeHandle.Identifier, PinName = FromNodeOutputPinName };

            if (CachedPinData!.TryGetValue(pinKey, out var value))
                return value;
            return null;
        }


        public void EvaluateGraph_Internal()
        {
            LastNumEvaluatedNodes = 0;

            NodeHandle CurrentNodeHandle = Graph.StartNodeHandle;
            NodeBase CurrentNode = Graph.StartNode as NodeBase;

            List<IConnectionInfo> OutputConnections = new List<IConnectionInfo>();
            Graph.FindConnectionsFrom(CurrentNodeHandle.Identifier, "", ref OutputConnections, EConnectionType.Sequence);
            if (OutputConnections.Count == 0)
                return;
            IConnectionInfo NextSequenceConnection = OutputConnections[0];

            if (EnableDebugPrinting)
                System.Console.WriteLine("ExecutionGraphEvaluator.EvaluateGraph: begin graph evaluation");

            bool bDone = false;
            while (!bDone)
            {
                NodeHandle NextNodeHandle = new(NextSequenceConnection.ToNodeIdentifier);
                NodeBase? NextNode = Graph.FindNodeFromHandle(NextNodeHandle) as NodeBase;
                if (NextNode == null)
                    throw new Exception("ExecutionGraphEvaluator.EvaluateStandardNode: next node in sequence could not be found in graph!");

                IConnectionInfo NextConnection = IConnectionInfo.Invalid;
                EvaluateNode(NextNodeHandle, NextNode, out NextConnection);

                if (NextConnection == IConnectionInfo.Invalid) {
                    bDone = true;
                } else {
                    CurrentNodeHandle = NextNodeHandle;
                    CurrentNode = NextNode!;
                    NextSequenceConnection = NextConnection;
                }
            }

            if ( EnableDebugPrinting)
                System.Console.WriteLine("ExecutionGraphEvaluator.EvaluateGraph: evaluated {0} nodes", LastNumEvaluatedNodes);
        }



        protected void EvaluateGraphPath(NodeHandle StartNode, IConnectionInfo OutgoingConnection)
        {
            NodeHandle CurrentNodeHandle = StartNode;
            NodeBase CurrentNode = Graph.FindNodeFromHandle(StartNode)!;
            IConnectionInfo NextSequenceConnection = OutgoingConnection;
            Debug.Assert(CurrentNode != null && NextSequenceConnection.IsValid);

            bool bDone = false;
            while (!bDone)
            {
                NodeHandle NextNodeHandle = new(NextSequenceConnection.ToNodeIdentifier);
                NodeBase? NextNode = Graph.FindNodeFromHandle(NextNodeHandle) as NodeBase;
                if (NextNode == null)
                    throw new Exception("ExecutionGraphEvaluator.EvaluateGraphPath: next node in sequence could not be found in graph!");

                IConnectionInfo NextConnection = IConnectionInfo.Invalid;
                EvaluateNode(NextNodeHandle, NextNode, out NextConnection);

                if (NextConnection == IConnectionInfo.Invalid) {
                    bDone = true;
                } else {
                    CurrentNodeHandle = NextNodeHandle;
                    CurrentNode = NextNode!;
                    NextSequenceConnection = NextConnection;
                }
            }
        }


        protected void EvaluateNode(
            NodeHandle NextNodeHandle,
            NodeBase NextNode,
            out IConnectionInfo NextConnection)
        {
            if (EnableDebugging) DebugManager.Instance.PushActiveNode_Async(NextNodeHandle.Identifier);

            if (NextNode is IterationNode)
            {
                EvaluateIterationNode(NextNodeHandle, NextNode, out NextConnection);
            }
            else if (NextNode is ControlFlowNode)
            {
                EvaluateControlFlowNode(NextNodeHandle, NextNode, out NextConnection);
            }
            else
            {
                EvaluateStandardNode(NextNodeHandle, NextNode, out NextConnection);
            }

            if (EnableDebugging) {
                Thread.Sleep(100);
                DebugManager.Instance.PopActiveNode_Async(NextNodeHandle.Identifier);
            }
        }


        //! run node evaluation and handle any error reporting
        protected virtual void RunNodeEvaluation(NodeBase Node, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            try {
                if (EnableDebugging)
                    System.Console.WriteLine("Evaluating " + Node.GetNodeName());
                Node.Evaluate(in DataIn, RequestedDataOut);
            } catch (Exception e) {
                throw new EvaluationAbortedException(e.Message) { FailedNode = Node } ;
            }
        }


        protected void EvaluateIterationNode(
            NodeHandle NextNodeHandle,
            NodeBase NextNode,
            out IConnectionInfo NextConnection)
        {
            IterationNode IterNode = (NextNode as IterationNode)!;
            Debug.Assert(IterNode != null);

            NextConnection = IConnectionInfo.Invalid;

            // figure out which outputs are in use (should query pins here)
            List<string> RequiredOutputs = new List<string>();
            List<Type> RequiredOutputTypes = new List<Type>();
            foreach (NodeBase.NodeOutputInfo outputInfo in NextNode.Outputs)
            {
                RequiredOutputs.Add(outputInfo.Name);
                RequiredOutputTypes.Add( outputInfo.Output.GetDataType().DataType );
            }

            // add control flow output
            RequiredOutputs.Add(ControlFlowNode.SelectedOutputPathName);
            RequiredOutputTypes.Add(typeof(ControlFlowOutputID));
            RequiredOutputs.Add(IterationNode.ContinueIterationName);
            RequiredOutputTypes.Add(typeof(bool));

            // figure out which inputs are required to compute those outputs
            List<NodeInputRequirement> RequiredInputs = new List<NodeInputRequirement>();
            NextNode.CollectOutputRequirements(RequiredOutputs, RequiredInputs);

            // gather up the incoming data on those inputs
            NamedDataMap InputDatas = new NamedDataMap(RequiredInputs.Count);
            FetchInputDatas(NextNode, NextNodeHandle, RequiredInputs, InputDatas);

            NamedDataMap OutputDatas = new NamedDataMap(RequiredOutputs.Count);
            for (int i = 0; i < RequiredOutputs.Count; ++i)
                OutputDatas.SetItem(i, RequiredOutputs[i], RequiredOutputTypes[i], null);

            // initialize the iteration
            IterNode.InitializeIteration(in InputDatas);


            List<IConnectionInfo> OutputConnections = new List<IConnectionInfo>();
            ControlFlowOutputID SelectedOutput = new ControlFlowOutputID("@invalid");
            bool bContinueIteration = true;
            int counter = 0;
            while (bContinueIteration && counter < 9999999)
            {
                RunNodeEvaluation(IterNode, ref InputDatas, OutputDatas);
                LastNumEvaluatedNodes++;
                update_cached_pin_data(NextNodeHandle, NextNode, OutputDatas);

                OutputDatas.FindItemValueStrict<ControlFlowOutputID>(ControlFlowNode.SelectedOutputPathName, ref SelectedOutput);
                OutputDatas.FindItemValueStrict<bool>(IterationNode.ContinueIterationName, ref bContinueIteration);

                if (bContinueIteration) {
                    OutputConnections.Clear();
                    Graph.FindConnectionsFrom(NextNodeHandle.Identifier, SelectedOutput.OutputPathName, ref OutputConnections, EConnectionType.Sequence);
                    if (OutputConnections.Count == 1)
                        EvaluateGraphPath(NextNodeHandle, OutputConnections[0]);
                }
            }

            // done iteration, follow 'finished' output
            OutputConnections.Clear();
            Graph.FindConnectionsFrom(NextNodeHandle.Identifier, SelectedOutput.OutputPathName, ref OutputConnections, EConnectionType.Sequence);
            if (OutputConnections.Count == 1)
                NextConnection = OutputConnections[0];
        }



        protected void EvaluateControlFlowNode(
            NodeHandle NextNodeHandle,
            NodeBase NextNode,
            out IConnectionInfo NextConnection)
        {
            NextConnection = IConnectionInfo.Invalid;

            // figure out which outputs are in use (should query pins here)
            List<string> RequiredOutputs = new List<string>();
            List<Type> RequiredOutputTypes = new List<Type>();
            foreach (NodeBase.NodeOutputInfo outputInfo in NextNode.Outputs)
            {
                RequiredOutputs.Add(outputInfo.Name);
                RequiredOutputTypes.Add( outputInfo.Output.GetDataType().DataType );
            }

            // add control flow output
            RequiredOutputs.Add(ControlFlowNode.SelectedOutputPathName);
            RequiredOutputTypes.Add(typeof(ControlFlowOutputID));

            // figure out which inputs are required to compute those outputs
            List<NodeInputRequirement> RequiredInputs = new List<NodeInputRequirement>();
            NextNode.CollectOutputRequirements(RequiredOutputs, RequiredInputs);

            // gather up the incoming data on those inputs
            NamedDataMap InputDatas = new NamedDataMap(RequiredInputs.Count);
            FetchInputDatas(NextNode, NextNodeHandle, RequiredInputs, InputDatas);

            NamedDataMap OutputDatas = new NamedDataMap(RequiredOutputs.Count);
            for (int i = 0; i < RequiredOutputs.Count; ++i)
                OutputDatas.SetItem(i, RequiredOutputs[i], RequiredOutputTypes[i], null);

            RunNodeEvaluation(NextNode, ref InputDatas, OutputDatas);
            LastNumEvaluatedNodes++;
            update_cached_pin_data(NextNodeHandle, NextNode, OutputDatas);

            ControlFlowOutputID SelectedOutput = new ControlFlowOutputID();
            OutputDatas.FindItemValueStrict<ControlFlowOutputID>(ControlFlowNode.SelectedOutputPathName, ref SelectedOutput);
            string PathName = SelectedOutput.OutputPathName;

            List<IConnectionInfo> OutputConnections = new List<IConnectionInfo>();
            Graph.FindConnectionsFrom(NextNodeHandle.Identifier, PathName, ref OutputConnections, EConnectionType.Sequence);
            if (OutputConnections.Count > 1)
                throw new EvaluationAbortedException("EvaluateControlFlowNode: found node with more than one output sequence connection?") { FailedNode = NextNode };
            if (OutputConnections.Count == 1)
                NextConnection = OutputConnections[0];
        }


        protected void EvaluateStandardNode(
            NodeHandle NextNodeHandle,
            NodeBase NextNode,
            out IConnectionInfo NextConnection)
        {
            NextConnection = IConnectionInfo.Invalid;

            // figure out which outputs are in use (should query pins here)
            List<string> RequiredOutputs = new List<string>();
            List<Type> RequiredOutputTypes = new List<Type>();
            foreach (NodeBase.NodeOutputInfo outputInfo in NextNode.Outputs) {
                RequiredOutputs.Add(outputInfo.Name);
                RequiredOutputTypes.Add( outputInfo.Output.GetDataType().DataType );
            }

            // figure out which inputs are required to compute those outputs
            List<NodeInputRequirement> RequiredInputs = new List<NodeInputRequirement>();
            NextNode.CollectOutputRequirements(RequiredOutputs, RequiredInputs);

            // gather up the incoming data on those inputs
            NamedDataMap InputDatas = new NamedDataMap(RequiredInputs.Count);
            FetchInputDatas(NextNode, NextNodeHandle, RequiredInputs, InputDatas);

            NamedDataMap OutputDatas = new NamedDataMap(RequiredOutputs.Count);
            for (int i = 0; i < RequiredOutputs.Count; ++i)
                OutputDatas.SetItem(i, RequiredOutputs[i], RequiredOutputTypes[i], null);

            RunNodeEvaluation(NextNode, ref InputDatas, OutputDatas);
            LastNumEvaluatedNodes++;
            update_cached_pin_data(NextNodeHandle, NextNode, OutputDatas);

            List<IConnectionInfo> OutputConnections = new List<IConnectionInfo>();
            Graph.FindConnectionsFrom(NextNodeHandle.Identifier, "", ref OutputConnections, EConnectionType.Sequence);
            if (OutputConnections.Count > 1)
                throw new EvaluationAbortedException("EvaluateStandardNode: found node with more than one output sequence connection?") { FailedNode = NextNode };
            if (OutputConnections.Count == 1)
                NextConnection = OutputConnections[0];
        }


        protected void FetchInputDatas(NodeBase Node, NodeHandle nodeHandle, List<NodeInputRequirement> RequiredInputs, NamedDataMap InputDatas)
        {
            int NumInputs = RequiredInputs.Count;
            for (int k = 0; k < NumInputs; ++k)
            {
                // find the connection to this input
                string InputName = RequiredInputs[k].InputName;
                Connection FoundConnection = Graph.FindDataConnectionForInput(nodeHandle, InputName);

                // if no connection, either use constant value or abort
                if (FoundConnection == Connection.Invalid)
                {
                    // try getting default value
                    (object? ConstantInputData, bool bIsDefined) = Graph.GetConstantValueForInput(nodeHandle, InputName);
                    if (bIsDefined == false)
                        throw new EvaluationAbortedException("[" + InputName + "] no incoming connection and default value is undefined") { FailedNode = Node };
                    if (ConstantInputData != null)
                        InputDatas.SetItem(k, InputName, ConstantInputData);
                    else
                        InputDatas.SetItemNull(k, InputName);

                    continue;
                }

				bool bFoundInputType = Graph.GetInputTypeForNode(nodeHandle, InputName, out GraphDataType inputDataType);
                Debug.Assert(bFoundInputType == true);

				// only need this to handle complex conversions...could gate it on InputData.GetType() != inputDataType.DataType...
				bool bFoundOutputType = Graph.GetOutputTypeForNode(FoundConnection.FromNode, FoundConnection.FromOutput, out GraphDataType outputDataType);
				Debug.Assert(bFoundOutputType == true);

				// if we have already computed the node on the other side of the connection, it's data should be in the pin cache
				object? InputData = find_cached_pin_data(FoundConnection.FromNode, FoundConnection.FromOutput);
                if (InputData != null)
                {
                    try {
                        if (InputData.GetType() != inputDataType.DataType)
                            InputData = ApplyTypeConversionToInputType(InputData, outputDataType, inputDataType);
                    } catch (Exception e) {
						throw new EvaluationAbortedException($"type conversion from output [{FoundConnection.FromOutput}:{TypeUtils.TypeToString(outputDataType)}] to input [{FoundConnection.ToInput}:{TypeUtils.TypeToString(inputDataType)}] failed - {e.Message}") { FailedNode = Node };
					}

					InputDatas.SetItem(k, InputName, InputData);

                    continue;
                }

                // If we have not already computed that data, we can try to "pull" it. 
                // This should only be done for nodes that do not require a sequence path though...?

                InputData = RecursiveComputeNodeOutputData(FoundConnection.FromNode, FoundConnection.FromOutput);
                if (InputData == null)
                {
                    InputDatas.SetItemNull(k, InputName);
                }
                else
                {
					try
					{
						if (InputData.GetType() != inputDataType.DataType)
                           InputData = ApplyTypeConversionToInputType(InputData, outputDataType, inputDataType);
					} catch (Exception e) {
						throw new EvaluationAbortedException($"type conversion from output [{FoundConnection.FromOutput}:{TypeUtils.TypeToString(outputDataType)}] to input [{FoundConnection.ToInput}:{TypeUtils.TypeToString(inputDataType)}] failed - {e.Message}") { FailedNode = Node };
					}
					InputDatas.SetItem(k, InputName, InputData);
                }

                //    throw new EvaluationAbortedException("[" + InputName + "] failed to compute input data") { FailedNode = Node };
            }
        }


        protected object ApplyTypeConversionToInputType(object incomingData, in GraphDataType incomingDataType, in GraphDataType requiredInputType)
        {
            // TODO we could warn or disallow common conversions like float/double -> integral, or even check for out-of-range conversions...

            Type incomingType = incomingData.GetType();
            if (incomingType == requiredInputType.DataType) 
                return incomingData;
            if (incomingType.IsAssignableTo(requiredInputType.DataType)) 
                return incomingData;

            // try registered global conversions
			if (GlobalDataConversionLibrary.Find(incomingDataType, requiredInputType, out IDataTypeConversion? foundConversion)) {
                return foundConversion!.Convert(incomingData);
			}

			// todo other kinds of casts?

			if (incomingData is IConvertible) {
                object? converted = Convert.ChangeType(incomingData, requiredInputType.DataType);
                if (converted != null)
                    incomingData = converted;
            }

            return incomingData;
        }



        public object? RecursiveComputeNodeOutputData(INodeInfo nodeInfo, string OutputName)
        {
            return RecursiveComputeNodeOutputData(new NodeHandle(nodeInfo.Identifier), OutputName);
        }
        public object? RecursiveComputeNodeOutputData(NodeHandle TargetNodeHandle, string OutputName)
        {
            NodeBase? FoundNode = Graph.FindNodeFromHandle(TargetNodeHandle);
            if (FoundNode == null)
                throw new Exception("RecursiveComputeNodeOutputData: could not find TargetNodeHandle");

            if (EnableDebugging) DebugManager.Instance.MarkNodeTouchedThisFrame(TargetNodeHandle.Identifier);

            INodeOutput? FoundOutput = null;
            GraphDataType OutputDataType = GraphDataType.Default;
            List<string> Outputs = new List<string>();
            if (OutputName.Length == 0)
            {
                if (FoundNode.GetType().IsSubclassOf(typeof(SinkNode)) == false)
                    throw new Exception("RecursiveComputeNodeOutputData: no valid output name provided but Node is not a SinkNode");
            }
            else
            {
                FoundOutput = FoundNode.FindOutput(OutputName);
                if (FoundOutput == null)
                    throw new Exception("RecursiveComputeNodeOutputData: FoundNode does not have an output named " + OutputName);
                OutputDataType = FoundOutput.GetDataType();
                Outputs.Add(OutputName);
            }

            List<NodeInputRequirement> RequiredInputs = new List<NodeInputRequirement>();
            FoundNode.CollectOutputRequirements(Outputs, RequiredInputs);
            int NumInputs = RequiredInputs.Count;

            // we are going to fetch the value for each input and store it in this map
            NamedDataMap InputDatas = new NamedDataMap(NumInputs);
            FetchInputDatas(FoundNode, TargetNodeHandle, RequiredInputs, InputDatas);

            // for now this Count is always either 0 or 1...
            NamedDataMap OutputDatas = new NamedDataMap(Outputs.Count);
            for (int i = 0; i < Outputs.Count; ++i)
                OutputDatas.SetItem(i, Outputs[i], OutputDataType.DataType, null);

            RunNodeEvaluation(FoundNode, ref InputDatas, OutputDatas);
            LastNumEvaluatedNodes++;

            update_cached_pin_data(TargetNodeHandle, FoundNode, OutputDatas);

            object? Result = OutputDatas.FindItemValue(OutputName);
            return Result;
        }



        internal class EvaluationAbortedException : Exception
        {
            public NodeBase? FailedNode { get; set; } = null;
            public EvaluationAbortedException(string Message) : base(Message) { }
        }
    }
}
