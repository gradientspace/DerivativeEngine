// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;

namespace Gradientspace.NodeGraph
{
    [NodeFunctionLibrary("Core.Filesystem")]
    public static class GSFilesystemFunctions
    {
        // https://learn.microsoft.com/en-us/dotnet/api/system.io.directory?view=net-9.0
        // https://learn.microsoft.com/en-us/dotnet/api/system.io.path?view=net-9.0
        // https://learn.microsoft.com/en-us/dotnet/api/system.io.file?view=net-9.0

        // current directory
        // FileExists, DirectoryExists
        // FindFile

        //System.IO.Directory
        //System.IO.File
        //System.IO.Path


        [NodeFunction]
        public static bool FileExists(string Path)
        {
            return false;
        }

    }

}
