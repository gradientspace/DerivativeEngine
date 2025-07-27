// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace Gradientspace.NodeGraph
{

    public struct NodeInputRequirement
    {
        public string InputName;
        // flags?
    }



    public abstract class NodeBase : INode
    {
        public abstract string GetDefaultNodeName();
        public virtual string GetCustomNodeName()
        {
            return GetDefaultNodeName();
        }

        public struct NodeInputInfo
        {
            public string Name;
            public int LastTimestamp;
            public INodeInput Input;
            public INodeInputInfo AsInterface { get { return new INodeInputInfo() { Input = this.Input, InputName = this.Name }; } }
		};

        public struct NodeOutputInfo
        {
            public string Name;
            public INodeOutput Output;
        }

        public List<NodeInputInfo> Inputs;
        public List<NodeOutputInfo> Outputs;

        public NodeBase()
        {
            Inputs = new List<NodeInputInfo>();
            Outputs = new List<NodeOutputInfo>();
        }

        //! the identifier of this node in the Graph that owns it
        public int GraphIdentifier { get; private set; } = -1;
        //! the type of this node in the NodeLibrary that owns it
        public NodeType? LibraryNodeType { get; private set; } = null;
        
        //! configure external NodeGraph/NodeLibrary info in an instance of this node
        internal void SetOwningGraphInfo(int useGraphIdentifier, NodeType? useNodeType)
        {
            GraphIdentifier = useGraphIdentifier;
            LibraryNodeType = useNodeType;
        }



        public bool AddInput(string Name, INodeInput Input)
        {
            if ( Inputs.Exists(x => x.Name == Name) )
            {
                GlobalGraphOutput.AppendLine("INPUT " + Name + "ALREADY EXISTS!", EGraphOutputType.GraphWarning);
                return false;
            }
            NodeInputInfo InputInfo = new();
            InputInfo.Name = Name;
            InputInfo.LastTimestamp = -1;
            InputInfo.Input = Input;
            Inputs.Add(InputInfo);
            return true;
        }


        public bool ReplaceInput(string Name, INodeInput NewInput)
        {
            for (int i = 0; i < Inputs.Count; ++i ) {
                if (Inputs[i].Name == Name) {
                    NodeInputInfo CurInfo = Inputs[i];
                    CurInfo.LastTimestamp = -1;
                    CurInfo.Input = NewInput;
                    Inputs[i] = CurInfo;
                    return true;
                }
            }
            return false;
        }


        public bool ReplaceInputAtIndex(int InputIndex, string NewName, INodeInput NewInput)
        {
            if (InputIndex < 0 && InputIndex >= Inputs.Count)
                return false;
            if (FindInput(NewName) != null)
                return false;

            NodeInputInfo CurInfo = Inputs[InputIndex];
            CurInfo.Name = NewName;
            CurInfo.LastTimestamp = -1;
            CurInfo.Input = NewInput;
            Inputs[InputIndex] = CurInfo;
            return true;
        }



		public bool RemoveInput(string Name)
		{
			for (int i = 0; i < Inputs.Count; ++i) {
				if (Inputs[i].Name == Name) {
                    Inputs.RemoveAt(i);
                    return true;
				}
			}
			return false;
		}



		public bool AddOutput(string Name, INodeOutput Output)
        {
            if (Outputs.Exists(x => x.Name == Name))
            {
                System.Diagnostics.Debugger.Break();
				GlobalGraphOutput.AppendLine("OUTPUT " + Name + "ALREADY EXISTS!", EGraphOutputType.GraphWarning);
                return false;
            }
            NodeOutputInfo OutputInfo = new();
            OutputInfo.Name = Name;
            OutputInfo.Output = Output;
            Outputs.Add(OutputInfo);
            return true;
        }

        public bool ReplaceOutput(string Name, INodeOutput NewOutput)
        {
            for (int i = 0; i < Inputs.Count; ++i) {
                if (Outputs[i].Name == Name) {
                    NodeOutputInfo CurInfo = Outputs[i];
                    CurInfo.Output = NewOutput;
                    Outputs[i] = CurInfo;
                    return true;
                }
            }
            return false;
        }


        public INodeInput? FindInput(string Name)
        {
            foreach (NodeInputInfo inputInfo in Inputs) {
                if (inputInfo.Name == Name)
                    return inputInfo.Input;
            }
            return null;
        }
        public INodeOutput? FindOutput(string Name)
        {
            foreach (NodeOutputInfo OutputInfo in Outputs)
            {
                if (OutputInfo.Name == Name)
                    return OutputInfo.Output;
            }
            return null;
        }


        //
        // INode Interface
        // 
        public string GetNodeName()     
        {
            //return GetDefaultNodeName();
            // returns default node name unless a custom name has been defined
            return GetCustomNodeName();
        }
        public IEnumerable<INodeInputInfo> EnumerateInputs()
        {
            foreach (NodeInputInfo InputInfo in Inputs)
                yield return new INodeInputInfo() { InputName = InputInfo.Name, Input = InputInfo.Input };
        }
        public IEnumerable<INodeOutputInfo> EnumerateOutputs()
        {
            foreach (NodeOutputInfo OutputInfo in Outputs)
                yield return new INodeOutputInfo() { OutputName = OutputInfo.Name, Output = OutputInfo.Output };
        }



        //
        // Evaluation
        //


        public virtual void CollectOutputRequirements(
            IEnumerable<string> Outputs,
            List<NodeInputRequirement> AccumRequirements)
        {
            // by default assume all inputs are necessary
            foreach ( NodeInputInfo info in Inputs )
            {
                AccumRequirements.Add(new() { InputName = info.Name });
            }
        }


        // RequestedDataOut must be initialized with desired members!
        public virtual void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            throw new Exception("Must implement node evaluation...");
        }

		public virtual void Evaluate(
			EvaluationContext EvalContext,
			ref readonly NamedDataMap DataIn,
			NamedDataMap RequestedDataOut)
        {
            this.Evaluate(in DataIn, RequestedDataOut);
        }

		//
		// dynamic node change notifications
		//
		public delegate void NodeModifiedEventDelegate(NodeBase node);
        public NodeModifiedEventDelegate? OnNodeModified;

        //! fire after significant change to node structure (add/remove pins, change pin type)
        public void PublishNodeModifiedNotification()
        {
            OnNodeModified?.Invoke(this);
        }


        //
        // serialization-related functions
        //
        public virtual void CollectCustomDataItems(out NodeCustomData? DataItems) { DataItems = null; }
        public virtual void RestoreCustomDataItems(NodeCustomData DataItems) { }




		public override string ToString()
		{
            return $"{GetDefaultNodeName()} ({GetType().ToString()})";
		}

    }




    public abstract class StandardNode : NodeBase
    {
        public override string GetDefaultNodeName()
        {
            return GetType().Name;
        }
    }
}
