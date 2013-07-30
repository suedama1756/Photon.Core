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
using System.Collections.Concurrent;

namespace Photon.Data
{
    public class Converter 
    {
        private static Delegate NullDelegate = (Action)(() => {});
        private static MethodInfo GetMethod = typeof(Converter).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).First(
            x => x.Name == "Get" && x.IsGenericMethodDefinition);

        private static class ConverterCache<TIn, TOut> 
        {
            public static Delegate Instance;
        }

        public static TTarget Convert<TSource, TTarget>(TSource source) 
        {
            var converter = Get<TSource, TTarget>();
            if (converter != null)
            {
                return converter(source);
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
            if (!typeof(TOut).IsValueType)
            {
                return null;
            }

            var result = ConverterCache<TIn, TOut>.Instance; 
            if (result == null)
            {
                result = StandardConvert.GetConvertDelegate<TIn, TOut>() ?? 
                    GenerateConverter(typeof(TIn), typeof(TOut));
                ConverterCache<TIn, TOut>.Instance = result ?? NullDelegate;
            }

            return result as Func<TIn, TOut>;
        }

        private static Delegate GenerateCastConverter(Type sourceType, Type targetType)
        {
            var parameter = Expression.Parameter(sourceType);
            return Expression.Lambda(Expression.Convert(parameter, targetType), parameter).Compile();
        }

        private static Delegate GenerateConvertFromNullable(Type sourceType, Type targetType, MethodInfo convert)
        {
            var underlyingSourceType = Nullable.GetUnderlyingType(sourceType);

            //  define parameter
            var parameter = Expression.Parameter(sourceType, "value");

            // define return target and label
            var returnLabel = Expression.Label(Expression.Label(targetType), Expression.Default(targetType));

            //  parameter == null
            var equalsNull = Expression.Equal(parameter, Expression.Constant(null));

            //  what do we do if its null
            Expression whenNull;
            if (Nullable.GetUnderlyingType(targetType) == null)
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

        static Delegate GenerateToNullableConverter(Type sourceType, Type targetType, MethodInfo convert)
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
            var convert = Converter.Get(underlyingSourceType, underlyingTargetType);
            if (convert == null)
            {
                return null;
            }
            if (underlyingSourceType != sourceType)
            {
                return GenerateConvertFromNullable(sourceType, targetType, convert.Method);
            }

            return GenerateToNullableConverter(sourceType, targetType, convert.Method);




        }

        private static Delegate GenerateConverter(Type sourceType, Type targetType)
        {
            // we are only interested in values that may get boxed/unboxed, casting will deal with all other scenarios
            if (!(sourceType.IsValueType && targetType.IsValueType))
            {
                return null;
            }

            Delegate result = null;

            if (targetType.IsAssignableFrom(sourceType))
            {
                return GenerateCastConverter(sourceType, targetType);
            }

            return GenerateNullableConverter(sourceType, targetType);
        }
         
    }


	
	// public class Data
	/*
     * So given a (col, row) we have an index into some storage, we don't know what type of storage the cell holds so we must look up by key
     * 
     */

}
