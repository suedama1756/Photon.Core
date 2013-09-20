using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Photon.Reflection;

namespace Photon
{
    internal class ConverterGenerator
    {
        private static readonly MethodInfo ClassToStringMethod = typeof(ConverterGenerator).GetMethod("ClassToString", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo StructToStringMethod = typeof(ConverterGenerator).GetMethod("StructToString", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo EnumTryParseMethod = typeof(Enum).GetMethods(BindingFlags.Static | BindingFlags.Public).
                                                     First(x =>
                                                     {
                                                         if (x.Name == "TryParse" && x.IsGenericMethodDefinition)
                                                         {
                                                             var parameters = x.GetParameters();
                                                             return parameters.Length == 2 &&
                                                                    parameters[0].ParameterType == typeof(string) &&
                                                                    x.ReturnType == typeof(bool);
                                                         }
                                                         return false;
                                                     });
        
        /// <summary>
        /// Generates a converter that casts the output of another converter. This is usefull when converting to 
        /// nullable types, e.g. (int?)ConvertToInt(1.0); where ConvertToInt is hte intermediate conversion.
        /// </summary>
        /// <remarks>
        /// When the target type is not nullable
        /// <code>
        /// (TSource source) => return {
        ///     if (source == null) {
        ///         throw new InvalidCaseException();
        ///     }
        ///     return convert(source.Value);
        /// };
        /// </code>
        /// When the target type is  nullable
        /// <code>
        /// (TSource source) => return {
        ///     if (source == null) {
        ///        return null;
        ///     }
        ///     return (TTarget)convert(source.Value);
        /// };
        /// </code>
        /// </remarks>
        /// <param name="sourceType">The source type.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="intermediateConversion">The converter to invoke.</param>
        /// <returns>A delegate that can be used to convert from the source type to the target type.</returns>
        private static Delegate GenerateIntermediateConversionFromNullable(Type sourceType, Type targetType, Delegate intermediateConversion)
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
                Expression.Invoke(Expression.Constant(intermediateConversion), Expression.Property(parameter, "Value")), targetType);

            //  return Convert(value.Value)
            var returnValue = Expression.Return(returnLabel.Target, convertTo);

            //  create lambda
            return Expression.Lambda(Expression.Block(Expression.IfThenElse(equalsNull, whenNull, returnValue), returnLabel), parameter).Compile();

        }

        /// <summary>
        /// Generates a converter that casts the output of another converter. This is usefull when converting to 
        /// nullable types, e.g. (int?)ConvertToInt(1.0); where ConvertToInt is hte intermediate conversion.
        /// </summary>
        /// <remarks>
        /// <code>
        /// (TSource source) => return (TTarget)intermediateConversion(source);
        /// </code>
        /// </remarks>
        /// <param name="sourceType">The source type.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="intermediateConversion">The intermediate converter method to invoke.</param>
        /// <returns>A delegate that can be used to convert from the source type to the target type.</returns>
        private static Delegate GenerateIntermediateConversionWithCast(Type sourceType, Type targetType, Delegate intermediateConversion)
        {
            var parameter = Expression.Parameter(sourceType, "value");

            var convertTo = Expression.Convert(
                Expression.Invoke(Expression.Constant(intermediateConversion), parameter), targetType);

            return Expression.Lambda(convertTo, parameter).Compile();
        }

        /// <summary>
        /// Generates a simple cast converter
        /// </summary>
        /// <returns>The cast converter.</returns>
        /// <remarks>
        /// <code>
        /// (TSource source) => (TTarget)source;
        /// </code>
        /// </remarks>
        /// <param name="sourceType">Source type.</param>
        /// <param name="targetType">Target type.</param>
        private static Delegate TryGenerateCastConverter(Type sourceType, Type targetType)
        {
            if (sourceType.CanCastTo(targetType))
            {
                var parameter = Expression.Parameter(sourceType);
                return Expression.Lambda(
                    Expression.Convert(parameter, targetType), parameter).Compile();
            }

            return null;
        }

        /// <summary>
        /// Generates a converter 
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="targetType">The target type.</param>
        /// <returns>If the types are compatible with the conversion strategy a <see cref="Delegate"/> that can be used to convert from 
        /// the source type to the target type; otherwise, a null reference.</returns>
        private static Delegate TryGenerateNullableConverter(Type sourceType, Type targetType)
        {
            var underlyingSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
            var underlyingTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // both sides non-nullable?
            if (underlyingSourceType == sourceType && underlyingTargetType == targetType)
            {
                return null;
            }

            //  get standard conversion method
            var converter = TryGenerate(underlyingSourceType, underlyingTargetType);
            if (converter == null)
            {
                return null;
            }

            return underlyingSourceType != sourceType ?
                       GenerateIntermediateConversionFromNullable(sourceType, targetType, converter) :
                       GenerateIntermediateConversionWithCast(sourceType, targetType, converter);
        }

        private static Delegate TryGenerateConvertibleConverter(Type sourceType, Type targetType)
        {
            // if we can't derive the type then its not supported
            var sourceNativeType = GetConvertibleTypeFromTypeCode(Type.GetTypeCode(sourceType));
            if ((sourceNativeType == null || GetConvertibleTypeFromTypeCode(Type.GetTypeCode(targetType)) == null))
            {
                return null;
            }
            
            //  bind method and create delegate
            var method = GetConvertibleTargetMethod(sourceNativeType, targetType);
            if (method != null)
            {
                if (sourceType != sourceNativeType) // Enums 
                {
                    // let toString pick this one up; otherwise we just get the oridinal value as string
                    if (targetType == typeof (string))
                    {
                        return null;
                    }

                    // we need an additional cast
                    var valueParameter = Expression.Parameter(sourceType);
                    return Expression.Lambda(
                        Expression.Call(method, 
                            Expression.Convert(valueParameter, sourceNativeType)), valueParameter).Compile();
                }

                return Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(sourceType, targetType), method);
            }

            return null;
        }

        private static Delegate TryGenerateToString(Type sourceType, Type targetType)
        {
            if (targetType != typeof (string))
            {
                return null;
            }
            
            var method = (sourceType.IsValueType ? StructToStringMethod : ClassToStringMethod);
            return Delegate.CreateDelegate(typeof (Func<,>).MakeGenericType(sourceType, targetType),
                method.MakeGenericMethod(sourceType));
        }

        private static Delegate TryGenerateParse(Type sourceType, Type targetType)
        {
            //  source type must be string
            if (sourceType != typeof (string))
            {
                return null;
            }

            // try to locate a suitable parse method
            var parseMethod = targetType.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new[] {sourceType}, null);
            if (parseMethod == null || parseMethod.ReturnType != targetType)
            {
                return null;
            }

            var valueParameter = Expression.Parameter(typeof(string));
            return Expression.Lambda(Expression.Call(parseMethod, valueParameter),
                valueParameter).Compile();
        }

        private static Delegate TryGenerateTryParse(Type sourceType, Type targetType)
        {
            // source type must be a string
            if (sourceType != typeof (string))
            {
                return null;
            }

            MethodInfo tryParseMethod;
            if (targetType.IsEnum)
            {
                tryParseMethod = EnumTryParseMethod.MakeGenericMethod(targetType);
            }
            else
            {
                tryParseMethod = targetType.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, null, new[] {sourceType, targetType.MakeByRefType()}, null);
                if (tryParseMethod == null || tryParseMethod.ReturnType != typeof(bool))
                {
                    return null;
                }
            }
            
            // compile converter
            var valueParameter = Expression.Parameter(typeof (string));

            var returnLabel = Expression.Label(Expression.Label(targetType), Expression.Default(targetType));

            var resultVariable = Expression.Variable(targetType);

            var tryParseCall = Expression.Call(tryParseMethod, valueParameter, resultVariable);

            var ifParsed = Expression.IfThen(tryParseCall,
                Expression.Return(returnLabel.Target, resultVariable, targetType));

            var ifNotParsed = Expression.Throw(Expression.New(typeof (InvalidCastException)));

            return Expression.Lambda(Expression.Block(new[] {resultVariable}, ifParsed, ifNotParsed, returnLabel),
                valueParameter).Compile();
        }
        
        private static MethodInfo GetConvertibleTargetMethod(Type sourceType, Type targetType)
        {
            // get conversion method name (e.g. ToInt32, ToBoolean)
            var methodName = GetConvertibleTargetMethodName(targetType);
            if (methodName == null)
            {
                return null;
            }

            // bind to appropriate method
            var methodInfo = typeof(Convert).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public, null, new[]
                {
                    sourceType
                }, null);
            return methodInfo;
        }

        private static string GetConvertibleTargetMethodName(Type targetType)
        {
            switch (Type.GetTypeCode(targetType))
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

        private static Type GetConvertibleTypeFromTypeCode(TypeCode typeCode)
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

 // ReSharper disable UnusedMember.Local

        /// <summary>
        /// Converts a reference type to string.
        /// </summary>
        /// <remarks>
        /// Accessed using reflection <see cref="ClassToStringMethod"/>
        /// </remarks>
        /// <typeparam name="T">The type of the value to convert.</typeparam>
        /// <param name="value">The value to convert.</param>
        /// <returns>The value converted to a string.</returns>
        private static string ClassToString<T>(T value) where T : class
        {
            return value != null ? value.ToString() : null;
        }

        /// <summary>
        /// Converts a value type to string.
        /// </summary>
        /// <remarks>
        /// Accessed using reflection <see cref="StructToStringMethod"/>
        /// </remarks>
        /// <typeparam name="T">The type of the value to convert.</typeparam>
        /// <param name="value">The value to convert.</param>
        /// <returns>The value converted to a string.</returns>
        private static string StructToString<T>(T value) where T : struct
        {
            return value.ToString();
        }

// ReSharper restore UnusedMember.Local
        
        public static Delegate TryGenerate(Type sourceType, Type targetType)
        {
            // unfortunetly order is important
            return TryGenerateCastConverter(sourceType, targetType) ??
                   TryGenerateTryParse(sourceType, targetType) ??
                   TryGenerateParse(sourceType, targetType) ??
                   TryGenerateConvertibleConverter(sourceType, targetType) ??
                   TryGenerateNullableConverter(sourceType, targetType) ??
                   TryGenerateToString(sourceType, targetType);
        }
    }
}