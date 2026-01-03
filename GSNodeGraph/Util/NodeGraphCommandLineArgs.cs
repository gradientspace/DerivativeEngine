using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Util
{
    public static class NodeGraphCommandLineArgs
    {
        public const string NodeLibraryArgPrefix = "-AddNodeLibraryPath=";

        public static IEnumerable<string> EnumerateNodeLibraryPathArgs(string[] arguments)
        {
            foreach (string s in arguments) {
                if ( s.StartsWith(NodeLibraryArgPrefix, StringComparison.InvariantCultureIgnoreCase) ) {
                    int idx = s.IndexOf('=');
                    string path = s.Substring(idx+1);
                    path.Trim();
                    path.Trim('\"');
                    path.Trim();
                    yield return path;
                }
            }
        }

        public static string? FindStartupGraphFileArg(string[] arguments)
        {
            // perhaps could be smarter...
            for (int i = 0; i < arguments.Length; ++i) {
                if (arguments[i].StartsWith("-") == false)
                    return arguments[i];
            }
            return null;
        }

    }
}
