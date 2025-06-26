
using System.Collections;

namespace Gradientspace.NodeGraph
{
    /**
     * Placeholder nodes are special nodes that can exist in the graph but cannot be evaluated.
     * They are meant to be replaced by the Graph Editor when data connections are made that allow
     * the editor to determine a more specific node type. 
     * 
     * For example a Placeholder Add node can have two inputs that could accept any addable type.
     * When one of the pins is connected, the Placeholder node is replaced with an Add node for
     * the suitable type (eg a float/float, vector/vector, etc). This has some benefits over having
     * some sort of "uber-Add" node.
     */
    public abstract class PlaceholderNodeBase : NodeBase
    {
        //! Subclasses must implement this API to communicate back to calling code (eg a graph editor)
        //! which concrete Node type should be used to replace the placeholder upon connection.
        public abstract bool GetPlaceholderReplacementNodeInfo(
            string PlaceholderInputName, in GraphDataType incomingType,
            out Type ReplacementNodeClassType, out string ReplacementNodeInputName,
            out Action<INode, GraphDataType>? ReplacementNodeInitializer);


        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            throw new NotImplementedException("Placeholder nodes cannot be evaluated");
        }

    }

    /**
     * A Placeholder input is meant to be used by Placeholder nodes. This
     * type of input (derived from DynamicNodeInput) returns a GraphDataType
     * with a customizable filter, which can be used to specify what kind of
     * outputs can be connected to the Placeholder input (which will then be
     * swapped for a concrete node/input)
     */
    public class PlaceholderNodeInput : DynamicNodeInput
    {
        public PlaceholderNodeInput(Func<Type, bool> TypeFilterFunc) : base(TypeFilterFunc)
        {
        }
    }


}
