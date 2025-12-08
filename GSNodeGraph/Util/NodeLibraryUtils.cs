// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Gradientspace.NodeGraph.Util
{
    public static class NodeLibraryUtils
    {
        public static int FindAndLoadNodeLibraries(string directoryPath, bool bIncludeSubFolders = true)
        {
            int NumLoaded = 0;
            SearchOption searchOption = bIncludeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (string dirPath in Directory.EnumerateDirectories(directoryPath, "*", searchOption) )
            {
                foreach (string filePath in Directory.EnumerateFiles(dirPath, "*.dll", SearchOption.TopDirectoryOnly)) 
                {
                    if ( AssemblyUtils.IsDotNetAssembly(filePath) == false)
                        continue;

                    try {
                        Assembly LoadedAssembly = Assembly.LoadFrom(filePath);
                        GlobalGraphOutput.AppendLog($"Loaded {filePath}");
                        NumLoaded++;
                    } catch {
                        GlobalGraphOutput.AppendLog($"Failed to load {filePath}");
                        // ignore load failures
                    }
                }
            }
            return NumLoaded;
        }


        public static int FindAndLoadNodeLibraries(IEnumerable<string> paths, bool bIncludeSubFolders = true)
        {
            int NumLoaded = 0;
            foreach (string path in paths)
            {
                if ( Directory.Exists(path) )
                {
                    NumLoaded += FindAndLoadNodeLibraries(path, bIncludeSubFolders);
                }
            }
            return NumLoaded;
        }



    }
}
