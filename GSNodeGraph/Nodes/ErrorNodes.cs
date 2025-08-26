// Copyright Gradientspace Corp. All Rights Reserved.

namespace Gradientspace.NodeGraph
{

    // MissingNodeErrorNode is inserted by Serializer when
    // a node type cannot be found
    [SystemNode]
    public class MissingNodeErrorNode : NodeBase
    {
        public string NodeName = "MissingNode";

        public string NodeClassType = "";
        public string NodeClassVariant = "";

        public override string GetDefaultNodeName()
        {
            return "MissingNode";
        }

        public override string GetCustomNodeName()
        {
            return NodeName;
        }

        // missing node should never be evaluated
        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            throw new NotImplementedException();
        }

    }
}
