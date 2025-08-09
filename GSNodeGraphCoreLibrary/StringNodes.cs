// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;

namespace Gradientspace.NodeGraph
{
    [NodeFunctionLibrary("Gradientspace.String")]
    public static class GradientspaceStringFunctionLibrary
    {
        [NodeFunction]
        public static string Append(string A, string B) {
            return A + B;
        }

        [NodeFunction]
        public static bool Contains(string String, string Contains, bool bIgnoreCase = true)
        {
            StringComparison useComparison = (bIgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return String.Contains(Contains, useComparison);
        }

    }

}
