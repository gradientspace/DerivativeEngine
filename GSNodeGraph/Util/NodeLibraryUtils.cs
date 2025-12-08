// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

namespace Gradientspace.NodeGraph.Util
{
    public static class NodeLibraryUtils
    {
        public const string INSTALL_FOLDER = "%INSTALLDIR%";

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
            // installer actually creates a registry key on windows...
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            if ( Debugger.IsAttached || appPath.Contains("net9.0") )
                appPath = Path.Combine(appPath, "..", "..", "..", "..");        // this is hardcoded for rms80 dev environment, for now... (how to do another way??)
            else
                appPath = Path.Combine(appPath, "..");                          // in installed build, exe is copied in subfolder of installer folder
            appPath = Path.GetFullPath(appPath);

            int NumLoaded = 0;
            foreach (string path in paths)
            {
                // handle app-specific variables in paths
                string fullPath = path;
                if (path.Contains(INSTALL_FOLDER))
                    fullPath = path.Replace(INSTALL_FOLDER, appPath);

                if ( Directory.Exists(fullPath) )
                {
                    NumLoaded += FindAndLoadNodeLibraries(fullPath, bIncludeSubFolders);
                }
            }
            return NumLoaded;
        }



    }
}
