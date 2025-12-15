// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.CodeNodes
{
	public delegate void ICodeNodeCompileStatusEvent(INodeWithInlineCode SourceNode);

	public interface INodeWithInlineCode
	{
		SourceCodeDataType GetInlineSourceCode();
		void SetInlineSourceCode(SourceCodeDataType NewSourceCode);
		string GetCodeNameHint();

        // status of last compile attempt
        bool LastCompileOK { get; }
        IEnumerable<string> LastCompileMessages { get; }

        event ICodeNodeCompileStatusEvent? OnCompileStatusUpdate;
	}
}
