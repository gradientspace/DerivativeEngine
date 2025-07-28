// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

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
        public bool EnableDebugging { get; 
            set; 
        } = false;

        public delegate void EvaluationErrorEvent(string Error, NodeBase? ErrorAtNode);
        public event EvaluationErrorEvent? OnEvaluationErrorEvent;

        protected StandardVariables? EvalVariables = null;
        protected EvaluationContext? EvalContext = null;
		
        public IVariablesInterface? ActiveVariables { get { return EvalVariables; } }


		int LastNumEvaluatedNodes = 0;

        public void EvaluateGraph()
        {
            init_cache();

            FunctionReturnsHack.Clear();

            EvalVariables = new StandardVariables();
            EvalContext = new EvaluationContext() { Variables = this.EvalVariables };

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
				GlobalGraphOutput.AppendError("ExecutionGraphEvaluator.EvaluateGraph - Caught Exception!!!");
				GlobalGraphOutput.AppendError(e.ToString());
            }

            if (EnableDebugging)
                DebugManager.Instance.EndGraphExecutionDebugSession();

            reset_cache();
		}







/**
    - EvaluateGraph():
        - does some pre/post evaluation setup, calls EvaluateGraph_Internal() to do the work

    - EvaluateGraph_Internal() :
        - starts at Graph.StartNodeHandle
	    - repeatedly calls EvaluateNode() on each node in the sequence, which returns a next-connection that is followed
	    - terminates when no next-connection is found
	 
    - EvaluateNode() calls either EvaluateStandardNode(), EvaluateControlFlowNode(), or EvaluateIterationNode() depending on the node type

    - EvaluateStandardNode(node, out next_connection) :
	    - finds required inputs from the set of required outputs(currently always all outputs)
	    - calls FetchInputDatas() to populate a NamedDataMap of(input-pin, input-data) pairs
		    (this may recursively evaluate things, etc)
	    - calls RunNodeEvaluation() to evaluate the node and fill a NamedDataMap for the(output-pin, output-data) pairs
	    - calls update_cached_pin_data() to save the output data in the cache
	    - finds the next output-sequence pin/connection to follow, if it exists
	
    - FetchInputDatas(node, required_inputs, inout NamedDataMap input_data) :
	    - this function finds the incoming data on each input pin for a node, and stores it in a NamedDataMap
	    - might come from a constant-value on that pin, or from the output-pin-data cache(via find_cached_pin_data())
	    - may call RecursiveComputeNodeOutputData() to try to compute/"pull" missing data(only viable for sets of non-sequence nodes like math/etc, eg "pure")
	    - calls ApplyTypeConversionToInputType() to try to convert to input-pin type if it's different
	
	
    - ApplyTypeConversionToInputType(object, inputType, outputType) :
	    - tries various methods to convert an output data to be the right type for an input pin
	    - C#-level conversions via Type.IsAssignableTo and IConvertible
	    - custom conversions via GlobalDataConversionLibrary
	
    - RecursiveComputeNodeOutputData(node, output) :
	    - tries to do "pull"/dataflow-style evaluation of a node+output without following sequence connections
	    - similar to EvaluateStandardNode() in that it figures out required inputs, calls FetchInputDatas(), RunNodeEvaluation(), update_cached_pin_data()
	    - FetchInputDatas() may involve recursive calls to RecursiveComputeNodeOutputData()

    - EvaluateControlFlowNode() :
	    - similar to EvaluateStandardNode(), but deals with selecting 1 of N output pins on ControlFlowNode-subclasses

    - EvaluateIterationNode() :
	    - similar to EvaluateStandardNode(), but handles special IterationNode types
	    - IterationNode evalution returns either a continue-iteration output seq pin, or a done-interation seq pin
	    - for continue-iteration, EvaluateGraphPath() is called to evaluate the entire iteration sequence path
	    - for done-iteration, returns next-connection as usual

    - EvaluateGraphPath(start_node, start_connection) :
	    - very similar to EvaluateGraph_Internal(), but starts at a specific node+connection and follows the path that defines

    ===============
    Pin Data Cache
    ===============
    - CachedPinData map stores calculated data on output pins in the graph. 
    - update_cached_pin_data(node, in NamedDataMap outputData): 
	    - given NamedDataMap of output data on a Node, stores those datas in the cache if the output is in use downstream (ie pin is connected)
    - find_cached_pin_data(node, output_pin):
	    - finds data in the cache if it is available
**/	



        // Output Pin Data Caching
        //    (should be moved to a standalone object)


        struct CachedPinKey
        {
            public int NodeIdentifier;
            public string PinName;
        }

        // This map stores data objects on output pins, it is populated and
        // updated during graph evaluation
        Dictionary<CachedPinKey, object?>? CachedPinData;

        void init_cache()
        {
            CachedPinData = new Dictionary<CachedPinKey, object?>();
        }
        void reset_cache()
        {
            CachedPinData = null;
        }
        //! given a NamedDataMap of output data at a Node, save each datum in the cache
        //! if it is going to be used (ie if pin is connected)
        void update_cached_pin_data(NodeHandle NodeHandle, NodeBase Node, in NamedDataMap outputDatas)
        {
            bool bForceStorage = (Node is FunctionDefinitionNode);

            // if we do not have a sequence-in, this is a pure node?
            IConnectionInfo sequenceConnection = Graph.FindConnectionTo(NodeHandle.Identifier, "", EConnectionType.Sequence);
            if (sequenceConnection.IsValid == false && bForceStorage == false)
                return;

            List<IConnectionInfo> tmp = new List<IConnectionInfo>();
            foreach (DataItem item in outputDatas.Items)
            {
                string OutputPinName = item.Name;
                CachedPinKey pinKey = new() { NodeIdentifier = NodeHandle.Identifier, PinName = OutputPinName };

                // todo why were we removing from cache on null value? what is the situation in which that happens??
                //if (item.Value == null)
                //{
                //    CachedPinData!.Remove(pinKey);
                //    continue;
                //}

                Graph.FindConnectionsFrom(NodeHandle.Identifier, OutputPinName, ref tmp, EConnectionType.Data);

                // TODO: a lot of improvements possible here...   (like?)
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
        //! try to find existing cached data on an output pin of a node, or null if not found
        (object?,bool) find_cached_pin_data(NodeHandle FromNodeHandle, string FromNodeOutputPinName)
        {
            CachedPinKey pinKey = new() { NodeIdentifier = FromNodeHandle.Identifier, PinName = FromNodeOutputPinName };

            if (CachedPinData!.TryGetValue(pinKey, out var value))
                return (value, true);
            return (null, false);
        }


		//! top-level evaluation function
        protected virtual void EvaluateGraph_Internal()
        {
            LastNumEvaluatedNodes = 0;

            // start evaluation at the start of the graph, but CurrentNode/Handle will be
            // updated during the evaluation
            NodeHandle CurrentNodeHandle = Graph.StartNodeHandle;
            NodeBase CurrentNode = Graph.StartNode as NodeBase;

            // Find output sequence connection from the current node, ie the next node
            List<IConnectionInfo> OutputConnections = new List<IConnectionInfo>();
            Graph.FindConnectionsFrom(CurrentNodeHandle.Identifier, "", ref OutputConnections, EConnectionType.Sequence);
            if (OutputConnections.Count == 0)
                return;
            IConnectionInfo NextSequenceConnection = OutputConnections[0];

            if (EnableDebugPrinting)
                GlobalGraphOutput.AppendLine("ExecutionGraphEvaluator.EvaluateGraph: begin graph evaluation", EGraphOutputType.Logging);

            // iterate until we can't find another sequence connection
            // (potentially this block can be replaced by a call to EvaluateGraphPath()...)
            bool bDone = false;
            while (!bDone)
            {
                // NextNode is the node we will evaluate
                NodeHandle NextNodeHandle = new(NextSequenceConnection.ToNodeIdentifier);
                NodeBase? NextNode = Graph.FindNodeFromHandle(NextNodeHandle) as NodeBase;
                if (NextNode == null)
                    throw new Exception("ExecutionGraphEvaluator.EvaluateStandardNode: next node in sequence could not be found in graph!");

				// evaluate it. EvaluateNode() returns the connection to the next-node in the sequence path.
				// Note that EvaluateNode() may evaluate entire sequence sub-paths via EvaluateGraphPath() for
                // things like loops, etc
                IConnectionInfo NextConnection = IConnectionInfo.Invalid;
                EvaluateNode(NextNodeHandle, NextNode, out NextConnection);

                // if we have a new next-connection, continue, otherwise we are done
                if (NextConnection == IConnectionInfo.Invalid) {
                    bDone = true;
                } else {
                    // note: not actually using CurrentNode/CurrentNodeHandle...
                    CurrentNodeHandle = NextNodeHandle;
                    CurrentNode = NextNode!;
                    NextSequenceConnection = NextConnection;
                }
            }

            if ( EnableDebugPrinting)
				GlobalGraphOutput.AppendLine(String.Format("ExecutionGraphEvaluator.EvaluateGraph: evaluated {0} nodes", LastNumEvaluatedNodes), EGraphOutputType.Logging);
        }


		// EvaluateGraphPath() evaluates the graph from a StartNode/Connection until no further sequence
		// connection is found. IE similar to EvaluateGraph_Internal() but for an internal sequence path, 
		// like the iteration pin of a for-loop
		protected void EvaluateGraphPath(NodeHandle StartNode, IConnectionInfo OutgoingConnection, out NodeBase? LastNodeInPath)
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
                CurrentNodeHandle = NextNodeHandle;
                CurrentNode = NextNode;

                if (NextConnection == IConnectionInfo.Invalid) {
                    bDone = true;
                } else {
                    NextSequenceConnection = NextConnection;
                }
            }

            LastNodeInPath = CurrentNode;
        }
        protected void EvaluateGraphPath(NodeHandle StartNode, IConnectionInfo OutgoingConnection)
        {
            EvaluateGraphPath(StartNode, OutgoingConnection, out NodeBase? LastNodeInPath);
        }

        // Top-level Node evaluation function. Evaluates a single node and outputs the sequenece connection
        // to the node that should be evaluated next, if there is one
        protected void EvaluateNode(
            NodeHandle NextNodeHandle,
            NodeBase NextNode,
            out IConnectionInfo NextConnection)
        {
            // tell the debug manager we are in this node
            if (EnableDebugging) 
                DebugManager.Instance.PushActiveNode_Async(NextNodeHandle.Identifier);

            // some node types are special and are evaluated differently than "standard" nodes
            // that have only have one sequence output
            if (NextNode is IterationNode)
            {
                EvaluateIterationNode(NextNodeHandle, NextNode, out NextConnection);
            }
            else if (NextNode is ControlFlowNode)
            {
                EvaluateControlFlowNode(NextNodeHandle, NextNode, out NextConnection);
            }
            else if (NextNode is FunctionCallNode callNode) 
            {
                EvaluateFunctionCallNode(NextNodeHandle, callNode, out NextConnection);
            }
            else if (NextNode is FunctionReturnNode retNode) 
            {
                EvaluateFunctionReturnNode(NextNodeHandle, retNode, out NextConnection);
            }
            else {
                EvaluateStandardNode(NextNodeHandle, NextNode, out NextConnection);
            }

            if (EnableDebugging) {
                Thread.Sleep(100);      // possibly DebugManager should be doing this...
                DebugManager.Instance.PopActiveNode_Async(NextNodeHandle.Identifier);
            }
        }


        //! Actually call the Node.Evaluate() function for a node, with the given Input datamap and output datamap
        //! This wrapper exists mainly for debugging support and error-handling
        protected virtual void RunNodeEvaluation(NodeBase Node, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            try {
                if (EnableDebugging)
					GlobalGraphOutput.AppendLine("Evaluating " + Node.GetNodeName(), EGraphOutputType.Logging);
                Node.Evaluate(this.EvalContext!, in DataIn, RequestedDataOut);
            } catch (Exception e) {
                throw new EvaluationAbortedException(e.Message) { FailedNode = Node } ;
            }
        }


		// Evaluate an IterationNode-type node, which will recursively evaluate a graph path
        // multiple times before continuing the calling path evaluation
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

            // add special outputs that Iteration nodes must have to support 
            // detecting if we should keep iterating, on a specific output, or
            // terminate the iteration
            RequiredOutputs.Add(ControlFlowNode.SelectedOutputPathName);
            RequiredOutputTypes.Add(typeof(ControlFlowOutputID));
            RequiredOutputs.Add(IterationNode.ContinueIterationName);
            RequiredOutputTypes.Add(typeof(bool));

            // figure out which inputs are required to compute those outputs
            List<NodeInputRequirement> RequiredInputs = new List<NodeInputRequirement>();
            NextNode.CollectOutputRequirements(RequiredOutputs, RequiredInputs);

            // gather up the incoming data on those inputs (which may evaluate non-sequence nodes!)
            NamedDataMap InputDatas = new NamedDataMap(RequiredInputs.Count);
            FetchInputDatas(NextNode, NextNodeHandle, RequiredInputs, InputDatas);

            // populate output datamap
            NamedDataMap OutputDatas = new NamedDataMap(RequiredOutputs.Count);
            for (int i = 0; i < RequiredOutputs.Count; ++i)
                OutputDatas.SetItem(i, RequiredOutputs[i], RequiredOutputTypes[i], null);

            // initialize the iteration
            IterNode.InitializeIteration(in InputDatas);

            // repeatedly evaluate the iteration node, to find out (a) which output sequence pin
            // should be followed and (b) if we are continuing to iterate or terminating.
            // If we are continuing to iterate, evaluate path starting at the returned output pin/connection
            // (assumption is that this is not the "terminate and continue" pin)
            List<IConnectionInfo> OutputConnections = new List<IConnectionInfo>();
            ControlFlowOutputID SelectedOutput = new ControlFlowOutputID("@invalid");
            bool bContinueIteration = true;
            int counter = 0;
            while (bContinueIteration && counter < 9999999)
            {
                // evaluate the iteration node
                RunNodeEvaluation(IterNode, ref InputDatas, OutputDatas);
                LastNumEvaluatedNodes++;
                update_cached_pin_data(NextNodeHandle, NextNode, OutputDatas);

                // get the current output sequence pin and continue values
                OutputDatas.FindItemValueStrict<ControlFlowOutputID>(ControlFlowNode.SelectedOutputPathName, ref SelectedOutput);
                OutputDatas.FindItemValueStrict<bool>(IterationNode.ContinueIterationName, ref bContinueIteration);

                // if we are continuing, evaluate the graph path on the output sequence pin
                if (bContinueIteration) {
                    OutputConnections.Clear();
                    Graph.FindConnectionsFrom(NextNodeHandle.Identifier, SelectedOutput.OutputPathName, ref OutputConnections, EConnectionType.Sequence);
                    if (OutputConnections.Count == 1)
                        EvaluateGraphPath(NextNodeHandle, OutputConnections[0]);
                }
            }

            // now we are done the iteration, and return the final output sequenece pin/connection as the next-connection
            OutputConnections.Clear();
            Graph.FindConnectionsFrom(NextNodeHandle.Identifier, SelectedOutput.OutputPathName, ref OutputConnections, EConnectionType.Sequence);
            if (OutputConnections.Count == 1)
                NextConnection = OutputConnections[0];
        }


		// Evaluate a ControlFlowNode-type node, eg like a branch, where there are multiple possible
        // output sequence pins but only one will be followed
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

            // gather up the incoming data on those inputs (which may evaluate non-sequence nodes!)
            NamedDataMap InputDatas = new NamedDataMap(RequiredInputs.Count);
            FetchInputDatas(NextNode, NextNodeHandle, RequiredInputs, InputDatas);

            // populate output datamap
            NamedDataMap OutputDatas = new NamedDataMap(RequiredOutputs.Count);
            for (int i = 0; i < RequiredOutputs.Count; ++i)
                OutputDatas.SetItem(i, RequiredOutputs[i], RequiredOutputTypes[i], null);

            // evaluate the ControlFlow node
            RunNodeEvaluation(NextNode, ref InputDatas, OutputDatas);
            LastNumEvaluatedNodes++;
            update_cached_pin_data(NextNodeHandle, NextNode, OutputDatas);

            // figure out which output sequence pin the evaluation returned
            ControlFlowOutputID SelectedOutput = new ControlFlowOutputID();
            OutputDatas.FindItemValueStrict<ControlFlowOutputID>(ControlFlowNode.SelectedOutputPathName, ref SelectedOutput);
            string PathName = SelectedOutput.OutputPathName;

            // find the connection from that sequence pin, if it exists, and return it via NextConnection
            List<IConnectionInfo> OutputConnections = new List<IConnectionInfo>();
            Graph.FindConnectionsFrom(NextNodeHandle.Identifier, PathName, ref OutputConnections, EConnectionType.Sequence);
            if (OutputConnections.Count > 1)
                throw new EvaluationAbortedException("EvaluateControlFlowNode: found node with more than one output sequence connection?") { FailedNode = NextNode };
            if (OutputConnections.Count == 1)
                NextConnection = OutputConnections[0];
        }


        // Standard Nodes have one input sequence pin and one output sequence pin, so all we are
        // doing is evaluating the node and then returning the unambiguous output connection
        protected void EvaluateStandardNode(
            NodeHandle NextNodeHandle,
            NodeBase NextNode,
            out IConnectionInfo NextConnection)
        {
            NextConnection = IConnectionInfo.Invalid;

            // figure out which outputs are in use
            // (note: probably ought to be checking which outputs are actually connected here...)
            List<string> RequiredOutputs = new List<string>();
            List<Type> RequiredOutputTypes = new List<Type>();
            foreach (NodeBase.NodeOutputInfo outputInfo in NextNode.Outputs) {
                RequiredOutputs.Add(outputInfo.Name);
                RequiredOutputTypes.Add( outputInfo.Output.GetDataType().DataType );
            }

            // figure out which inputs are required to compute those outputs
            List<NodeInputRequirement> RequiredInputs = new List<NodeInputRequirement>();
            NextNode.CollectOutputRequirements(RequiredOutputs, RequiredInputs);

            // gather up the incoming data on those inputs (which may recursively evaluate non-sequence nodes!)
            NamedDataMap InputDatas = new NamedDataMap(RequiredInputs.Count);
            FetchInputDatas(NextNode, NextNodeHandle, RequiredInputs, InputDatas);

            // populate the required-outputs datamap
            NamedDataMap OutputDatas = new NamedDataMap(RequiredOutputs.Count);
            for (int i = 0; i < RequiredOutputs.Count; ++i)
                OutputDatas.SetItem(i, RequiredOutputs[i], RequiredOutputTypes[i], null);

            // run the node evaluation and store the outputs in the cache
            RunNodeEvaluation(NextNode, ref InputDatas, OutputDatas);
            LastNumEvaluatedNodes++;
            update_cached_pin_data(NextNodeHandle, NextNode, OutputDatas);

            // find the output connection if it exist and return via NextConnection
            List<IConnectionInfo> OutputConnections = new List<IConnectionInfo>();
            Graph.FindConnectionsFrom(NextNodeHandle.Identifier, "", ref OutputConnections, EConnectionType.Sequence);
            if (OutputConnections.Count > 1)
                throw new EvaluationAbortedException("EvaluateStandardNode: found node with more than one output sequence connection?") { FailedNode = NextNode };
            if (OutputConnections.Count == 1)
                NextConnection = OutputConnections[0];
        }


        // this is used as a sort of stack mechanism, where a Return node pushes it's input NamedDataMap
        // by FunctionID, and the parent EvaluateFunctionCallNode finds it. Note that currently this
        // prevents recursion.
        // (if we actually used a stack here...would recursion work? seems like it....)
        Dictionary<string, NamedDataMap> FunctionReturnsHack = new Dictionary<string, NamedDataMap>();


        protected void EvaluateFunctionCallNode(
            NodeHandle CallNodeHandle,
            FunctionCallNode CallNode,
            out IConnectionInfo NextConnection)
        {
            NextConnection = IConnectionInfo.Invalid;

            // figure out which outputs are in use
            List<string> RequiredOutputs = new List<string>();
            List<Type> RequiredOutputTypes = new List<Type>();
            foreach (NodeBase.NodeOutputInfo outputInfo in CallNode.Outputs) {
                RequiredOutputs.Add(outputInfo.Name);
                RequiredOutputTypes.Add(outputInfo.Output.GetDataType().DataType);
            }

            // figure out which inputs are required to compute those outputs
            List<NodeInputRequirement> RequiredInputs = new List<NodeInputRequirement>();
            CallNode.CollectOutputRequirements(RequiredOutputs, RequiredInputs);

            // gather up the incoming data on those inputs (which may recursively evaluate non-sequence nodes!)
            NamedDataMap CallInputDatas = new NamedDataMap(RequiredInputs.Count);
            FetchInputDatas(CallNode, CallNodeHandle, RequiredInputs, CallInputDatas);

            // populate the required-outputs datamap
            NamedDataMap CallOutputDatas = new NamedDataMap(RequiredOutputs.Count);
            for (int i = 0; i < RequiredOutputs.Count; ++i)
                CallOutputDatas.SetItem(i, RequiredOutputs[i], RequiredOutputTypes[i], null);

            if (EnableDebugging)
                GlobalGraphOutput.AppendLine("Evaluating Function Call " + CallNode.GetNodeName(), EGraphOutputType.Logging);

            // find the FunctionDefinition node
            FunctionDefinitionNode? FuncNode = null;
            foreach (INodeInfo nodeInfo in Graph.EnumerateNodes()) {
                if (nodeInfo.Node is FunctionDefinitionNode defNode) {
                    if (defNode.FunctionID == CallNode.FunctionID) {
                        FuncNode = defNode;
                        break;
                    }
                }
            }
            if (FuncNode == null)
                throw new EvaluationAbortedException($"EvaluateFunctionCallNode - function {CallNode.FunctionName} with ID {CallNode.FunctionID} could not be found") { FailedNode = CallNode };
            NodeHandle FuncNodeHandle = new NodeHandle(FuncNode.GraphIdentifier);

            // find the sequence connection leaving the FunctionDefinition node
            List<IConnectionInfo> FuncSequenceConnections = new List<IConnectionInfo>();
            Graph.FindConnectionsFrom(FuncNode.GraphIdentifier, "", ref FuncSequenceConnections, EConnectionType.Sequence);
            IConnectionInfo FuncNodeConnection = FuncSequenceConnections[0];

            // The FunctionDefinition node input pins and output pins are the same,
            // and should have the same names/types as the FunctionCall input pins. 
            // These are all the same data and so we can use the InputDatas on the FunctionCall
            // as the output datamap for the FunctionDefinition node
            //
            // NOTE: this prevents recursion and also any kind of nested function calls, 
            // as the second call will overwrite the cached data for the first! 
            // Need a more reliable way to do this...maybe with scope??
            update_cached_pin_data(FuncNodeHandle, FuncNode, CallInputDatas);

            // now we can evaluate the function graph
            EvaluateGraphPath(FuncNodeHandle, FuncNodeConnection, out NodeBase? LastNodeInPath);

            NamedDataMap? returnOutputDatas = null;
            bool bReturnRequired = (RequiredOutputs.Count > 0);
            if (LastNodeInPath is FunctionReturnNode returnNode) {
                if ( FunctionReturnsHack.ContainsKey(FuncNode.FunctionID) == false )
                    throw new EvaluationAbortedException($"EvaluateFunctionCallNode - cannot find return data for {FuncNode.FunctionName} call") { FailedNode = FuncNode };
                returnOutputDatas = FunctionReturnsHack[FuncNode.FunctionID];
                FunctionReturnsHack.Remove(FuncNode.FunctionID);
            } else if (bReturnRequired)
                throw new EvaluationAbortedException($"EvaluateFunctionCallNode - {FuncNode.FunctionName} terminated without a Return node") { FailedNode = FuncNode };

            // run the node evaluation and store the outputs in the cache
            LastNumEvaluatedNodes++;
            if (returnOutputDatas != null)
                update_cached_pin_data(CallNodeHandle, CallNode, returnOutputDatas);

            // find the output connection if it exist and return via NextConnection
            List<IConnectionInfo> OutputConnections = new List<IConnectionInfo>();
            Graph.FindConnectionsFrom(CallNodeHandle.Identifier, "", ref OutputConnections, EConnectionType.Sequence);
            if (OutputConnections.Count > 1)
                throw new EvaluationAbortedException("EvaluateFunctionCallNode: found node with more than one output sequence connection?") { FailedNode = CallNode };
            if (OutputConnections.Count == 1)
                NextConnection = OutputConnections[0];
        }


        protected void EvaluateFunctionReturnNode(
            NodeHandle ReturnNodeHandle,
            FunctionReturnNode ReturnNode,
            out IConnectionInfo NextConnection)
        {
            NextConnection = IConnectionInfo.Invalid;       // always the case for a return node...

            // figure out which outputs are in use
            List<string> RequiredOutputs = new List<string>();
            List<Type> RequiredOutputTypes = new List<Type>();
            foreach (NodeBase.NodeOutputInfo outputInfo in ReturnNode.Outputs) {
                RequiredOutputs.Add(outputInfo.Name);
                RequiredOutputTypes.Add(outputInfo.Output.GetDataType().DataType);
            }

            // figure out which inputs are required to compute those outputs
            List<NodeInputRequirement> RequiredInputs = new List<NodeInputRequirement>();
            ReturnNode.CollectOutputRequirements(RequiredOutputs, RequiredInputs);

            // gather up the incoming data on those inputs (which may recursively evaluate non-sequence nodes!)
            NamedDataMap InputDatas = new NamedDataMap(RequiredInputs.Count);
            FetchInputDatas(ReturnNode, ReturnNodeHandle, RequiredInputs, InputDatas);

            if (EnableDebugging)
                GlobalGraphOutput.AppendLine("Evaluating Function Return " + ReturnNode.GetNodeName(), EGraphOutputType.Logging);

            // The inputs to Return are the outputs to Call. We can't directly access the Call
            // here, but any evaluation of this function is running inside an EvaluateFunctionCallNode.
            // So we store the NamedDataMap by UUID and let the call pick it up. 
            // (this hack prevents recursion...)
            if (FunctionReturnsHack.ContainsKey(ReturnNode.FunctionID))
                throw new Exception("EvaluateFunctionReturnNode - recursion not supported");
            FunctionReturnsHack.Add(ReturnNode.FunctionID, InputDatas);
        }



        // FetchInputDatas looks at a list of required input-pins at a Node, and gets the incoming data on each pin, and stores
        // that (pin,datum) info in a NamedDataMap. There are various cases/complications because:
        //   1) a pin might not be connected to anything, but have constant data defined on the input  (eg like a float or string input)
        //   2) the output pin on the other side of the input pin connection may have it's data cached already
        //   3) if the data isn't cached already, we need to recursively compute it (only possible for nodes that don't require a sequence connection, like math/etc)
        //   4) we might need to apply automatic type conversions
        // 
        protected void FetchInputDatas(NodeBase Node, NodeHandle nodeHandle, List<NodeInputRequirement> RequiredInputs, /*inout*/ NamedDataMap InputDatas)
        {
            int NumInputs = RequiredInputs.Count;
            for (int k = 0; k < NumInputs; ++k)
            {
                // find the connection to this input, if one exists
                string InputName = RequiredInputs[k].InputName;
                BaseGraph.Connection FoundConnection = Graph.FindDataConnectionForInput(nodeHandle, InputName);

                // if no connection, either use constant value stored on the pin, or abort for ths input (probably causes evaluation failure)
                if (FoundConnection == BaseGraph.Connection.Invalid)
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

                // find the type for the input
				bool bFoundInputType = Graph.GetInputTypeForNode(nodeHandle, InputName, out GraphDataType inputDataType);
                Debug.Assert(bFoundInputType == true);

				// only need this to handle complex conversions...could gate it on InputData.GetType() != inputDataType.DataType...
				bool bFoundOutputType = Graph.GetOutputTypeForNode(FoundConnection.FromNode, FoundConnection.FromOutput, out GraphDataType outputDataType);
				Debug.Assert(bFoundOutputType == true);

				// if we have already computed the node on the other side of the connection, it's data should be in the output-pin cache
                (object? InputData, bool bFound) = find_cached_pin_data(FoundConnection.FromNode, FoundConnection.FromOutput);
                if (bFound)
                {
	                if (InputData == null)
	                {
		                InputDatas.SetItemNull(k, InputName);
	                } 
                    else
	                {
		                // we might need to convert the cached data to another type   (should that also be cached?? this could be expensive...)
		                try
		                {
			                if (InputData.GetType() != inputDataType.DataType)
				                InputData = ApplyTypeConversionToInputType(InputData, outputDataType, inputDataType);
		                } catch (Exception e)
		                {
			                throw new EvaluationAbortedException($"type conversion from output [{FoundConnection.FromOutput}:{TypeUtils.TypeToString(outputDataType)}] to input [{FoundConnection.ToInput}:{TypeUtils.TypeToString(inputDataType)}] failed - {e.Message}") { FailedNode = Node };
		                }

		                InputDatas.SetItem(k, InputName, InputData);
	                }

	                continue;
                }

                // If we have not already computed and cached that data, we can try to "pull" it. 
                // This should only be done for nodes that do not require a sequence path though...currently no way to detect that

                InputData = RecursiveComputeNodeOutputData(FoundConnection.FromNode, FoundConnection.FromOutput);
                if (InputData == null)
                {
                    InputDatas.SetItemNull(k, InputName);
                }
                else
                {
					// we might need to convert the computed data to another type
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


        // convert data coming out of an output pin to the type required by an input pin, if possible
        protected object ApplyTypeConversionToInputType(object incomingData, in GraphDataType incomingDataType, in GraphDataType requiredInputType)
        {
            // TODO we could warn or disallow common conversions like float/double -> integral, or even check for out-of-range conversions...

            // trivial cases - no conversion necessary, or direct C# assignment between types is possible
            Type incomingType = incomingData.GetType();
            if (incomingType == requiredInputType.DataType) 
                return incomingData;
            if (incomingType.IsAssignableTo(requiredInputType.DataType)) 
                return incomingData;

            // try registered global conversions
			if (GlobalDataConversionLibrary.Find(incomingDataType, requiredInputType, out IDataTypeConversion? foundConversion)) {
                return foundConversion!.Convert(incomingData);
			}

			// todo other kinds of C#-level casts?

            // try standard IConvertible interface - this only converts to POD types so we possibly
            // could avoid this test if the required type is an object/etc...
			if (incomingData is IConvertible) {
                object? converted = Convert.ChangeType(incomingData, requiredInputType.DataType);
                if (converted != null)
                    incomingData = converted;
            }

            // if we didn't convert we just return the input data and allow higher levels
            // of the graph to throw an Exception/etc
            return incomingData;
        }


        // given a Node and an Output data pin, this function tries to recursively evaluate that Node 
        // and any upstream nodes to make sure that OutputName is available. IE this basically implements
        // "pull"/dataflow evaluation. This is called by FetchInputDatas(), which is the place where
        // we might discover that we need data we do not have.
        //
        // The assumption is that no Sequence pins/connections are involved.
        // Currently this is not enforced
        //
        // Note that any output pins/data calculated here will be cached
        public object? RecursiveComputeNodeOutputData(NodeHandle TargetNodeHandle, string OutputName)
        {
            // find our node
            NodeBase? FoundNode = Graph.FindNodeFromHandle(TargetNodeHandle);
            if (FoundNode == null)
                throw new Exception("RecursiveComputeNodeOutputData: could not find TargetNodeHandle");

            // any nodes evaluated in this function are evaluated "with" a node-eval frame in the sequenece path
            if (EnableDebugging) 
                DebugManager.Instance.MarkNodeTouchedThisFrame(TargetNodeHandle.Identifier);

            // find the output pin on the node given it's OutputName. 
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

            // note this code squenece is very similar to EvaluateStandardNode(), possibly refactorable?

            // collect up required inputs to evaluate our output
            List<NodeInputRequirement> RequiredInputs = new List<NodeInputRequirement>();
            FoundNode.CollectOutputRequirements(Outputs, RequiredInputs);
            int NumInputs = RequiredInputs.Count;

            // fetch the incoming data for each input and store it in this map. This is where
            // recursive calls to ourself (RecursiveComputeNodeOutputData) may occur.
            NamedDataMap InputDatas = new NamedDataMap(NumInputs);
            FetchInputDatas(FoundNode, TargetNodeHandle, RequiredInputs, InputDatas);

            // for now this Count is always either 0 or 1...
            NamedDataMap OutputDatas = new NamedDataMap(Outputs.Count);
            for (int i = 0; i < Outputs.Count; ++i)
                OutputDatas.SetItem(i, Outputs[i], OutputDataType.DataType, null);

            // run the node evaluation and store the outputs in the cache
            RunNodeEvaluation(FoundNode, ref InputDatas, OutputDatas);
            LastNumEvaluatedNodes++;
            update_cached_pin_data(TargetNodeHandle, FoundNode, OutputDatas);

            // return the data object that came back from evaluation on the output pin
            object? Result = OutputDatas.FindItemValue(OutputName);
            return Result;
        }
        public object? RecursiveComputeNodeOutputData(INodeInfo nodeInfo, string OutputName)
        {
	        return RecursiveComputeNodeOutputData(new NodeHandle(nodeInfo.Identifier), OutputName);
        }


        internal class EvaluationAbortedException : Exception
        {
            public NodeBase? FailedNode { get; set; } = null;
            public EvaluationAbortedException(string Message) : base(Message) { }
        }
    }
}
