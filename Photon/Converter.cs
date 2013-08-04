using System;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using System.Globalization;

namespace Photon.Data
{
    public class Converter 
    {
        private static readonly Delegate NullDelegate = (Action)(() => {});
        private static readonly MethodInfo GetMethod = typeof(Converter).GetMethods(
            BindingFlags.NonPublic | BindingFlags.Static).First(x => x.Name == "Get" && x.IsGenericMethodDefinition);
        
        private static class ConvertType<TSource>
        {
            /// <summary>
            /// Strangely, evaluating Type.IsValueType is slow (it walks up the hierarchy), so we cache the value
            /// </summary>
            internal static readonly bool IsWorthGeneratingAConverter = GetIsWorthConvertingTo();

            static bool GetIsWorthConvertingTo()
            {
                return typeof(TSource).IsValueType || 
                    typeof(TSource) == typeof(string);
            }

            public static class Cache<TTarget>
            {
                private static readonly Delegate ConverterInstance = InitializeInstance();

                private static Delegate InitializeInstance()
                {
                    return StandardConvert.GetConvertDelegate<TSource, TTarget>() ??
                                 GenerateConverter(typeof (TSource), typeof (TTarget)) ??
                                 NullDelegate;
                }

                public static Func<TSource, TTarget>  Converter
                {
                    get { return ConverterInstance as Func<TSource, TTarget>; }
                }
            }
        }

        public static TTarget Convert<TSource, TTarget>(TSource source) 
        {
            if (ConvertType<TTarget>.IsWorthGeneratingAConverter)
            {
                var converter = ConvertType<TSource>.Cache<TTarget>.Converter;
                if (converter != null)
                {
                    return converter(source);
                }
            }

            return (TTarget)(object)source;
//            var convertible = source as IConvertible;
//            if (convertible != null)
//            {
//                return (TTarget)convertible.ToType(typeof(TTarget), CultureInfo.CurrentCulture);
//            }
//            return (TTarget)(object)source;
        }

        private static Delegate Get(Type sourceType, Type targetType) 
        {
            return (Delegate)GetMethod.MakeGenericMethod(
                sourceType, targetType).Invoke(null, new object[0]);
        }

        private static Func<TIn, TOut> Get<TIn, TOut>()
        {
            return ConvertType<TOut>.IsWorthGeneratingAConverter ? ConvertType<TIn>.Cache<TOut>.Converter : null;
        }

        private static Delegate GenerateCastConverter(Type sourceType, Type targetType)
        {
            var parameter = Expression.Parameter(sourceType);
            return Expression.Lambda(Expression.Convert(parameter, targetType), parameter).Compile();
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

            //  conver expression (reading from Nullable.value
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
            var convert = Get(underlyingSourceType, underlyingTargetType);
            if (convert == null)
            {
                return null;
            }
            if (underlyingSourceType != sourceType)
            {
                return GenerateFromNullableConverter(sourceType, targetType, convert.Method);
            }

            return GenerateToNullableConverter(sourceType, targetType, convert.Method);
        }

        private static Delegate GenerateConverter(Type sourceType, Type targetType)
        {
            // we are only interested in values that may get boxed/unboxed, casting will deal with all other scenarios
            if (CanCast(sourceType, targetType)) 
            {
                return GenerateCastConverter(sourceType, targetType);
            }
                
            return GenerateNullableConverter(sourceType, targetType);
        }

        private static bool CanCast(Type sourceType, Type targetType) 
        {
            if (targetType.IsAssignableFrom(sourceType)) 
            {
                return true;
            }

            var result = HasCastOperator(sourceType, sourceType, targetType) ||
                HasCastOperator(targetType, sourceType, targetType);
            return result;
        }

        private static bool HasCastOperator(IReflect type, Type sourceType, Type targetType) 
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(x => IsCastOperatorMethod(x, sourceType, targetType));
        }

        private static bool IsCastOperatorMethod(MethodInfo method, Type sourceType, Type targetType) 
        {
            var result = (method.Name == "op_Implicit" || method.Name == "op_Explicit") &&
                method.ReturnType == targetType;
            if (result) 
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == sourceType;
            }
            return false;       
        }
    }


	
	// public class Data
	/*
     * So given a (col, row) we have an index into some storage, we don't know what type of storage the cell holds so we must look up by key
     * 
     */

}
