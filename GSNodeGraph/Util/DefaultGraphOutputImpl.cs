using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
	public class DefaultGraphOutputImpl : IGraphOutput
	{
		struct OutputLine
		{
			public string Line;
			public EGraphOutputType Type;
		}
		List<OutputLine> OutputLines = new List<OutputLine>();

		public void AppendLine(string line, EGraphOutputType outputType = EGraphOutputType.User)
		{
			OutputLines.Add( new() { Line = line, Type = outputType } ); 
		}

		public void Clear()
		{
			OutputLines.Clear();
		}


		public IEnumerable<Tuple<string,EGraphOutputType>> EnumerateLines()
		{
			foreach (OutputLine l in OutputLines)
				yield return new(l.Line, l.Type);
		}
	}
}
