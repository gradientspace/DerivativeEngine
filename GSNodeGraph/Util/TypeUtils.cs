// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
    public static class TypeUtils
    {
        public static bool IsNumericType(Type t)
        {
            return (t.IsPrimitive && t.IsValueType
                && t != typeof(nuint) && t != typeof(nint) && t != typeof(bool) && t != typeof(char));
        }

        public static bool IsRealType(Type t)
        {
            return t == typeof(float) || t == typeof(double);
        }

        public static bool IsIntegerType(Type t)
        {
            return IsNumericType(t) && !IsRealType(t);
        }

        public static bool IsNullableType(Type t)
        {
            if (!t.IsValueType) return true;
            if (Nullable.GetUnderlyingType(t) != null) return true;
            return false;
        }

        public static bool IsIntegerType(Type t, out bool bIsUnsigned, out int NumBytes)
        {
            if (t == typeof(byte) || t == typeof(sbyte))
            {
                bIsUnsigned = (t == typeof(byte)); NumBytes = sizeof(byte); return true;
            }
            else if (t == typeof(short) || t == typeof(ushort))
            {
                bIsUnsigned = (t == typeof(ushort)); NumBytes = sizeof(ushort); return true;
            }
            else if (t == typeof(int) || t == typeof(uint))
            {
                bIsUnsigned = (t == typeof(uint)); NumBytes = sizeof(uint); return true;
            }
            else if (t == typeof(long) || t == typeof(ulong))
            {
                bIsUnsigned = (t == typeof(ulong)); NumBytes = sizeof(ulong); return true;
            }
            bIsUnsigned = false; NumBytes = 0; return false;
        }


        public static bool IsLossyNumericConversion(Type From, Type To)
        {
            bool bFromIsInt = IsIntegerType(From, out bool bFromIsUnsigned, out int FromBytes);
            bool bToIsInt = IsIntegerType(To, out bool bToIsUnsigned, out int ToBytes);
            bool bFromIsReal = IsRealType(From), bToIsReal = IsRealType(To);
            if ( bFromIsReal && bToIsReal) {
                return From == typeof(double) && To == typeof(float);
            } else if ( (bFromIsReal && bToIsInt) || (bFromIsInt && bToIsReal) ) {
                return true;
            } else if (bFromIsInt && bToIsInt) {
                if (!bFromIsUnsigned && bToIsUnsigned)          // signed to unsigned  
                    return true;
                if (bFromIsUnsigned != bToIsUnsigned)           // unsigned to signed is ok if signed has more bytes
                    return FromBytes >= ToBytes;
            }
            return false;
        }


        public static string TypeToString(Type type)
        {
            if (type == typeof(float))
                return "float";
            else if (type == typeof(double))
                return "double";
            else if (type == typeof(byte))
                return "byte";
            else if (type == typeof(int))
                return "int";
            else if (type == typeof(uint))
                return "uint";
            else if (type == typeof(long))
                return "long";
            else if (type == typeof(ulong))
                return "ulong";
            else if (type.GetGenericArguments().Length > 0)
                return MakeNiceGenericName(type);
            else
                return type.Name;
        }


        public static string MakeNiceGenericName(Type type)
        {
            if (type.GetGenericArguments().Length == 0)
                return type.Name;
            var genericArguments = type.GetGenericArguments();
            var typeDefinition = type.Name;
            var unmangledName = typeDefinition.Substring(0, typeDefinition.IndexOf("`"));
            var generics = "<" + String.Join(",", genericArguments.Select(MakeNiceGenericName)) + ">";

            if (unmangledName == "Func")
            {
                int last_comma = generics.LastIndexOf(',');
                if (last_comma >= 0)
                    generics = generics.Insert(last_comma + 1, " (ret) ");
            }

            return unmangledName + generics;
        }


        public static string TypeToString(GraphDataType graphType)
        {
            if (graphType.ExtendedTypeInfo != null)
                return graphType.ExtendedTypeInfo.GetCustomTypeString() ?? TypeToString(graphType.DataType);
            return TypeToString(graphType.DataType);
		}


        public static bool IsWritableProperty(PropertyInfo property)
        {
            MethodInfo? setMethod = property.SetMethod;
            if (setMethod == null) return false;
            if (setMethod.IsPublic == false) return false;

            Type[] setReturnModifiers = setMethod.ReturnParameter.GetRequiredCustomModifiers();
            bool bIsInitOnly = setReturnModifiers.Contains(typeof(System.Runtime.CompilerServices.IsExternalInit));

            return bIsInitOnly == false;
        }


        public static bool TypeImplementsInterface<T>(Type t)
        {
            return t.IsAssignableTo(typeof(T));
        }

        public static bool IsEnumerable(Type t)
        {
            return TypeImplementsInterface<System.Collections.IEnumerable>(t);
        }
        public static bool IsEnumerable(object o)
        {
            return TypeImplementsInterface<System.Collections.IEnumerable>(o.GetType());
        }


        //! return the inner Type T of an IEnumerable<T>, or an object that implements that interface (including arrays)
        //! returns null if not of a suitable type
        public static Type? GetEnumerableElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            // if Type is literally an IEnumerable<T> then we can just use it's first generic type
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];

            // find IEnumerable<T> interface and get it's type. Note that this can somehow fail for
            // weird cases where a class implements multiple IEnumerable<T>, that just won't be supported...
            Type[] interfaces = type.GetInterfaces();
            int N = interfaces.Length;
            for ( int i = 0; i < N; ++i ) {
                if (interfaces[i].IsGenericType && interfaces[i].GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return interfaces[i].GenericTypeArguments[0];
            }

            return null;
        }


        /**
         * Search through the available constructors for a given Type and find one that
         * can be executed without any input parameters. Ideally this will just be a
         * default constructor SomeClass(), however it will also work if there are 
         * arguments with defaults, ie SomeClass(bool bParam1 = true, float Param2 = 2.0f).
         * Returns null if no such constructor can be found.
         */
        public static Func<object>? FindParameterlessConstructorForType(Type t)
        {
            ConstructorInfo? defaultConstructor = t.GetConstructor(Type.EmptyTypes);
            if (defaultConstructor != null)
            {
                return () => {  return defaultConstructor.Invoke(null); };
            }

            ConstructorInfo[] constructors = t.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            for ( int i = 0; i < constructors.Length; ++i )
            {
                ConstructorInfo? constructor = constructors[i];
                ParameterInfo[] Parameters = constructor.GetParameters();
                int NumParameters = Parameters.Length;
                bool bAllHaveDefaults = true;
                for ( int j = 0; j < NumParameters; ++j )
                    bAllHaveDefaults = bAllHaveDefaults && Parameters[j].HasDefaultValue;
                if (bAllHaveDefaults == false)
                    continue;

                object[] missingParams = new object[NumParameters];
                for (int j = 0; j < missingParams.Length; ++j)
                    missingParams[j] = Type.Missing;

                return () => {
                    return constructor.Invoke(BindingFlags.OptionalParamBinding | BindingFlags.InvokeMethod | BindingFlags.CreateInstance,
                                                null, missingParams, CultureInfo.InvariantCulture);
                };
            }

            return null;
        }


        /**
         * Construct an assembly-qualified name without version information for a Type
         * eg "g3.DMesh3, geometry3Sharp"
         * This will work with Type.GetType()...
         */
        public static string MakePartialQualifiedTypeName(Type type)
        {
            // for types in System libraries we do not need the assembly name... ?
            if (type.FullName != null && type.FullName.StartsWith("System."))
                return type.FullName;

            string baseName = type.AssemblyQualifiedName!;
            string[] substrings = baseName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return $"{substrings[0]}, {substrings[1]}";
        }


        /**
         * Try to find a Type named typeName in all loaded assemblies. Optionally specify the assembly short name.
         * 
         * If typeName is a fully-qualified name (ie "namespace.type, assembly, version, ...") then Type.GetType() should find it.
         * If not, the fq name is parsed and we search for the namespace.type in the specified assembly
         * (I think the fully-qualified type search would fail if the loaded assembly version is different...)
         * 
         * if the typeName is a namespace.type name, we search all assemblies (or optionally just the specified named assembly)
         */
        public static Type? FindTypeInLoadedAssemblies(string typeName, string? assemblyName = null)
        {
            string AssemblyName = assemblyName ?? string.Empty;

            bool bIsQualifiedName = typeName.Contains(',');
            if (bIsQualifiedName)
            {
                Type? QualifiedType = Type.GetType(typeName);
                if (QualifiedType != null)
                    return QualifiedType;
            }

            if ( bIsQualifiedName )
            {
                string[] tokens = typeName.Split(',', StringSplitOptions.TrimEntries);
                typeName = tokens[0];
                AssemblyName = tokens[1];
            }

            // conceivably could still search with the short name, it's just pretty risky...
            bool bIsFullName = typeName.Contains('.');
            if (!bIsFullName)
                return null;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                AssemblyName curName = assembly.GetName();
                if (AssemblyName.Length > 0 && curName.Name != AssemblyName)
                    continue;

                foreach (Type type in assembly.GetTypes()) {
                    if (typeName == type.FullName )
                        return type;
                }
            }
            return null;
        }



        public struct EnumInfo
        {
            public Type EnumType;
            public Type UnderlyingType;

            public object[] EnumValues;
            public int[] EnumIDs;           // note that some ints might be re-used more than once!
            public string[] EnumStrings;

            public int NumEnumValues { get { return EnumValues.Length; } }

            public bool FindIndexForEnumValue(object enumValue, out int FoundIndex)
            {
                FoundIndex = -1;
                Debug.Assert(enumValue.GetType() == EnumType);
                int N = EnumValues.Length;
                for (int i = 0; i < N; ++i) {
                    object? convert1 = Convert.ChangeType(EnumValues[i], UnderlyingType);
                    object? convert2 = Convert.ChangeType(enumValue, UnderlyingType);
                    if (convert1.Equals(convert2)) {
                        FoundIndex = i;
                        return true;
                    }
                }
                return false;
            }

            public object? FindEnumValueFromString(string EnumString)
            {
                for (int i = 0; i < EnumStrings.Length; ++i) {
                    if (EnumStrings[i] == EnumString)
                        return EnumValues[i];
                }
                return null;
            }

            public object? FindEnumValueFromID(int EnumID)
            {
                for ( int i = 0; i < EnumIDs.Length; ++i) {
                    if (EnumIDs[i] == EnumID)
                        return EnumValues[i];
                }
                return null;
            }
        }

        public static bool GetEnumInfo(Type enumType, out EnumInfo enumInfo)
        {
            enumInfo = new EnumInfo();
            if (enumType.IsEnum == false) return false;

            enumInfo.EnumType = enumType;
            enumInfo.UnderlyingType = System.Enum.GetUnderlyingType(enumType);
            // should verify this is a type compatible w/ int32...

            Array enumValues = System.Enum.GetValues(enumType);
            string[] enumStrings = System.Enum.GetNames(enumType);
            Array enumTypedValues = System.Enum.GetValuesAsUnderlyingType(enumType);
            Debug.Assert(enumValues.Length == enumStrings.Length && enumTypedValues.Length == enumValues.Length);
            int N = enumValues.Length;

            //object? defaultValue = Graph.GetNodeConstantValue(OwningNodeIdentifier, InputName);
            enumInfo.EnumStrings = enumStrings;
            enumInfo.EnumValues = new object[N];
            enumInfo.EnumIDs = new int[N];
            for ( int i = 0; i < N; i++ )
            {
                enumInfo.EnumValues[i] = enumValues.GetValue(i)!;
                enumInfo.EnumIDs[i] = (int)enumTypedValues.GetValue(i)!;
            }

            return true;
        }



        public static Type? FindEnumTypeFromFullName(string enumName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(enumName);
                if (type == null)
                    continue;
                if (type.IsEnum)
                    return type;
            }
            return null;
        }

    }
}
