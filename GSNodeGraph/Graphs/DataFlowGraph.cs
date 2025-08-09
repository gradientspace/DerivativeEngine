// Copyright Gradientspace Corp. All Rights Reserved.

namespace Gradientspace.NodeGraph
{
    public class DataFlowGraph : BaseGraph
    {
        public DataFlowGraph() : base()
        {
        }


        public object? ComputeNodeOutputData(INodeInfo nodeInfo, string OutputName)
        {
            return ComputeNodeOutputData(new NodeHandle(nodeInfo.Identifier), OutputName);
        }
        public object? ComputeNodeOutputData(NodeHandle TargetNodeHandle, string OutputName)
        {
            NodeBase? FoundNode = FindNodeFromHandle(TargetNodeHandle);
            if (FoundNode == null) return null;

            INodeOutput? FoundOutput = null;
            GraphDataType OutputDataType = GraphDataType.Default;
            List<string> Outputs = new List<string>();
            if ( OutputName.Length == 0 )
            {
                if (FoundNode.GetType().IsSubclassOf(typeof(SinkNode)) == false)
                    throw new Exception("ComputeNodeOutputData: no valid output name provided but Node is not a SinkNode");
            }
            else
            {
                FoundOutput = FoundNode.FindOutput(OutputName);
                if (FoundOutput == null) return null;
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
            for ( int i = 0; i < Outputs.Count; ++i )
                OutputDatas.SetItem(i, Outputs[i], OutputDataType.CSType, null);

            FoundNode.Evaluate(ref InputDatas, OutputDatas);

            object? Result = OutputDatas.FindItemValue(OutputName);
            return Result;
        }
        protected void FetchInputDatas(NodeBase Node, NodeHandle nodeHandle, List<NodeInputRequirement> RequiredInputs, NamedDataMap InputDatas)
        {
            int NumInputs = RequiredInputs.Count;
            for (int k = 0; k < NumInputs; ++k)
            {
                // find the connection to this input
                string InputName = RequiredInputs[k].InputName;
                Connection FoundConnection = FindDataConnectionForInput(nodeHandle, InputName);
                if (FoundConnection != Connection.Invalid)
                {
                    // recursively evaluate the output on the other side of that connection
                    object? InputData = ComputeNodeOutputData(FoundConnection.FromNode, FoundConnection.FromOutput);
                    if (InputData == null)
                        throw new Exception("ComputeNodeOutputData: [" + Node.GetNodeName() + ":" + InputName + "] - failed to compute incoming data");
                    InputDatas.SetItem(k, InputName, InputData);
                }
                else
                {
                    // try getting default value
                    (object? InputData, bool bIsDefined) = GetConstantValueForInput(nodeHandle, InputName);
                    if (bIsDefined == false)
                        throw new Exception("ComputeNodeOutputData: [" + Node.GetNodeName() + ":" + InputName + "] - no incoming connection and default value is undefined");
                    if (InputData == null)
                        InputDatas.SetItemNull(k, InputName);
                    else
                        InputDatas.SetItem(k, InputName, InputData);
                }

            }
        }



    }
}
