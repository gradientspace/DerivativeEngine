using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GSPython
{
	/**
	 * standard python types enumeration
	 */
	public enum EPythonTypes
	{
		Str = 0,
		Int = 1,
		Float = 2,
		Complex = 3,
		Bool = 4,

		Tuple = 10,

		List = 20,
		Dict = 21,
		Set = 22,

		// Range
		// FrozenSet
		// Bytes
		// ByteArray
		// MemoryView

		//! python object/class types, eg numpy.dtype, etc
		DefinedClass = 99,

		// ... what is the difference between these?
		None = 100,
		Undefined = 111
	}


	public static class PythonTypeUtil
	{
		public static bool IsStandardCSharpType(EPythonTypes type)
		{
			switch (type)
			{
				case EPythonTypes.Bool:
				case EPythonTypes.Int:
				case EPythonTypes.Float:
				case EPythonTypes.Str:
					return true;
				default:
					return false;
			}
		}

		public static Type? GetStandardCSharpType(EPythonTypes pyType)
		{
			switch (pyType)
			{
				case EPythonTypes.Bool:		return typeof(bool);
				case EPythonTypes.Int:		return typeof(int);
				case EPythonTypes.Float:	return typeof(double);
				case EPythonTypes.Str:		return typeof(string);
				default:					return null;
			}
		}

		public static string TypeToString(EPythonTypes pyType)
		{
			switch (pyType) { 
				case EPythonTypes.Bool:		return "bool";
				case EPythonTypes.Int:		return "int";
				case EPythonTypes.Float:	return "float";
				case EPythonTypes.Complex:	return "complex";
				case EPythonTypes.Str:		return "str";
				case EPythonTypes.List:		return "list";
				case EPythonTypes.Set:		return "set";
				case EPythonTypes.Dict:		return "dict";
				default:					return "";
			}
		}

		public static PythonType CSharpTypeToPythonType(in Type csharpType)
		{
			if (csharpType == typeof(int) || csharpType == typeof(long))
				return new PythonType(EPythonTypes.Int);
			else if (csharpType == typeof(float) || csharpType == typeof(double))
				return new PythonType(EPythonTypes.Float);
			else if (csharpType == typeof(bool))
				return new PythonType(EPythonTypes.Bool);
			else if (csharpType == typeof(string))
				return new PythonType(EPythonTypes.Str);
			else
				return new PythonType();
		}

	}


	/**
	 *  PythonType represents a type of a symbol/variable/etc in Python. 
	 *  This is similar to PythonSymbol but only includes the type information.
	 *  
	 *  Possibly this structure is over-complicated...the NestedTypes array is stored but
	 *  the Identifier string will always contain the same information. So the question
	 *  is if we ever actually need the NestedTypes array in any usage of PythonType - as 
	 *  compared to PythonSymbol.NestedSymbols which is necessary to build node pins, construct 
	 *  dynamic function calls, and so on...
	 */
	[JsonConverter(typeof(PythonTypeJsonConverter))]
	public struct PythonType : IEquatable<PythonType>
	{
		//! standard python data type (int, str, list, tuple, class, etc)
		public EPythonTypes DataType { get; init; } = EPythonTypes.None;

		//! string identifier for the type
		//! for classes this will be the full name, eg numpy.dtype
		//! for composite types it will be the entire type string
		public string Identifier { get; init; } = "";

		//! Optional nested types. If type is a List[type], Dict[a,b], there may be 1 or 2 NestedTypes defining the type(s)
		//! If type is a Tuple, there will be N nested types
		public PythonType[]? NestedTypes { get; init; } = null;
		// make custom struct type that has 2 members and then a list for extended tuples?
		// (do we even need to keep this list? can just use string?)

		public PythonType()
		{
			DataType = EPythonTypes.None;
			Identifier = PythonTypeUtil.TypeToString(DataType);
			NestedTypes = null;
		}

		// construct a basic python type that has no nested types
		public PythonType(EPythonTypes type)
		{
			DataType = type;
			Identifier = PythonTypeUtil.TypeToString(DataType);
			NestedTypes = null;
		}

		//! construct a type with an explicit identifier, only valid for class types
		public PythonType(EPythonTypes type, string identifier)
		{
			Debug.Assert(type == EPythonTypes.DefinedClass);    // should not be using for other cases?
			DataType = type;
			Identifier = identifier;
			NestedTypes = null;
		}

		//! construct a type with one nested type, eg like a list[type] or set[type]
		public PythonType(EPythonTypes type, EPythonTypes nestedType)
		{
			Debug.Assert(type == EPythonTypes.List || type == EPythonTypes.Set);    // should not be using for other cases?
			DataType = type;
			NestedTypes = new PythonType[1] { new PythonType(nestedType) };
			Identifier = GetTypeString();
		}

		//! construct a type with a list of nested types
		public PythonType(EPythonTypes type, string identifier, PythonType[]? nestedTypes, bool bCopyArray = true)
		{
			DataType = type;
			Identifier = identifier;
			if (bCopyArray && nestedTypes != null) {
				NestedTypes = new PythonType[nestedTypes.Length];
				Array.Copy(nestedTypes, NestedTypes, nestedTypes.Length);
			} else
				NestedTypes = nestedTypes;
		}


		//! construct a type with a list of nested types
		public PythonType(EPythonTypes type, PythonType[]? nestedTypes, bool bCopyArray = true)
		{
			DataType = type;
			if (bCopyArray && nestedTypes != null) {
				NestedTypes = new PythonType[nestedTypes.Length];
				Array.Copy(nestedTypes, NestedTypes, nestedTypes.Length);
			} else
				NestedTypes = nestedTypes;
			Identifier = GetTypeString();
		}


		//! convert PythonSymbol to PythonType
		public PythonType(PythonSymbol symbol)
		{
			DataType = symbol.DataType;
			if (symbol.NestedSymbols != null)
			{
				int N = symbol.NestedSymbols.Length;
				NestedTypes = new PythonType[N];
				for (int i = 0; i < N; ++i)
					NestedTypes[i] = new PythonType(symbol.NestedSymbols[i]);
				Identifier = this.GetTypeString();
			} else
				Identifier = symbol.GetPyTypeString();
		}

		//! return true if this is a type that has nested sub-types, eg a tuple, list[int], etc
		public readonly bool IsCompositeType { get { return NestedTypes != null; } }


		// constants for various standard types (generally that convert to C# types)
		public static PythonType ListInt = new PythonType(EPythonTypes.List, EPythonTypes.Int);
		public static PythonType ListStr = new PythonType(EPythonTypes.List, EPythonTypes.Str);


		//! return true if this type is the same as the other type
		public readonly bool IsSameType(in PythonType other)
		{
			if (DataType != other.DataType)
				return false;
			int N1 = (NestedTypes == null) ? 0 : NestedTypes.Length;
			int N2 = (other.NestedTypes == null) ? 0 : other.NestedTypes.Length;

			if (N1 == 0 && N1 == N2) {		// no nested symbols, only check identifier
				return Identifier == other.Identifier;
			}
			if (N1 != N2)
				return false;
			for (int i = 0; i < N1; ++i) {
				PythonType a = NestedTypes![i];
				PythonType b = other.NestedTypes![i];
				if (a.IsSameType(b) == false)
					return false;
			}

			return true;
		}

		//! return true if this type is compatible with the other type
		//! This compatibility is directional, ie tuple[int,int,string] is compatible with tuple[int,int] but not the other way around
		public readonly bool IsCompatibleType(in PythonType second)
		{
			// typed connections to an untyped connection are always allowed
			if (second.DataType == EPythonTypes.Undefined)
				return true;
			// otherwise must have same datatype
			if (DataType != second.DataType)
				return false;
			if (DataType == EPythonTypes.Tuple)
			{
				return IsCompatibleTuple(second, false);
			}
			return IsSameType(second);
		}

		//! return true if this tupe is a tuple and is compatible with the other type
		//! tuple[int,int,string] is compatible with tuple[int,int] but not the other way around
		//! tuple[anything] is compatible with untyped tuple, and vice-versa
		public readonly bool IsCompatibleTuple(in PythonType second, bool bCheckExactMatch)
		{
			Debug.Assert(DataType == EPythonTypes.Tuple && second.DataType == EPythonTypes.Tuple);
			if (NestedTypes == null || second.NestedTypes == null)
				return true;
			int N = NestedTypes.Length;
			int N2 = second.NestedTypes.Length;
			if (N2 > N)
				return false;
			for (int i = 0; i < N2; ++i) {
				PythonType a = NestedTypes![i];
				PythonType b = second.NestedTypes![i];
				if (bCheckExactMatch) {
					if (a.IsSameType(b) == false)
						return false;
				} else {
					if (a.IsCompatibleType(b) == false)
						return false;
				}
			}
			return true;
		}





		//! convert to string
		public readonly string GetTypeString()
		{
			string typesymbol = "";
			switch (DataType)
			{
				case EPythonTypes.Str: typesymbol = "str"; break;
				case EPythonTypes.Int: typesymbol = "int"; break;
				case EPythonTypes.Float: typesymbol = "float"; break;
				case EPythonTypes.Complex: typesymbol = "complex"; break;
				case EPythonTypes.Bool: typesymbol = "bool"; break;

				case EPythonTypes.Tuple:
					if (NestedTypes != null && NestedTypes.Length > 0)
					{
						string accum = NestedTypes[0].GetTypeString()!;
						for (int j = 1; j < NestedTypes.Length; ++j)
							accum += ", " + NestedTypes[j].GetTypeString();
						typesymbol = $"({accum})";
					} else
						typesymbol = "(tuple)";
					break;

				case EPythonTypes.List:
					if (NestedTypes != null && NestedTypes.Length == 1)
					{
						string subType = NestedTypes[0].GetTypeString();
						typesymbol = $"List[{subType}]";
					} else
						typesymbol = "list";
					break;

				case EPythonTypes.Dict: typesymbol = "(dict)"; break;
				case EPythonTypes.Set: typesymbol = "(set)"; break;

				case EPythonTypes.DefinedClass:
					typesymbol = Identifier;
					break;

				case EPythonTypes.None: typesymbol = "(none)"; break;
				case EPythonTypes.Undefined: typesymbol = "(untyped)"; break;
			}

			return typesymbol;
		}

		public override readonly string ToString() {
			return GetTypeString();
		}


		// comparisons and hash that properly handle nested types

		public static bool operator ==(PythonType a, PythonType b) {
			return a.IsSameType(b);
		}
		public static bool operator !=(PythonType a, PythonType b) {
			return !a.IsSameType(b);
		}
		public override readonly bool Equals(object? obj) {
			if (obj == null) return false;
			PythonType other = (PythonType)obj;
			return obj != null && this.IsSameType(other);
		}
		public readonly bool Equals(PythonType other) {
			return this.IsSameType(other);
		}
		public override readonly int GetHashCode()
		{
			HashCode hash = new HashCode();
			hash.Add<int>(DataType.GetHashCode());
			hash.Add<int>(Identifier.GetHashCode());
			int N = (NestedTypes != null) ? NestedTypes.Length : 0;
			for (int i = 0; i < N; ++i)
				hash.Add<int>(NestedTypes![i].GetHashCode());
			return hash.ToHashCode();
		} 
	}



	/**
	 * PythonSymbol is used to store symbols parsed from python code - eg method parameters, return values, etc.
	 * Generally constructed by interrogating the Python code directly using the inspect module, in PythonLibrary.ParseModuleSource()
	 */
	public struct PythonSymbol
	{
		//! SymbolName is the optional string name associated with the type symbol (eg arg1: int - SymbolName is arg1)
		//! Default value is PythonSymbol.ANONYMOUS token, ie no specified name
		public string SymbolName = ANONYMOUS;

		//! standard python type of the symbol
		public EPythonTypes DataType = EPythonTypes.None;

		//! module for the symbol type (eg numpy.dtype - ModuleName is numpy)
		public string ModuleName = "";
		//! string identifier for the type - may be a class name (eg dtype) or an identifier token (eg DYNAMIC or RETURN below)
		public string TypeName = DYNAMIC;

		//! Optional nested symbols. If symbol is a List[type], Dict[a,b], there may be 1 or 2 NestedSymbols defining the type(s)
		//! If symbol is a Tuple, there will be N nested symbols
		public PythonSymbol[]? NestedSymbols = null;


		// SymbolName constants
		public const string ANONYMOUS = "#no_name#";

		// TypeName constants
		public const string DYNAMIC = "#dynamic#";
		public const string RETURN = "#return#";

		static public readonly PythonSymbol DefaultReturnType = new PythonSymbol(PythonSymbol.RETURN);

		public PythonSymbol()
		{
			SymbolName = ANONYMOUS;
			DataType = EPythonTypes.Undefined;
			TypeName = DYNAMIC;
		}

		public PythonSymbol(string? VariableName)
		{
			SymbolName = VariableName ?? ANONYMOUS;
			DataType = EPythonTypes.Undefined;
			TypeName = DYNAMIC;
		}

		public readonly bool IsTuple { get { return DataType == EPythonTypes.Tuple; } }
		public readonly bool HasNestedSymbols { get { return NestedSymbols != null; } }

		public PythonType GetPyType() { 
			return new PythonType(this); 
		} 

		public bool TrySetBuiltInType(string typeName)
		{
			TypeName = typeName;
			if (typeName == "str") {
				DataType = EPythonTypes.Str;
				return true;
			} else if (typeName == "int") {
				DataType = EPythonTypes.Int;
				return true;
			} else if (typeName == "float") {
				DataType = EPythonTypes.Float;
				return true;
			} else if (typeName == "complex") {
				DataType = EPythonTypes.Complex;
				return true;
			} else if (typeName == "bool") {
				DataType = EPythonTypes.Bool;
				return true;
			} else if (typeName == "tuple") {
				DataType = EPythonTypes.Tuple;
				return true;
			} else if (typeName == "list") {
				DataType = EPythonTypes.List;
				return true;
			} else if (typeName == "dict") {
				DataType = EPythonTypes.Dict;
				return true;
			} else if (typeName == "set") {
				DataType = EPythonTypes.Set;
				return true;
			} else if (typeName == "inspect._empty") {
				DataType = EPythonTypes.Undefined;
				TypeName = DYNAMIC;
				return true;
			}
			return false;
		}

		public void SetToBuiltInType(EPythonTypes type)
		{
			switch (type)
			{
				case EPythonTypes.Bool:
				case EPythonTypes.Int:
				case EPythonTypes.Float:
				case EPythonTypes.Str:
				case EPythonTypes.Complex:
					DataType = type;
					return;
				default:
					throw new Exception("PythonSymbol.SetToBuiltInType: invalid simple type");
			}
		}

		public void SetToDefinedClass(string moduleName, string className)
		{
			ModuleName = moduleName;
			TypeName = className;
			DataType = EPythonTypes.DefinedClass;
		}


		public void SetToTuple(IEnumerable<PythonSymbol> Arguments)
		{
			DataType = EPythonTypes.Tuple;
			NestedSymbols = Arguments.ToArray();
		}

		public void SetToList(PythonSymbol ListType)
		{
			DataType = EPythonTypes.List;
			NestedSymbols = new PythonSymbol[1];
			NestedSymbols[0] = ListType;
		}

		//! construct full type signature for this symbol (including nested types)
		public readonly string GetPyTypeString()
		{
			string typesymbol = "";
			switch (DataType)
			{
				case EPythonTypes.Str: typesymbol = "str"; break;
				case EPythonTypes.Int: typesymbol = "int"; break;
				case EPythonTypes.Float: typesymbol = "float"; break;
				case EPythonTypes.Complex: typesymbol = "complex"; break;
				case EPythonTypes.Bool: typesymbol = "bool"; break;

				case EPythonTypes.Tuple: 
					if ( NestedSymbols != null && NestedSymbols.Length > 0 )
					{
						string accum = NestedSymbols[0].GetPyTypeString();
						for (int j = 1; j < NestedSymbols.Length; ++j)
							accum += ", " + NestedSymbols[j].GetPyTypeString();
						typesymbol = $"({accum})";
					}
					else
						typesymbol = "(tuple)"; 
					break;

				case EPythonTypes.List:
					if (NestedSymbols != null && NestedSymbols.Length == 1)
					{
						string subType = NestedSymbols[0].GetPyTypeString();
						typesymbol = $"List[{subType}]";
					} else
						typesymbol = "list"; 
					break;

				case EPythonTypes.Dict: typesymbol = "(dict)"; break;
				case EPythonTypes.Set: typesymbol = "(set)"; break;

				case EPythonTypes.DefinedClass:
					typesymbol = (ModuleName.Length > 0) ? (ModuleName + "." + TypeName) : TypeName;
					break;

				case EPythonTypes.None: typesymbol = "(none)"; break;
				case EPythonTypes.Undefined: typesymbol = "(untyped)"; break;
			}

			return typesymbol;
		}


		public override readonly string ToString()
		{
			// todo: do we need a separate function that calls ToString() on the nested symbols?
			// can nested symbols ever have a SymbolName?

			string typesymbol = GetPyTypeString();
			if (SymbolName == ANONYMOUS || SymbolName == RETURN)
				return $"{typesymbol}";
			else
				return $"{typesymbol} {SymbolName}";
		}


		/**
		 * check if two PythonSymbols have equivalent type
		 */
		public readonly bool IsSameType(in PythonSymbol other)
		{
			// todo: should we just construct the PythonType for each and compare that?

			if (DataType != other.DataType)
				return false;
			if (HasNestedSymbols != other.HasNestedSymbols)
				return false;

			if (!HasNestedSymbols) {
				// anything else to check?
				// (does this even make sense to do??)
				return GetPyTypeString() == other.GetPyTypeString();
			}

			// have nested symbols. Do we need to check TypeName here...?

			if (NestedSymbols!.Length != other.NestedSymbols!.Length)
				return false;
			for ( int i = 0; i < NestedSymbols.Length; ++i )
			{
				PythonSymbol a = NestedSymbols[i];
				PythonSymbol b = other.NestedSymbols[i];
				if (a.IsSameType(b) == false)
					return false;
			}

			return true;
		}


	}



	// json read and write for PythonType, which gets serialized as custom data for some graph inputs like dynamic SplitTuple node
	public class PythonTypeJsonConverter : JsonConverter<PythonType>
	{
		public override PythonType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			JsonNode? node = JsonNode.Parse(ref reader);
			if (node == null)
				return new PythonType(EPythonTypes.Int);
			return read_pytype_node(node);
		}

		internal PythonType read_pytype_node(JsonNode node)
		{
			int dataTypeValue = node["DataType"]?.GetValue<int>() ?? (int)EPythonTypes.Int;
			EPythonTypes dataType = (EPythonTypes)dataTypeValue;
			string Identifier = node["Identifier"]?.GetValue<string>() ?? "";

			PythonType[]? nestedTypes = null;
			JsonArray? nestedTypeArray = node["NestedTypes"]?.AsArray() ?? null;
			if (nestedTypeArray != null)
			{
				nestedTypes = new PythonType[nestedTypeArray.Count];
				for (int i = 0; i < nestedTypeArray.Count; ++i)
					nestedTypes[i] = read_pytype_node(nestedTypeArray[i]!);
			}
			return new PythonType(dataType, Identifier, (nestedTypes != null) ? nestedTypes.ToArray() : null);

		}


		public override void Write(Utf8JsonWriter writer, PythonType value, JsonSerializerOptions options)
		{
			write_pytype(writer, ref value);
		}

		internal void write_pytype(Utf8JsonWriter writer, ref PythonType value)
		{
			writer.WriteStartObject();
			writer.WriteNumber("DataType", (int)value.DataType);
			writer.WriteString("Identifier", value.Identifier);
			if (value.NestedTypes != null && value.NestedTypes.Length > 0)
			{
				writer.WriteStartArray("NestedTypes");
				for (int i = 0; i < value.NestedTypes.Length; ++i)
					write_pytype(writer, ref value.NestedTypes[i]);
				writer.WriteEndArray();
			}
			writer.WriteEndObject();
		}


	}

}
