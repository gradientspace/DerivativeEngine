using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.CodeNodes
{
	public delegate void ICodeNodeCompileStatusEvent(bool bCompileOK, List<string>? Errors);

	public interface INodeWithInlineCode
	{
		SourceCodeDataType GetInlineSourceCode();
		void SetInlineSourceCode(SourceCodeDataType NewSourceCode);
		string GetCodeNameHint();

		event ICodeNodeCompileStatusEvent? OnCompileStatusUpdate;
	}
}
