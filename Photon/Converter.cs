using System;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;

namespace Photon.Data
{
    public class Converter
    {
        private static readonly Delegate NullDelegate = (Action)(() => {});
        private static readonly MethodInfo ConvertConverterMethod = typeof(Converter).GetMethods(
            BindingFlags.NonPublic | BindingFlags.Static).First(x => x.Name == "GetConverter" && x.IsGenericMethodDefinition);
        private static readonly MethodInfo ClassToStringMethod = typeof(Converter).GetMethod("ClassToString", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo StructToStringMethod = typeof(Converter).GetMethod("StructToString", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly ConcurrentDictionary<Type, Delegate> _boxedMap = new ConcurrentDictionary<Type, Delegate>();

        private static class Convert<T>
        {
            public static readonly bool IsSupportedConversionTarget = GetIsSupportedConversionTarget();

            public static Func<T, bool> IsNull = InitializeIsNull() ; 

            /// <summary>
            /// Inner static type used to convert to specific target type
            /// </summary>
            internal static class To<TTarget>
            {
                private static readonly Delegate ConverterInstance = InitializeInstance();

                private static Delegate InitializeInstance()
                {
                    return GetStandardConvertDelegate<T, TTarget>() ??
                        GenerateConverter(typeof(T), typeof(TTarget)) ??
                            NullDelegate;
                }

                public static Func<T, TTarget>  Converter
                {
                    get { return ConverterInstance as Func<T, TTarget>; }
                }
            }

            private static bool GetIsSupportedConversionTarget()
            {
                return typeof(T).IsValueType || 
                    typeof(T) == typeof(string);
            }

            private static bool False(T value) 
            {
                return false;
            }

            private static Func<T, bool> InitializeIsNull()
            {
                if (Nullable.GetUnderlyingType(typeof(T)) == null && typeof(T).IsValueType) 
                {
                    return False;
                }

                var parameter = Expression.Parameter(typeof(T));
                return Expression.Lambda<Func<T, bool>>(Expression.Equal(
                    parameter, Expression.Constant(null)), parameter).Compile();
            }
        }

        public static TTarget Convert<TSource, TTarget>(TSource source)
        {
            var converter = GetConverter<TSource, TTarget>();
            if (converter != null)
            {
                return converter(source);
            }

            if (!IsNull(source)) 
            {
                var sourceType = source.GetType();
                if (sourceType.IsValueType)
                {
                    Delegate convertDelegate;
                    if (!_boxedMap.TryGetValue(sourceType, out convertDelegate))
                    {
                        var method = typeof(Converter).GetMethod("UnboxAndConvert", BindingFlags.Static | BindingFlags.NonPublic);
                        convertDelegate = Delegate.CreateDelegate(
                            typeof(Func<TSource, TTarget>),
                            method.MakeGenericMethod(source.GetType(), typeof(TTarget)));
                        _boxedMap.TryAdd(sourceType, convertDelegate);
                    }
                    return ((Func<TSource, TTarget>)convertDelegate)(source);
                }
            }

            return (TTarget)(object)source;
        }

        private static TTarget UnboxAndConvert<TSource, TTarget>(object o) 
        {
            var converter = GetConverter<TSource, TTarget>();
            if (converter != null) {
                return converter((TSource)o);
            }
            return (TTarget)o;
        }


        public static bool IsNull<T>(T value) 
        {
            return Convert<T>.IsNull(value);
        }

        /// <summary>
        /// Gets a converter delegate that can be used to convert from the <paramref name="sourceType"/> to <paramref name="targetType"/>.
        /// </summary>
        /// <returns>The converter.</returns>
        /// <param name="sourceType">Source type.</param>
        /// <param name="targetType">Target type.</param>
        private static Delegate GetConverter(Type sourceType, Type targetType)
        {
            return (Delegate)ConvertConverterMethod.MakeGenericMethod(
                sourceType, targetType).Invoke(null, new object[0]);
        }

        /// <summary>
        /// Gets a typed converter delegate that can be used to convert from <typeparamref="TSource" /> to <typeparamref="TTarget"/>
        /// </summary>
        /// <returns>The converter.</returns>
        /// <typeparam name="TSource">The source type parameter.</typeparam>
        /// <typeparam name="TTarget">The target type parameter.</typeparam>
        private static Func<TSource, TTarget> GetConverter<TSource, TTarget>()
        {
            return Convert<TTarget>.IsSupportedConversionTarget ? Convert<TSource>.To<TTarget>.Converter : null;
        }

        /// <summary>
        /// Generates a simple cast converter
        /// </summary>
        /// <returns>The cast converter.</returns>
        /// <param name="sourceType">Source type.</param>
        /// <param name="targetType">Target type.</param>
        private static Delegate GenerateCastConverter(Type sourceType, Type targetType)
        {
            var parameter = Expression.Parameter(sourceType);
            return Expression.Lambda(
                Expression.Convert(parameter, targetType), parameter).Compile();
        }

        private static Delegate GenerateFromNullableConverter(Type sourceType, Type targetType, MethodInfo convert)
        {
            //  define parameter
            var parameter = Expression.Parameter(sourceType, "value");

            // define return target and label
            var returnLabel = Expression.Label(Expression.Label(targetType), Expression.Default(targetType));

            //  parameter == null
            var equalsNull = Expression.Equal(parameter, Expression.Constant(null));

            //  what do we do if its null
            Expression whenNull;
            if (targetType.IsValueType && (Nullable.GetUnderlyingType(targetType) == null))
            {
                whenNull = Expression.Throw(Expression.New(typeof(InvalidCastException)));
            }
            else
            {
                whenNull = Expression.Return(returnLabel.Target, Expression.Constant(null, targetType), targetType);
            }

            //  convert expression (reading from Nullable.value)
            var convertTo = Expression.Convert(
                Expression.Call(convert, Expression.Property(parameter, "Value")), targetType);

            //  return Convert(value.Value)
            var returnValue = Expression.Return(returnLabel.Target, convertTo);

            //  create lambda
            return Expression.Lambda(Expression.Block(Expression.IfThenElse(equalsNull, whenNull, returnValue), returnLabel), parameter).Compile();

        }

        private static Delegate GenerateToNullableConverter(Type sourceType, Type targetType, MethodInfo convert)
        {
            //  define parameter
            var parameter = Expression.Parameter(sourceType, "value");
            //  convert expression (reading from Nullable.value
            var convertTo = Expression.Convert(Expression.Call(convert, parameter), targetType);
            //  return Convert(value.Value)
            var castValue = Expression.Convert(convertTo, targetType);
            //  create lambda
            return Expression.Lambda(castValue, parameter).Compile();
        }

        private static Delegate GenerateNullableConverter(Type sourceType, Type targetType)
        {
            var underlyingSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
            var underlyingTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // both sides non-nullable?
            if (underlyingSourceType == sourceType && underlyingTargetType == targetType)
            {
                return null;
            }

            //  get standard conversion method
            var convert = GetConverter(underlyingSourceType, underlyingTargetType);
            if (convert == null)
            {
                return null;
            }

            return underlyingSourceType != sourceType ? 
                GenerateFromNullableConverter(sourceType, targetType, convert.Method) : 
                    GenerateToNullableConverter(sourceType, targetType, convert.Method);

        }

        private static Delegate GenerateConverter(Type sourceType, Type targetType)
        {
            // we are only interested in values that may get boxed/unboxed, casting will deal with all other scenarios
            if (sourceType.CanCastTo(targetType))
            {
                return GenerateCastConverter(sourceType, targetType);
            }
                
            return GenerateNullableConverter(sourceType, targetType);
        }

        private static MethodInfo BindStandardConvertMethod(Type sourceType, Type targetType)
        {
            // get conversion method name (e.g. ToInt32, ToBoolean)
            var methodName = GetStandardConvertMethodName(targetType);
            if (methodName == null)
            {
                return null;
            }

            // bind to appropriate method
            var methodInfo = typeof(Convert).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public, null, new Type[]
            {
                sourceType
            }, null);
            return methodInfo;
        }

        private static string GetStandardConvertMethodName(Type type)
        {
            switch (Type.GetTypeCode(type))
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

        private static Type ConvertibleTypeFromTypeCode(TypeCode typeCode)
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

        private static string ClassToString<T>(T value) where T:class
        {
            return value != null ? value.ToString() : null;
        }

        private static string StructToString<T>(T value) where T: struct
        {
            return value.ToString();
        }

        private static Func<TSource, TTarget> GetStandardConvertDelegate<TSource, TTarget>()
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);

            // if we can't derive the type then its not supported
            var sourceNativeType = ConvertibleTypeFromTypeCode(Type.GetTypeCode(sourceType));
            if (sourceNativeType == null || ConvertibleTypeFromTypeCode(Type.GetTypeCode(targetType)) == null)
            {
                return null;
            }

            MethodInfo method;
            if (targetType == typeof(string))
            {
                method = (sourceType.IsValueType ? StructToStringMethod : ClassToStringMethod);
                if (sourceType.IsValueType) {
                    return (Func<TSource, TTarget>)Delegate.CreateDelegate(
                        typeof(Func<, >).MakeGenericType(sourceType, targetType), 
                        method.MakeGenericMethod(sourceType));
                } 

            }

            //  bind method and create delegate
            method = BindStandardConvertMethod(sourceNativeType, targetType);
            if (method != null)
            {
                if (sourceType != sourceNativeType) // Enums 
                {
                    // we need an additional cast
                    var valueParameter = Expression.Parameter(sourceType);
                    return (Func<TSource, TTarget>)Expression.Lambda(
                        Expression.Call(method, Expression.Convert(valueParameter, sourceNativeType)
                    ), valueParameter).Compile();
                }
                return (Func<TSource, TTarget>)Delegate.CreateDelegate(typeof(Func<, >).MakeGenericType(sourceType, targetType), method);
            }   

            return null;
        }
    }
}
