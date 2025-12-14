// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Nodes
{
	[NodeFunctionLibrary("Core.Environment")]
	public static class GSEnvironmentFunctions
	{
		//! run environments that support arguments (eg command-line, etc) should set them
		//! here and then the argument nodes below will fetch them
		public static string[] GlobalArguments = new string[] { };


		[NodeFunction]
		public static void SetArgs(string argumentString)
		{
			string[] args = argumentString.Split();
			GlobalArguments = args;
		}


		[NodeFunction]
		public static int GetNumArgs()
		{
			return GlobalArguments.Length;
		}

		[NodeFunction]
		public static string GetArg(int Index)
		{
			if (Index >= 0 && Index < GlobalArguments.Length)
				return GlobalArguments[Index];
			return "";
		}

		[NodeFunction]
		public static List<string> GetAllArgs()
		{
			return GlobalArguments.ToList();
		}

		//! search for argument that begins with Prefix, and return string with Prefix stripped off
		[NodeFunction]
		public static string FindArgFromPrefix(string Prefix, string DefaultString = "")
		{
			string? found = find_argument_from_prefix(Prefix, true);
			return found ?? DefaultString;
		}

		[NodeFunction]
		public static int FindIntArgFromPrefix(string Prefix, int DefaultValue = 0)
		{
			string? found = find_argument_from_prefix(Prefix, true);
			if (int.TryParse(found, out int Value))
				return Value;
			return DefaultValue;
		}

		[NodeFunction]
		public static double FindRealArgFromPrefix(string Prefix, double DefaultValue = 0)
		{
			string? found = find_argument_from_prefix(Prefix, true);
			if (double.TryParse(found, out double Value))
				return Value;
			return DefaultValue;
		}

		[NodeFunction]
		public static bool FindBoolArgFromPrefix(string Prefix, bool bDefaultValue = false)
		{
			string? found = find_argument_from_prefix(Prefix, true);
			if (bool.TryParse(found, out bool Value))
				return Value;
			return bDefaultValue;
		}


        [NodeFunction]
        public static string GetEnvVariable(string Name, out bool Exists, string DefaultString = "")
        {
            string? found = Environment.GetEnvironmentVariable(Name);
            Exists = (found != null);
            return Exists ? found! : DefaultString;
        }

        [NodeFunction]
        public static void SetEnvVariable(string Name, string NewValue)
        {
            Environment.SetEnvironmentVariable(Name, NewValue);
        }

        [NodeFunction]
        public static void ExpandEnvVariables(string ToExpand, out string Expanded)
        {
            Expanded = Environment.ExpandEnvironmentVariables(ToExpand);
        }



        // registry key...


        // internals/utilities

        public static string? find_argument_from_prefix(string prefix, bool bIgnoreCase)
		{
			StringComparison useComparison = (bIgnoreCase) ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;

			for ( int i = 0; i < GlobalArguments.Length; i++ )
			{
				if (GlobalArguments[i].StartsWith(prefix, useComparison))
					return GlobalArguments[i].Substring(prefix.Length);
			}
			return null;
		}


	}


	internal class EnvironmentNodes
	{
	}
}
