using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GSPython;
using Python.Runtime;

namespace Gradientspace.NodeGraph.PythonNodes
{
    [GraphNodeNamespace("Gradientspace.Python")]
    public class CreatePythonTupleNode : StandardNode, INode_VariableInputs, INode_DynamicOutputs
	{
        public static string ElementBaseName { get { return "Input"; } }
        public static string TupleOutputName { get { return "Tuple"; } }

        public int NumInputs { get; set; } = 2;

        public CreatePythonTupleNode()
        {
			NumInputs = 2;
			updateInputsAndOutputs();
        }

        private string MakeInputName(int Index) { return $"{ElementBaseName}{Index}"; }

        public bool AddInput()
        {
			add_input();
			PublishNodeModifiedNotification();
            return true;
        }
		protected void add_input()
		{
			Type inputType = typeof(object);
			AddInput(MakeInputName(NumInputs), new StandardNodeInputBase(inputType));
			NumInputs = NumInputs + 1;
		}

		public bool RemoveInput(int SpecifiedIndex = -1)
        {
            if (NumInputs <= 2) return false;

            Inputs.RemoveAt(NumInputs - 1);
            NumInputs = Inputs.Count;
            PublishNodeModifiedNotification();
            return true;
        }


		protected virtual void updateInputsAndOutputs()
		{
			// rebuild inputs
			Inputs.Clear();
			int want_num_inputs = NumInputs;
			NumInputs = 0;
			while (NumInputs < want_num_inputs)
				add_input();

			update_output();
		}

		protected virtual void update_output()
		{
			// todo this depends on what is wired up!!
			// UpdateDynamicOutputs() can figure this out if it's called...but 
			// it is not called on load currently...

			Outputs.Clear();
			Type csharpType = typeof(object);
			PythonType tupleType = new PythonType(EPythonTypes.Tuple);
			PythonNodeOutputBase output = new PythonNodeOutputBase(csharpType, tupleType);
				
			AddOutput(TupleOutputName, output);
		}


		public virtual void UpdateDynamicOutputs(INodeGraph graph)
		{
			// determine types on input pins - requires querying the graph
			PythonType[] nestedTypes = new PythonType[Inputs.Count];
			for (int i = 0; i < Inputs.Count; ++i)
			{
				IConnectionInfo foundConnection = graph.FindConnectionTo(this.GraphIdentifier, Inputs[i].Name, EConnectionType.Data);
				if (foundConnection.IsValid == false) {
					nestedTypes[i] = new PythonType();
				} else {
					if (graph.GetNodeOutputType(foundConnection.FromNodeIdentifier, foundConnection.FromNodeOutputName, out GraphDataType dataType))
					{
						if (dataType.DataFormat == EGraphDataFormat.Python && dataType.ExtendedType != null)
							nestedTypes[i] = (PythonType)(dataType.ExtendedType);
						else
							nestedTypes[i] = PythonTypeUtil.CSharpTypeToPythonType(dataType.DataType);
					}
				}
			}

			// todo check for modified type, to avoid spurious modifications?

			// rebuild the output pin with the new typed tuple
			Outputs.Clear();
			Type csharpType = typeof(object);
			PythonType tupleType = new PythonType(EPythonTypes.Tuple, nestedTypes, false);
			PythonNodeOutputBase output = new PythonNodeOutputBase(csharpType, tupleType);
			AddOutput(TupleOutputName, output);

			// publish node change, this will rebuild it at the UI level
			PublishNodeModifiedNotification();
		}


		public override void Evaluate(
            ref readonly NamedDataMap DataIn,
            NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(TupleOutputName);
            if (OutputIndex == -1)
                throw new Exception("CreatePythonTupleNode: output not found");

			PyTuple? tuple = null;
			using (Py.GIL())
			{
				PyObject[] items = new PyObject[NumInputs];
				for (int i = 0; i < NumInputs; ++i) {
					object? itemValue = DataIn.FindItemValue(MakeInputName(i));
					items[i] = itemValue.ToPython();
				}
				tuple = new PyTuple(items);
			}

			RequestedDataOut.SetItemValue(OutputIndex, tuple);
        }






		public const string NumInputsKey = "NumInputs";
		public override void CollectCustomDataItems(out List<Tuple<string, object>>? DataItems)
		{
			DataItems = new List<Tuple<string, object>>();
			DataItems.Add(new(NumInputsKey, NumInputs));
		}
		public override void RestoreCustomDataItems(List<Tuple<string, object>> DataItems)
		{
			Tuple<string, object>? NumInputsFound = DataItems.Find((x) => { return x.Item1 == NumInputsKey; });
			if (NumInputsFound != null)
			{
				NumInputs = ((JsonElement)NumInputsFound.Item2).GetInt32();
				updateInputsAndOutputs();
			}
		}

	}

}
