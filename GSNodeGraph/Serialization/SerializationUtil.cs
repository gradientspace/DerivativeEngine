using System;
using System.Diagnostics;

namespace Gradientspace.NodeGraph
{
	public static class SerializationUtil
	{
		//! represents a stored constant value on an input pin
		public struct InputConstant
		{
			public string InputName { get; set; }
			public string DataType { get; set; }
			public object Value { get; set; }
		}

		/**
		 * Save input constants on a Node
		 **/
		public static void SaveInputConstants(INode Node, out List<InputConstant>? InputConstants)
		{
			InputConstants = null;

			foreach (INodeInputInfo inputInfo in Node.EnumerateInputs())
			{
				if (TryGetInputConstant(Node, inputInfo, out InputConstant constant))
				{
					if (InputConstants == null)
						InputConstants = new();
					InputConstants.Add(constant);
				}
			}
		}

		public static bool TryGetInputConstant(INode node, INodeInputInfo inputInfo, out InputConstant constant)
		{
			constant = new InputConstant();

			INodeInput input = inputInfo.Input;
			(object? constantValue, bool bIsDefined) = input.GetConstantValue();
			if (bIsDefined == false || constantValue == null)
				return false;

			GraphDataType inputDataType = input.GetDataType();
			Type inputType = inputDataType.DataType;

			constant.InputName = inputInfo.InputName;
			constant.DataType = inputType.FullName ?? inputType.Name;

			if (constantValue.GetType().IsSubclassOf(typeof(Type)))
				constant.Value = (constantValue as Type)!.AssemblyQualifiedName!;
			else if (inputType.IsEnum)
				constant.Value = constantValue.ToString()!;
			else
				constant.Value = constantValue;

			return true;
		}

		/**
		 * Restore input constants on a Node
		 **/
		public static void RestoreInputConstants(INode Node, List<InputConstant>? InputConstants)
		{
			if (InputConstants == null)
				return;
			foreach (INodeInputInfo inputInfo in Node.EnumerateInputs())
			{
				int index = InputConstants.FindIndex((val) => { return String.Compare(val.InputName, inputInfo.InputName, true) == 0; });
				if (index >= 0)
					RestoreInputConstant(Node, inputInfo, InputConstants[index]);
			}
		}


		public static void RestoreInputConstant(INode Node, INodeInputInfo InputInfo, InputConstant Constant)
		{
			INodeInput input = InputInfo.Input;
			GraphDataType dataType = input.GetDataType();
			Type inputType = dataType.DataType;

			string useTypeName = inputType.FullName ?? inputType.Name;
			(object? constantValue, bool bIsDefined) = input.GetConstantValue();
			if (bIsDefined != false && constantValue != null && useTypeName == Constant.DataType)
			{
				string? stringConstant = Constant.Value.ToString();
				Debug.Assert(stringConstant != null);

				if (useTypeName == "System.Type")
				{
					Type? FoundType = TypeUtils.FindTypeInLoadedAssemblies(stringConstant);
					if (FoundType != null)
						input.SetConstantValue(FoundType);
					return;
				}

				if (useTypeName.StartsWith("System.") == false)
				{
					// if it's not a system type, we need to find it. First check for enums...
					Type? EnumType = TypeUtils.FindEnumTypeFromFullName(useTypeName);
					if (EnumType != null && TypeUtils.GetEnumInfo(EnumType, out var EnumInfo))
					{
						object? EnumValue = EnumInfo.FindEnumValueFromString(stringConstant);
						if (EnumValue != null)
							input.SetConstantValue(EnumValue);

						// not using by-integer...
						//if (int.TryParse(stringConstant, out int EnumID))
						//{
						//    object? EnumValue = EnumInfo.FindEnumValueFromID(EnumID);
						//    if (EnumValue != null)
						//        input.SetConstantValue(EnumValue);
						//}
					}

					return;
				}

				// for int types can we cast?
				// for float/double do we need to consider writing precision?
				if (useTypeName == typeof(float).FullName)
				{
					if (float.TryParse(stringConstant, out float f))
						input.SetConstantValue(f);
				} 
				else if (useTypeName == typeof(double).FullName)
				{
					if (double.TryParse(stringConstant, out double f))
						input.SetConstantValue(f);
				} 
				else if (useTypeName == typeof(int).FullName)
				{
					if (int.TryParse(stringConstant, out int f))
						input.SetConstantValue(f);
				} 
				else if (useTypeName == typeof(short).FullName)
				{
					if (short.TryParse(stringConstant, out short f))
						input.SetConstantValue(f);
				} 
				else if (useTypeName == typeof(long).FullName)
				{
					if (long.TryParse(stringConstant, out long f))
						input.SetConstantValue(f);
				} 
				else if (useTypeName == typeof(bool).FullName)
				{
					if (bool.TryParse(stringConstant, out bool bValue))
						input.SetConstantValue(bValue);
				} 
				else if (useTypeName == typeof(string).FullName)
				{
					input.SetConstantValue(stringConstant);
				}
			}

		}

	}
}
