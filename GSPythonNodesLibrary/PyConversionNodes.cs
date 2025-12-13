using GSPython;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.PythonNodes
{
    public abstract class PyConversionNode : NodeBase
    {
        public PythonType FromPyType = new PythonType();
        public Type ToCSharpType = typeof(object);

        public string InputName = "Input";
        public string OutputName = "Output";

        public PyConversionNode(string inputName, PythonType inputType, string outputName, Type csharpType)
        {
            InputName = inputName; FromPyType = inputType;
            OutputName = outputName; ToCSharpType = csharpType;
            AddInput(InputName, new PythonNodeInputBase(FromPyType));
            AddOutput(OutputName, new StandardNodeOutputBase(ToCSharpType));
        }
    }


    public abstract class PyListToArrayNode<T> : PyConversionNode
    {
        public override string GetDefaultNodeName() { return "Convert"; }

        public PyListToArrayNode(PythonType pyType) : base("PyList", pyType, "Array", typeof(T[])) { }

        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            object? data = DataIn.FindItemValue(InputName);
            object array = GSPythonConversionsLibrary.ConvertPyListToArray<T>(data!);
            RequestedDataOut.SetItemValueChecked(OutputName, array);
        }
    }
    [GraphNodeNamespace("Python")] [GraphNodeUIName("List[string] to string[]")]
    public class PyStringListToArrayNode : PyListToArrayNode<string> {
        public PyStringListToArrayNode() : base(PythonType.ListStr) { }
    }
    [GraphNodeNamespace("Python")] [GraphNodeUIName("List[int] to int[]")]
    public class PyIntListToArrayNode : PyListToArrayNode<int> {
        public PyIntListToArrayNode() : base(PythonType.ListInt) { }
    }
    [GraphNodeNamespace("Python")] [GraphNodeUIName("List[float] to double[]")]
    public class PyFloatListToArrayNode : PyListToArrayNode<double> {
        public PyFloatListToArrayNode() : base(PythonType.ListFloat) { }
    }
    [GraphNodeNamespace("Python")] [GraphNodeUIName("List[bool] to bool[]")]
    public class PyBoolListToArrayNode : PyListToArrayNode<bool> {
        public PyBoolListToArrayNode() : base(PythonType.ListBool) { }
    }




    public abstract class PyListToCSharpListNode<T> : PyConversionNode
    {
        public override string GetDefaultNodeName() { return "Convert"; }

        public PyListToCSharpListNode(PythonType pyType) : base("PyList", pyType, "List", typeof(List<T>)) { }

        public override void Evaluate(ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            object? data = DataIn.FindItemValue(InputName);
            object array = GSPythonConversionsLibrary.ConvertPyListToList<T>(data!);
            RequestedDataOut.SetItemValueChecked(OutputName, array);
        }
    }
    [GraphNodeNamespace("Python")] [GraphNodeUIName("List[string] to List<string>")]
    public class PyStringListToListNode : PyListToCSharpListNode<string> {
        public PyStringListToListNode() : base(PythonType.ListStr) { }
    }
    [GraphNodeNamespace("Python")] [GraphNodeUIName("List[int] to List<int>")]
    public class PyIntListToListNode : PyListToCSharpListNode<int> {
        public PyIntListToListNode() : base(PythonType.ListInt) { }
    }
    [GraphNodeNamespace("Python")] [GraphNodeUIName("List[float] to List<double>")]
    public class PyFloatListToListNode : PyListToCSharpListNode<double> {
        public PyFloatListToListNode() : base(PythonType.ListFloat) { }
    }
    [GraphNodeNamespace("Python")] [GraphNodeUIName("List[bool] to List<bool>")]
    public class PyBoolListToListNode : PyListToCSharpListNode<bool> {
        public PyBoolListToListNode() : base(PythonType.ListBool) { }
    }

}
