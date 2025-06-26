using Gradientspace.NodeGraph;

namespace Gradientspace.NodeGraph
{
    [GraphNodeFunctionLibrary("Gradientspace.String")]
    public static class GradientspaceStringFunctionLibrary
    {
        [NodeFunction]
        public static string Append(string A, string B) {
            return A + B;
        }

    }

}