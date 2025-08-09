// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
    public static class CodeGenUtils
    {
        public static void CheckArgs(string[]? Arguments, int NumRequired, string FromNode)
        {
            int NumArgs = (Arguments == null) ? 0 : Arguments.Length;
            if (NumArgs != NumRequired)
                throw new Exception($"{FromNode} CodeGen expected {NumRequired} arguments, got {NumArgs}");
        }
        public static void CheckArgsAndOutputs(string[]? Arguments, int NumRequired, string[]? OutputNames, int NumRequiredOutputs, string FromNode)
        {
            int NumOutputs = (OutputNames == null) ? 0 : OutputNames.Length;
            if (NumOutputs != NumRequiredOutputs)
                throw new Exception($"{FromNode} CodeGen expected a {NumRequiredOutputs} outputs, received {NumOutputs}");

            int NumArgs = (Arguments == null) ? 0 : Arguments.Length;
            if (NumArgs != NumRequired)
                throw new Exception($"{FromNode} CodeGen expected {NumRequired} arguments, received {NumArgs}");
        }


        public static string SanitizeVarName(string Name)
        {
            string NewName = Char.ToLower(Name[0]) + Name.Substring(1);

            // todo;
            return NewName;
        }



        public static string GetCSharpTypeDecl(Type t)
        {
            return TypeUtils.TypeToString(t);
        }

    }
}
