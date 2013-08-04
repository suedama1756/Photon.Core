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
        private static readonly MethodInfo ToStringMethod = typeof(StandardConvert).GetMethod("ToString", BindingFlags.Static | BindingFlags.NonPublic);

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

        internal static string ToString<T>(T value) 
        {
            return value.ToString();
        }

        public static Func<TSource, TTarget> GetConvertDelegate<TSource, TTarget>()
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);

            // if we can't derive the type then its not supported
            var sourceNativeType = TypeFromTypeCode(Type.GetTypeCode(sourceType));
            if (sourceNativeType == null || TypeFromTypeCode(Type.GetTypeCode(targetType)) == null)
            {
                return null;
            }

            if (targetType == typeof(string))
            {
                return (Func<TSource, TTarget>)Delegate.CreateDelegate(
                    typeof(Func<, >).MakeGenericType(sourceType, targetType), 
                    ToStringMethod.MakeGenericMethod(sourceType));
            }

            //  bind method and create delegate
            var method = GetConvertMethod(sourceNativeType, targetType);
            if (method != null)
            {
                if (sourceType != sourceNativeType) // Enums 
                {
                    // we need an additional cast
                    var valueParameter = Expression.Parameter(sourceType);
                    return (Func<TSource, TTarget>)Expression.Lambda(
                        Expression.Call(method, 
                            Expression.Convert(valueParameter, sourceNativeType)
                        )
                    , valueParameter).Compile();
                }
                return (Func<TSource, TTarget>)Delegate.CreateDelegate(typeof(Func<, >).MakeGenericType(sourceType, targetType), method);
            }   
            return null;
        }
	}
}
