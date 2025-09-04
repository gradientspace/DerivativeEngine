// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Gradientspace.NodeGraph.TypeInfoLibrary;


namespace Gradientspace.NodeGraph
{
    public class TypeInfoLibrary
    {
        public struct DataTypeInfo
        {
            public Type dataType;

            public bool SupportsInputConstant = false;
            public object? DefaultConstantValue = null;

            public DataTypeInfo(Type type) { dataType = type; }


        }
        public Dictionary<Type, DataTypeInfo> Library;

        public TypeInfoLibrary(bool bLaunchAutoBackgroundBuild = false)
        {
            Library = new();

            RegisterType(typeof(bool), true, false);
            RegisterType(typeof(float), true, 0.0f);
            RegisterType(typeof(double), true, 0.0);
            RegisterType(typeof(int), true, 0);
            RegisterType(typeof(short), true, 0);
            RegisterType(typeof(long), true, 0);
            // todo: not all types are supported yet in (eg) BuildInputNodeForMethodArgument, NodeWidget, etc
            //RegisterType(typeof(byte), true, 0);
            //RegisterType(typeof(sbyte), true, 0);
            RegisterType(typeof(string), true, "");

            // generic enum type?
        }


        public void RegisterType(Type dataType, bool bSupportsInputConstant, object? defaultConstantValue = null)
        {
            if (Library.ContainsKey(dataType))
                throw new Exception($"TypeInfoLibrary already contains Type {dataType}!");

            DataTypeInfo typeInfo = new DataTypeInfo(dataType);
            typeInfo.SupportsInputConstant = bSupportsInputConstant;
            typeInfo.DefaultConstantValue = defaultConstantValue;
            Library.Add(dataType, typeInfo);
        }


        public bool TypeSupportsInputConstant(Type type)
        {
            if (Library.TryGetValue(type, out DataTypeInfo typeInfo))
                return typeInfo.SupportsInputConstant;
            return false;
        }


        public object? GetDefaultConstantValueForType(Type type)
        {
            if (Library.TryGetValue(type, out DataTypeInfo typeInfo))
                return typeInfo.DefaultConstantValue;
            return null;
        }



    }


    public sealed class DefaultTypeInfoLibrary
    {
        private static readonly TypeInfoLibrary instance = new TypeInfoLibrary(true);

        static DefaultTypeInfoLibrary() { }
        private DefaultTypeInfoLibrary() { }

        public static TypeInfoLibrary Instance {
            get {
                return instance;
            }
        }


        public static bool TypeSupportsInputConstant(Type type)
        {
            return Instance.TypeSupportsInputConstant(type);
        }

        public static object? GetDefaultConstantValueForType(Type type)
        {
            return Instance.GetDefaultConstantValueForType(type);
        }

    }

}
