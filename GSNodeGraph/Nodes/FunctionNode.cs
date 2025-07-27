using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Gradientspace.NodeGraph.FunctionDefinitionNode;

namespace Gradientspace.NodeGraph
{
    [SystemNode]
    public class FunctionDefinitionNode : NodeBase
    {
        public override string GetDefaultNodeName() {
            return "Function";
        }
        public override string GetCustomNodeName() {
            return functionName;
        }

        public FunctionDefinitionNode() : base()
        {
            FunctionID = Guid.NewGuid().ToString();
        }


        // unique ID that can be used to store links to function in other graph nodes (eg return nodes)
        public string FunctionID { get; private set; }

        public string FunctionName { get { return functionName; } }

        public struct FunctionArg
        {
            public string ArgName;
            public Type ArgType;
            public object? DefaultValue;
        }
        public IEnumerable<FunctionArg> Arguments { get { return functionArgs; } }
        public IEnumerable<FunctionArg> ReturnArguments { get { return returnArgs; } }


        protected string functionName = "NewFunction";
        protected List<FunctionArg> functionArgs = new List<FunctionArg>();
        protected List<FunctionArg> returnArgs = new List<FunctionArg>();

        public void UpdateFunctionName(string NewFunctionName)
        {
            functionName = NewFunctionName;
            PublishNodeModifiedNotification();
        }

        public void UpdateArguments( IEnumerable<FunctionArg> NewArguments )
        {
            List<FunctionArg> newArguments = new List<FunctionArg>();

            Inputs.Clear();
            Outputs.Clear();
            foreach (FunctionArg arg in NewArguments) 
            {
                newArguments.Add(arg);

                INodeInput argInput = FunctionNodeUtils.BuildInputNodeForType(arg.ArgType, arg.DefaultValue);
                if (argInput is StandardNodeInputBase argInputBase)
                    argInputBase.Flags = ENodeInputFlags.IsNodeConstant;
                bool bInputOK = AddInput(arg.ArgName, argInput);
                if (!bInputOK)
                    GlobalGraphOutput.AppendError($"FunctionDefinitionNode.UpdateArguments: input pin could not be created for {arg.ArgName} with Type {arg.ArgType}");

                INodeOutput argOutput = new StandardNodeOutputBase(arg.ArgType);
                bool bOutputOK = AddOutput(arg.ArgName, argOutput);
                if (!bOutputOK)
                    GlobalGraphOutput.AppendError($"FunctionDefinitionNode.UpdateArguments: output pin could not be created for {arg.ArgName} with Type {arg.ArgType}");
            }

            functionArgs = newArguments;

            PublishNodeModifiedNotification();
        }


        public void UpdateReturnArguments(IEnumerable<FunctionArg> NewReturnArguments)
        {
            returnArgs = NewReturnArguments.ToList();
        }



        // todo save/restore FunctionID / functionName / functionArgs / returnArgs / from custom data



        // does function node ever actually get evaluated??
        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            throw new NotImplementedException();
        }

    }



    [SystemNode]
    public class FunctionReturnNode : NodeBase
    {
        public override string GetDefaultNodeName() {
            return "Return";
        }

        public virtual void LinkToFunction(FunctionDefinitionNode functionNode) {
            this.FunctionID = functionNode.FunctionID;
            UpdateArguments(functionNode.ReturnArguments);
        }

        public string FunctionID { get; private set; } = "";


        protected List<FunctionDefinitionNode.FunctionArg> returnArgs = new List<FunctionDefinitionNode.FunctionArg>();

        public IEnumerable<FunctionDefinitionNode.FunctionArg> Arguments { get { return returnArgs; } }


        public void UpdateArguments(IEnumerable<FunctionArg> NewArguments)
        {
            List<FunctionArg> newArguments = new List<FunctionArg>();

            Inputs.Clear();
            Outputs.Clear();
            foreach (FunctionArg arg in NewArguments) {
                newArguments.Add(arg);

                INodeInput argInput = FunctionNodeUtils.BuildInputNodeForType(arg.ArgType, arg.DefaultValue);
                bool bInputOK = AddInput(arg.ArgName, argInput);
                if (!bInputOK)
                    GlobalGraphOutput.AppendError($"FunctionReturnNode.UpdateArguments: input pin could not be created for {arg.ArgName} with Type {arg.ArgType}");
            }

            returnArgs = newArguments;

            PublishNodeModifiedNotification();
        }



        // does return node ever actually get evaluated??
        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            throw new NotImplementedException();
        }
    }


}
