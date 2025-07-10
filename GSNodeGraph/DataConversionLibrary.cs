// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Gradientspace.NodeGraph
{

	/**
	 * DataTypeConversionPair is intended to (eg) be used as a Dictionary key
	 * for an available datatype conversion
	 */
	public struct DataTypeConversionPair : IEquatable<DataTypeConversionPair>
	{
		public GraphDataType From;
		public GraphDataType To;

		public DataTypeConversionPair(GraphDataType from, GraphDataType to)
		{
			From = from; To = to;
		}

		public readonly bool Equals(DataTypeConversionPair other)
		{
			return From.IsSameType(other.From) && To.IsSameType(other.To);
		}
	}



	public class DataConversionLibrary
	{
		Dictionary<DataTypeConversionPair, IDataTypeConversion> Conversions = new Dictionary<DataTypeConversionPair, IDataTypeConversion>();

		public void AddConversion(IDataTypeConversion conversion)
		{
			DataTypeConversionPair key = new DataTypeConversionPair(conversion.FromType, conversion.ToType);
			Conversions.Add(key, conversion);
		}

		public virtual bool Find(GraphDataType FromType, GraphDataType ToType, out IDataTypeConversion? foundConversion)
		{
			foundConversion = null;
			DataTypeConversionPair key = new DataTypeConversionPair(FromType, ToType);
			if (Conversions.TryGetValue(key, out foundConversion))
				return true;
			return false;
		}


		public virtual void Build()
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (Type type in assembly.GetTypes())
				{
					bool bCheckForConversions =
						(type.GetCustomAttribute<GraphNodeFunctionLibrary>() != null);
					if (bCheckForConversions == false)
						continue;


					foreach (MethodInfo methodInfo in type.GetMethods())
					{
						if (methodInfo.IsStatic == false) continue;

						Attribute? isConversion = methodInfo.GetCustomAttribute(typeof(GraphDataTypeConversion));
						if (isConversion != null)
						{
							DataTypeConverterMethod convertMethod = new DataTypeConverterMethod(methodInfo);
							AddConversion(convertMethod);
						}

						Attribute? isRegister = methodInfo.GetCustomAttribute(typeof(GraphDataTypeRegisterFunction));
						if (isRegister != null)
						{
							methodInfo.Invoke(null, new object[] { this } );
						}
					}
				}
			}
		}

	}



	public sealed class GlobalDataConversionLibrary
	{
		private static readonly DataConversionLibrary instance = new DataConversionLibrary();

		static GlobalDataConversionLibrary() {

			instance.Build();

			// tests
			//instance.AddConversion(new ListToArrayTypeConverter_Test<int>());
		}
		private GlobalDataConversionLibrary() { }

		public static DataConversionLibrary Instance {
			get {
				return instance;
			}
		}


		public static bool Find(GraphDataType FromType, GraphDataType ToType, out IDataTypeConversion? foundConversion)
		{
			return instance.Find(FromType, ToType, out foundConversion);
		}

	}



	public class DataTypeConverterMethod : DataTypeConverterBase
	{
		MethodInfo method;

		public DataTypeConverterMethod(MethodInfo methodInfo)
			: base(methodInfo.GetParameters()[0].ParameterType, methodInfo.ReturnType)
		{
			if (methodInfo.IsStatic == false)
				throw new Exception($"DataTypeConverterMethod() - input method {methodInfo.Name} is not static!");
			method = methodInfo;
		}

		public override object Convert(object o)
		{
			return method.Invoke(null, new object[] { o })!;
		}
	}


	public class DataTypeConverterLambda : DataTypeConverterBase
	{
		Func<object, object> ConvertFunc;

		public DataTypeConverterLambda(
			GraphDataType inputType,
			GraphDataType outputType,
			Func<object, object> convertFunc)
			: base(inputType, outputType)
		{
			ConvertFunc = convertFunc;
		}

		public override object Convert(object o)
		{
			return ConvertFunc(o);
		}
	}





	public class ListToArrayTypeConverter_Test<T> : DataTypeConverterBase
	{
		public ListToArrayTypeConverter_Test() : base(
			new GraphDataType(typeof(List<T>)),
			new GraphDataType(typeof(T[])))
		{ }

		public override object Convert(object o)
		{
			List<T> list = (List<T>)o;
			return list.ToArray();
		}
	}





}
