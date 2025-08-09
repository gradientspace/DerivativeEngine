// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Text;
using System.Diagnostics;

namespace Gradientspace.NodeGraph
{
    public class CodeBuilder
    {
        public bool ThrowExceptionOnWarning = true;

        public int SpacesPerIndent = 4;
        public bool IndentWithTabs = true;


        protected StringBuilder Builder = new StringBuilder();

        protected string IndentString = "";
        protected int IndentLevel = 0;


        public CodeBuilder()
        {
        }

        public CodeBuilder(CodeBuilder parentBuilder)
        {
            ThrowExceptionOnWarning = parentBuilder.ThrowExceptionOnWarning;
            SpacesPerIndent = parentBuilder.SpacesPerIndent;
            IndentWithTabs = parentBuilder.IndentWithTabs;
            IndentLevel = parentBuilder.IndentLevel;
            IndentString = parentBuilder.IndentString;
        }


        public string DebugString {
            get { return GetCode(); }
        }


        public virtual string GetCode()
        {
            return Builder.ToString();
        }

        public virtual void AppendEmptyLine(int Repeat = 1)
        {
            for ( int i = 0; i < Repeat; ++i )
                Builder.AppendLine("");
        }

        public virtual void AppendLine(string Line)
        {
            Builder.Append(IndentString);
            Builder.AppendLine(Line);
        }

        public virtual void AppendOpenBrace(bool bPushIndent = true)
        {
            Builder.Append(IndentString);
            Builder.AppendLine("{");
            if (bPushIndent)
                PushIndent();
        }

        public virtual void AppendCloseBrace(bool bPopIndent = true)
        {
            if (bPopIndent)
                PopIndent();
            Builder.Append(IndentString);
            Builder.AppendLine("}");
        }

        public virtual void AppendBlock(string Block)
        {
            // maybe more efficient to do find or substrings...
            string[] lines = Block.Split("\r\n");
            foreach (string line in lines) {
                Builder.Append(IndentString);
                Builder.AppendLine(line);
            }
        }

        public virtual void AppendLines(string[] Lines)
        {
            foreach (var line in Lines) {
                Builder.Append(IndentString);
                Builder.AppendLine(line);
            }
        }


        public virtual void PushIndent()
        {
            IndentLevel++;
            IndentString = (IndentWithTabs) ? new string('\t', IndentLevel) : new string(' ', SpacesPerIndent*IndentLevel);
        }

        public virtual void PopIndent()
        {
            if (IndentLevel > 0) {
                IndentLevel--;
                IndentString = (IndentWithTabs) ? new string('\t', IndentLevel) : new string(' ', SpacesPerIndent*IndentLevel);
            } else
                OnWarningFunc("tried to PopIndent at IndentLevel=0");
        }




        public virtual void OnWarningFunc(string warning) 
        {
            Debug.WriteLine(warning);
            if (ThrowExceptionOnWarning)
                throw new Exception($"CodeBuilder: OnWarningFunc: {warning}");
        }

    }
}
