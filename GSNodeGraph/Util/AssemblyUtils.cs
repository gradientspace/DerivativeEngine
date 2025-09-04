// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Reflection;

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


    }
}
