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

        public static int TryLoadNodeLibraryDLLs(string directory)
        {
            int NumLoaded = 0;
            foreach (string filePath in Directory.EnumerateFiles(directory, "*.dll", SearchOption.TopDirectoryOnly)) 
            {
                if ( AssemblyUtils.IsDotNetAssembly(filePath) == false)
                    continue;

                if (TryLoadNodeLibraryFromDLL(filePath))
                    NumLoaded++;
            }
            return NumLoaded;
        }


        public static int FindAndLoadNodeLibraries(string directoryPath, bool bIncludeSubFolders = true)
        {
            // try loading in top-level folder
            int NumLoaded = TryLoadNodeLibraryDLLs(directoryPath);

            // look in subdirectories 
            SearchOption searchOption = bIncludeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (string subDirPath in Directory.EnumerateDirectories(directoryPath, "*", searchOption) )
            {
                NumLoaded += TryLoadNodeLibraryDLLs(subDirPath);
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



        // This is some pretty hacky business to try to manage loaded NodeLibrary assemblies.
        // Probably needs to be significantly rewritten to handle various things:
        //  1) ExecutionGraphSerializer also tries to load assemblies referenced in .gg file.
        //     It currently calls NotifyNodeLibraryLoadedExternally(), but it doesn't go through
        //     the assembly-loading paths here, and it should.
        //  2) In both cases, assemblies are loaded into default domain. This means we cannot
        //     unload them. They need to be loaded into some other domain where we can guarantee
        //     there are no references to any objects, to be unloaded
        //  3) Currently punting on handling different versions. This should also be supported...?


        public enum ELibraryLoadCheckResult
        {
            NotLoaded = 0,
            AlreadyLoaded_ExactPath = 1,
            AlreadyLoaded_DifferentPath = 2
        }


        public static ELibraryLoadCheckResult CheckIfNodeLibraryAlreadyLoaded(string DLLPath, out string? loadedPath)
        {
            loadedPath = null;
            string baseDLLName = Path.GetFileNameWithoutExtension(DLLPath);
            bool bFound = LoadedDLLs.TryGetValue(baseDLLName, out LoadedNodeLibraryDLLInfo loadedDLL);
            if (bFound) {
                if (loadedDLL.DLLPath == DLLPath)
                    return ELibraryLoadCheckResult.AlreadyLoaded_ExactPath;
                loadedPath = loadedDLL.DLLPath;
                return ELibraryLoadCheckResult.AlreadyLoaded_DifferentPath;
            }
            return ELibraryLoadCheckResult.NotLoaded;
        }

        public static void NotifyNodeLibraryLoadedExternally(Assembly assembly)
        {
            string baseDLLName = Path.GetFileNameWithoutExtension(assembly.Location);
            LoadedDLLs.Add(baseDLLName, new LoadedNodeLibraryDLLInfo() { LoadedAssembly = assembly, DLLPath = assembly.Location });
        }




        // internal implementation


        private struct LoadedNodeLibraryDLLInfo
        {
            public Assembly LoadedAssembly;
            public string DLLPath;
        }

        private static Dictionary<string, LoadedNodeLibraryDLLInfo> LoadedDLLs = new();


        public static bool TryLoadNodeLibraryFromDLL(string DLLPath)
        {
            string baseDLLName = Path.GetFileNameWithoutExtension(DLLPath);
            bool bAlreadyLoaded = LoadedDLLs.TryGetValue(baseDLLName, out LoadedNodeLibraryDLLInfo loadedDLL);

            if ( bAlreadyLoaded ) {
                if (loadedDLL.DLLPath == DLLPath)
                    return true;
                GlobalGraphOutput.AppendError($"Tried to load NodeLibrary {baseDLLName} from {DLLPath}," +
                    $"but it has already been loaded from {loadedDLL.DLLPath}. Skipping new DLL/path.");
                return false;
            }

            try {
                Assembly loadedAssembly = Assembly.LoadFrom(DLLPath);
                GlobalGraphOutput.AppendLog($"Loaded NodeLibrary {baseDLLName} from {DLLPath}");
                LoadedDLLs.Add(baseDLLName, new LoadedNodeLibraryDLLInfo() { LoadedAssembly = loadedAssembly, DLLPath = DLLPath });
                return true;
            } catch {
                GlobalGraphOutput.AppendLog($"Failed to load NodeLibrary from {DLLPath}");
                return false;
            }

        }

    }
}
