// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Gradientspace.NodeGraph.Util
{
    public static class AssemblyUtils
    {
        public static List<Assembly> FindPotentialNodeAssemblies()
        {
            List<Assembly> AllAssemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());

            // filter out known system assemblies
            List<Assembly> TryAssemblies = new List<Assembly>();
            foreach (Assembly assembly in AllAssemblies) {
                if (assembly.FullName?.StartsWith("System.", StringComparison.InvariantCultureIgnoreCase) ?? true)
                    continue;
                if (assembly.FullName?.StartsWith("Microsoft.", StringComparison.InvariantCultureIgnoreCase) ?? true)
                    continue;

                // libraries used to build apps...can we identify these some other way?
                if (assembly.FullName?.StartsWith("Avalonia.", StringComparison.InvariantCultureIgnoreCase) ?? true)
                    continue;
                if (assembly.FullName?.StartsWith("SkiaSharp", StringComparison.InvariantCultureIgnoreCase) ?? true)
                    continue;
                if (assembly.FullName?.StartsWith("HarfBuzz", StringComparison.InvariantCultureIgnoreCase) ?? true)
                    continue;
                if (assembly.FullName?.StartsWith("Python", StringComparison.InvariantCultureIgnoreCase) ?? true)
                    continue;
                if (assembly.FullName?.StartsWith("geometry3Sharp", StringComparison.InvariantCultureIgnoreCase) ?? true)
                    continue;

                TryAssemblies.Add(assembly);
            }

            return TryAssemblies;
        }


        public static bool IsFunctionLibraryClass(Type type, out string LibraryName)
        {
            LibraryName = type.FullName!;
            NodeFunctionLibrary? LibraryAttrib = type.GetCustomAttribute<NodeFunctionLibrary>();
            if (LibraryAttrib == null)
                return false;
            if (LibraryAttrib.LibraryName == null || LibraryAttrib.LibraryName.Length == 0)
                return false;
            LibraryName = LibraryAttrib.LibraryName;
            return true;
        }


        public enum EAssemblyLoadResult
        {
            AlreadyLoaded,
            LoadedSuccessfully,
            FailedToLoad
        }


        /// <summary>
        /// Try to infer which Assemblies need to be loaded from C# code.
        /// So far, just parses out the 'using A.B.C' namespaces and tries to load based on namespace 
        /// </summary>
        public static bool TryLoadAssembliesFromCode(string codeText, bool bSuppressErrorMessages)
        {
            bool bAllOK = true;
            string[] lines = codeText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string line in lines) {

                // todo: this will fail for single-file .cs files...

                if (line.StartsWith("using ")) 
                {
                    string assemblyName = line.Replace("using ", "").Replace(';', ' ').Trim();
                    EAssemblyLoadResult loadResult = TryLoadAssemblyByNamespace(assemblyName, bSuppressErrorMessages);
                    if (loadResult == EAssemblyLoadResult.FailedToLoad)
                        bAllOK = false;
                    continue;
                }
                if (line.Contains("namespace")) break;      // as soon as we get to namespace we are done
                if (line.Contains("class")) break;      // as soon as we get to namespace we are done
            }
            return bAllOK;
        }

        /// <summary>
        /// Try to load the assembly for a 'using A.B.C;' namespace. This inherently is the wrong thing
        /// to do, as there is no direct connection between namespace name and assembly name. 
        /// 
        /// If A.B.C doesn't work, the code will recursively try A.B and then A
        /// 
        /// </summary>
        public static EAssemblyLoadResult TryLoadAssemblyByNamespace(string Namespace, bool bSilent = false)
        {
            Assembly? foundAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(asm => asm.FullName?.StartsWith(Namespace) ?? false );
            if (foundAssembly != null)
                return EAssemblyLoadResult.AlreadyLoaded;

            try {
                Assembly.Load(Namespace);
            } catch (Exception) {
                if (!bSilent)
                    GlobalGraphOutput.AppendError($"Failed to find or load assembly {Namespace}");

                // try loading parent namespace
                int idx = Namespace.LastIndexOf('.');
                if (idx > 0) {
                    string parentNamespace = Namespace.Substring(0, idx);
                    EAssemblyLoadResult parentResult = TryLoadAssemblyByNamespace(parentNamespace, bSilent);
                    if (parentResult != EAssemblyLoadResult.FailedToLoad)
                        return parentResult;
                }


                return EAssemblyLoadResult.FailedToLoad;
            }
            return EAssemblyLoadResult.LoadedSuccessfully;
        }


        public static EAssemblyLoadResult TryFindLoadAssembly(string QualifiedName, string DLLName)
        {
            Assembly? foundAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(asm => asm.FullName == QualifiedName);
            if (foundAssembly != null)
                return EAssemblyLoadResult.AlreadyLoaded;

            try
            {
                Assembly.Load(QualifiedName);
            }
            catch (Exception)
            {
                try
                {
                    Assembly.LoadFrom(DLLName);
                }
                catch (Exception)
                {
                    GlobalGraphOutput.AppendError($"Failed to load assembly {QualifiedName} from {DLLName}");
                    return EAssemblyLoadResult.FailedToLoad;
                }
            }

            return EAssemblyLoadResult.LoadedSuccessfully;
        }




        public static bool IsDotNetAssembly(string dllPath)
        {
            try {
                using (FileStream fileStream = new FileStream(dllPath, FileMode.Open, FileAccess.Read)) {
                    using (PEReader peReader = new PEReader(fileStream)) {
                        if (!peReader.HasMetadata) 
                            return false; // Not a .NET assembly
                        var metadataReader = peReader.GetMetadataReader();
                        return metadataReader.IsAssembly; // True if it's a .NET assembly
                    }
                }
            } catch {
                return false;
            }
        }

    }
}
