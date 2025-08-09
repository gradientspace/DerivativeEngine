// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Nodes
{

	[NodeFunctionLibrary("Gradientspace.IO")]
	public static class GradientspaceIOFunctionLibrary
	{

		// todo maybe should be a node w/ a dynamic type? so that input can return output type?
		[NodeFunction]
		public static object? PrintValue(object? Value, string Format = "{0}")
		{
			object showValue = (Value != null) ? Value : "null";
			GlobalGraphOutput.AppendLine(String.Format(Format, showValue), EGraphOutputType.User);
			return Value;
		}

	}

}
