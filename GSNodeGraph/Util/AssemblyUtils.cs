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
