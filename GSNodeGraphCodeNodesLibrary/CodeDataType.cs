// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{

    public class SourceCodeDataType
    {
        public string CodeText { get; set; } = "";

        public enum Language
        {
            CSharp = 0,
            Python = 1
        }
        public Language CodeLanguage = Language.CSharp;

        public SourceCodeDataType() { }

        public SourceCodeDataType MakeDuplicate()
        {
            return new SourceCodeDataType() { CodeText = this.CodeText, CodeLanguage = this.CodeLanguage };
        }

        public string DefaultFileSuffix { get { return GetSourceCodeFileSuffix(CodeLanguage); } }
        
        public static string GetSourceCodeFileSuffix(SourceCodeDataType.Language language)
        {
            switch (language)
            {
                case Language.CSharp: return ".cs";
                case Language.Python: return ".py";
            }
            return ".txt";
        }

        public static SourceCodeDataType MakeDefaultCSharp()
        {
            string text = String.Join(
                Environment.NewLine,
                "public class NodeClass",
                "{",
                "  public static int AddIntegers(int Number1, int Number2)",
                "  {",
                "      System.Console.WriteLine(\"Sum is {0}\", Number1+Number2);",
                "      return Number1+Number2;",
                "  }",
                "}");
            return new SourceCodeDataType() { CodeText = text, CodeLanguage = Language.CSharp };
        }


        public static SourceCodeDataType MakeDefaultPython()
        {
            string text = String.Join(
                Environment.NewLine,
                "def PyAddIntegers(Number1: int, Number2: int) -> int:",
                "   return Number1 + Number2"
                );
            return new SourceCodeDataType() { CodeText = text, CodeLanguage = Language.Python };
        }

    }



    //public class SourceCodeDataTypeNodeInput : StandardNodeInputBase
    //{
    //    public SourceCodeDataType LocalCode { get; set; }

    //    public delegate void CodeModifiedEventHandler(SourceCodeDataTypeNodeInput input);
    //    public event CodeModifiedEventHandler? OnSourceCodeModified;

    //    public SourceCodeDataTypeNodeInput(Type valueType, object initialValue) : base(valueType)
    //    {
    //        if (initialValue is SourceCodeDataType)
    //            LocalCode = ((SourceCodeDataType)initialValue).MakeDuplicate();
    //        else
    //            LocalCode = SourceCodeDataType.MakeDefault();
    //    }

    //    public override (object?, bool) GetConstantValue()
    //    {
    //        return (LocalCode, true);
    //    }

    //    public override void SetConstantValue(object NewValue)
    //    {
    //        if (NewValue is SourceCodeDataType) {
    //            LocalCode = ((SourceCodeDataType)NewValue).MakeDuplicate();
    //            OnSourceCodeModified?.Invoke(this);
    //        }
    //    }
    //}

}
