// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

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
			Type inputType = inputDataType.CSType;

			constant.InputName = inputInfo.InputName;
			constant.DataType = inputType.FullName ?? inputType.Name;
			if (constant.DataType.StartsWith("System.") == false )
				constant.DataType = TypeUtils.MakePartialQualifiedTypeName(inputType);

			if (constantValue.GetType().IsSubclassOf(typeof(Type)))
				constant.Value = TypeUtils.MakePartialQualifiedTypeName( (constantValue as Type)! );
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

			// There is some complexity / hidden behavior here because restoring an input constant
			// one input may result in other inputs changing. For example this can happen on a CreateVariable
			// node where we have a Type input and an InitialValue input, the type of the latter is defined by
			// the former. So when the Type constant is restored, the IntialValue input is recreated/replaced.
			// Currently this is handled by accident, as the Type InputConstant is restored first.
			// Need some way to explicitly specify these dependecies...

			// (another way to handle this might be via node custom data that is restored first...)

			List<INodeInputInfo> allInputs = new(Node.EnumerateInputs());
			for ( int i = 0; i < InputConstants.Count; ++i )
			{
				InputConstant inputConstant = InputConstants[i];
				int inputIndex = allInputs.FindIndex( (val) => { return String.Compare(val.InputName, inputConstant.InputName, true) == 0; });
				if (inputIndex >= 0 ) {
					if ( RestoreInputConstant(Node, allInputs[inputIndex], inputConstant) ) 
					{
						// perhaps the input or node could help us here...
						bool bPossibleTypeChanges = (allInputs[inputIndex].DataType.CSType == typeof(Type));
						if (bPossibleTypeChanges)
							allInputs = new(Node.EnumerateInputs());
					}
				}

			}

		}


		public static bool RestoreInputConstant(INode Node, INodeInputInfo InputInfo, InputConstant Constant)
		{
			INodeInput input = InputInfo.Input;
			GraphDataType dataType = input.GetDataType();
			Type inputType = dataType.CSType;

			string useTypeName = inputType.FullName ?? inputType.Name;
			(object? constantValue, bool bIsDefined) = input.GetConstantValue();
			if (bIsDefined == false || constantValue == null) 
				return false;

            // Constant.DataType may include library name, eg "g3.Vector3d, geometry3Sharp",
            // while useTypeName will not have that (currently). Maybe can construct?
			if (Constant.DataType.StartsWith(useTypeName) == false)
				return false;

			string? stringConstant = Constant.Value.ToString();
			Debug.Assert(stringConstant != null);

			if (useTypeName == "System.Type")
			{
				Type? FoundType = TypeUtils.FindTypeInLoadedAssemblies(stringConstant);
				if (FoundType != null) {
					input.SetConstantValue(FoundType);
					return true;
				}
				return false;
			}

			if (useTypeName.StartsWith("System.") == false)
			{
				// if it's not a system type, we need to find it. First check for enums...
				Type? EnumType = TypeUtils.FindEnumTypeFromFullName(useTypeName);
				if (EnumType != null && TypeUtils.GetEnumInfo(EnumType, out var EnumInfo))
				{
					object? EnumValue = EnumInfo.FindEnumValueFromString(stringConstant);
					if (EnumValue != null) {
						input.SetConstantValue(EnumValue);
						return true;
					}
                    // not using by-integer...
                    //if (int.TryParse(stringConstant, out int EnumID))
                    //{
                    //    object? EnumValue = EnumInfo.FindEnumValueFromID(EnumID);
                    //    if (EnumValue != null)
                    //        input.SetConstantValue(EnumValue);
                    //}
                }

                // try json deserialization
                JsonSerializerOptions jsonOptions = new JsonSerializerOptions();

                // look for [JsonConverter] attribute on the type. If it is found, 
                // create an instance of this Converter as it is probably necessary for Deserialize() to work
                JsonConverterAttribute? ConverterAttrib = inputType.GetCustomAttribute<JsonConverterAttribute>();
                if (ConverterAttrib != null && ConverterAttrib.ConverterType != null) {
                    JsonConverter? Converter = Activator.CreateInstance(ConverterAttrib.ConverterType) as JsonConverter;
                    if (Converter != null)
                        jsonOptions.Converters.Add(Converter);
                }

                try {
                    object? result = JsonSerializer.Deserialize((JsonElement)Constant.Value, inputType, jsonOptions);
                    if (result != null) {
                        input.SetConstantValue(result);
                        return true;
                    }
                } catch { }


                GlobalGraphOutput.AppendError($"[SerialiationUtil.RestoreInputConstant] - cannot restore constant of type {inputType}");
                return false;
			}

			// for int types can we cast?
			// for float/double do we need to consider writing precision?
			if (useTypeName == typeof(float).FullName)
			{
				if (float.TryParse(stringConstant, out float f)) { 
					input.SetConstantValue(f);
					return true;
				}
			} 
			else if (useTypeName == typeof(double).FullName)
			{
				if (double.TryParse(stringConstant, out double f)) { 
					input.SetConstantValue(f);
					return true;
				}
			} 
			else if (useTypeName == typeof(int).FullName)
			{
				if (int.TryParse(stringConstant, out int f)) { 
					input.SetConstantValue(f);
					return true;
				}
			} 
			else if (useTypeName == typeof(short).FullName)
			{
				if (short.TryParse(stringConstant, out short f)) { 
					input.SetConstantValue(f);
					return true;
				}
			} 
			else if (useTypeName == typeof(long).FullName)
			{
				if (long.TryParse(stringConstant, out long f)) { 
					input.SetConstantValue(f);
					return true;
				}
			} 
			else if (useTypeName == typeof(bool).FullName)
			{
				if (bool.TryParse(stringConstant, out bool bValue)) { 
					input.SetConstantValue(bValue);
					return true;
				}
			} 
			else if (useTypeName == typeof(string).FullName)
			{
				input.SetConstantValue(stringConstant);
				return true;
			}

			return false;
		}

	}
}
