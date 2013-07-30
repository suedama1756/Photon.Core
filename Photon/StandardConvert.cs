using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq.Expressions;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Collections.ObjectModel;

namespace Photon.Data
{
    public class StandardConvert 
	{
        private static readonly TypeCode[] TypeCodes = (TypeCode[])Enum.GetValues(typeof(TypeCode));
        private static readonly int TypeCodeArraySize = TypeCodes.Select(x => (int)x).Max() + 1;
        private static readonly ReadOnlyCollection<Delegate> NoConvert = new ReadOnlyCollection<Delegate>(TypeCodes.Select(x => (Delegate)null).ToArray());

        public static ReadOnlyCollection<Delegate> GetToConvertDelegates<TSource>()
        {
            var sourceType = typeof(TSource);
            if (!typeof(IConvertible).IsAssignableFrom(sourceType))
            {
                return NoConvert;
            }

            //  get source type code
            var sourceTypeCode = Type.GetTypeCode(sourceType);

            // create converters
            var result = new Delegate[TypeCodeArraySize];
            foreach (var typeCode in TypeCodes)
            {
                var index = (int)typeCode;
                result[index] = StandardConvert.GetConvertDelegate(sourceTypeCode, typeCode);
            }

            return new ReadOnlyCollection<Delegate>(result);
        }

        public static ReadOnlyCollection<Delegate> GetConvertFromDelegates<TTarget>()
        {
            var targetType = typeof(TTarget);
            var targetTypeCode = Type.GetTypeCode(targetType);

            if (GetConvertMethodName(targetTypeCode) == null)
            {
                return NoConvert;
            }

            var result = new Delegate[TypeCodeArraySize];
            foreach (var typeCode in TypeCodes)
            {
                var index = (int)typeCode;
                result[index] = StandardConvert.GetConvertDelegate(typeCode, targetTypeCode);
            }
            return new ReadOnlyCollection<Delegate>(result);
        }

        private static MethodInfo GetConvertMethod(Type sourceType, Type targetType)
        {
            // get conversion method name (e.g. ToInt32, ToBoolean)
            var methodName = GetConvertMethodName(targetType);
            if (methodName == null)
            {
                return null;
            }

            // bind to appropriate method
            var methodInfo = typeof(Convert).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public, null, new Type[] {
                sourceType
            }, null);
            return methodInfo;
        }

        private static string GetConvertMethodName(Type type)
        {
            return GetConvertMethodName(Type.GetTypeCode(type));
        }

		private static string GetConvertMethodName(TypeCode typeCode) 
		{
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return "ToBoolean";
                case TypeCode.Char:
                    return "ToChar";
                case TypeCode.SByte:
                    return "ToSByte";
                case TypeCode.Byte:
                    return "ToByte";
                case TypeCode.Int16:
                    return "ToInt16";
                case TypeCode.UInt16:
                    return "ToUInt16";
                case TypeCode.Int32:
                    return "ToInt32";
                case TypeCode.UInt32:
                    return "ToUInt32";
                case TypeCode.Int64:
                    return "ToInt64";
                case TypeCode.UInt64:
                    return "ToUInt64";
                case TypeCode.Single:
                    return "ToSingle";
                case TypeCode.Double:
                    return "ToDouble";
                case TypeCode.Decimal:
                    return "ToDecimal";
                case TypeCode.DateTime:
                    return "ToDateTime";
                case TypeCode.String:
                    return "ToString";
            }
			return null;
		}

		private static Type TypeFromTypeCode(TypeCode typeCode) 
		{
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return typeof(Boolean);
                case TypeCode.Char:
                    return typeof(Char);
                case TypeCode.SByte:
                    return typeof(SByte);
                case TypeCode.Byte:
                    return typeof(Byte);
                case TypeCode.Int16:
                    return typeof(Int16);
                case TypeCode.UInt16:
                    return typeof(UInt16);
                case TypeCode.Int32:
                    return typeof(Int32);
                case TypeCode.UInt32:
                    return typeof(UInt32);
                case TypeCode.Int64:
                    return typeof(Int64);
                case TypeCode.UInt64:
                    return typeof(UInt64);
                case TypeCode.Single:
                    return typeof(Single);
                case TypeCode.Double:
                    return typeof(Double);
                case TypeCode.Decimal:
                    return typeof(Decimal);
                case TypeCode.DateTime:
                    return typeof(DateTime);
                case TypeCode.String:
                    return typeof(String);
            }
			return null;
		}

		public static Func<TSource, TTarget> GetConvertDelegate<TSource, TTarget>() 
		{
			return (Func<TSource, TTarget>)GetConvertDelegate(
                Type.GetTypeCode(typeof(TSource)), 
                Type.GetTypeCode (typeof(TTarget)));
		}

        public static Delegate GetConvertDelegate(TypeCode source, TypeCode target) 
		{
			var sourceType = TypeFromTypeCode(source);
			var targetType = TypeFromTypeCode(target);
			
            // if we can't derive the type then its not supported
            if (sourceType == null || targetType == null) 
			{
				return null;
			}

            //  bind method and create delegate
            var method = GetConvertMethod(sourceType, targetType);
            return method != null ? 
                    Delegate.CreateDelegate(typeof(Func<, >).MakeGenericType(sourceType, targetType), method) : 
                    null;
		}
	}
}
