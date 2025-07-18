using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
	public interface IVariablesInterface
	{
		public bool CanCreateVariable(string Name, string Scope = "");
		public bool CreateVariable(string Name, Type type, object? initialValue = null, string Scope = "");
	}

	public class EvaluationContext
	{
		public required IVariablesInterface Variables;
	}



	public class StandardVariables : IVariablesInterface
	{
		public const string GlobalScope = "global";

		public class Variable
		{
			public string Scope = "";
			public string Name = "";
			public Type DataType = typeof(object);
			public object? Value = null;
		}

		Dictionary<string, Variable> Variables = new Dictionary<string, Variable>();


		public virtual bool CanCreateVariable(string Name, string Scope = "")
		{
			string key = $"{Scope}:{Name}";
			return ! Variables.ContainsKey(key);
		}

		public virtual bool CreateVariable(string name, Type type, object? initialValue = null, string scope = "")
		{
			string key = $"{scope}:{name}";
			if (Variables.ContainsKey(key))
				throw new Exception($"[StandardVariables.CreateVariable] {key} already exists");

			Variable v = new Variable() { Scope = scope, Name = name, DataType = type, Value = initialValue };
			Variables.Add(key, v);
			return true;
		}

	}

}
