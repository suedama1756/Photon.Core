using System;
using System.Linq.Expressions;

namespace Photon
{
    internal static class GenericType<T>
    {
        // ReSharper disable StaticFieldInGenericType
        public static readonly bool IsSupportedConversionTarget = GetIsSupportedConversionTarget();

        public static readonly Func<T, bool> IsNull = InitializeIsNull();
        // ReSharper restore StaticFieldInGenericType

        /// <summary>
        /// Inner static type used to cache converters for generic types
        /// </summary>
        internal static class ConverterContainer<TTarget>
        {
            public static readonly Func<T, TTarget> Converter = ConverterGenerator.TryGenerate(typeof(T), typeof(TTarget)) as Func<T, TTarget>;
        }

        public static Func<T, TTarget> GetConverter<TTarget>()
        {
            return GenericType<TTarget>.IsSupportedConversionTarget ? ConverterContainer<TTarget>.Converter : null; 
        }

        private static bool GetIsSupportedConversionTarget()
        {
            return typeof(T).IsValueType ||
                   typeof(T) == typeof(string);
        }

        private static bool NonNullable(T value)
        {
            return false;
        }

        private static Func<T, bool> InitializeIsNull()
        {
            if (Nullable.GetUnderlyingType(typeof(T)) == null && typeof(T).IsValueType)
            {
                return NonNullable;
            }

            var parameter = Expression.Parameter(typeof(T));
            return Expression.Lambda<Func<T, bool>>(Expression.Equal(
                parameter, Expression.Constant(null)), parameter).Compile();
        }
    }
}