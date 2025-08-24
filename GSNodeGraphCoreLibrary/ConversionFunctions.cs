using Gradientspace.NodeGraph;
using System;

namespace GSNodeGraphCoreLibrary
{
    [NodeFunctionLibrary("Core.Conversion")]
    public static class GSConversionFunctions
    {
        [NodeFunction(IsPure=true)]
        public static int TryParseInt(string String, out bool Success, int DefaultValue = 0)
        {
            Success = Int32.TryParse(String, out int result);
            return (Success) ? result : DefaultValue;
        }

    }

}
