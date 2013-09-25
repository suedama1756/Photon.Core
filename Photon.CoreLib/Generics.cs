using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Photon
{
    public static class Generics
    {
        private static readonly ConcurrentDictionary<DynamicConversionKey, Delegate> BoxedTypeMap = new ConcurrentDictionary<DynamicConversionKey,
            Delegate>();
        private static readonly MethodInfo CastAndConvertMethod = typeof(Generics)
            .GetMethod("CastAndConvert", BindingFlags.Static | BindingFlags.NonPublic);

        private struct DynamicConversionKey : IEquatable<DynamicConversionKey>
        {
            private readonly Type _from;
            private readonly Type _to;

            public DynamicConversionKey(Type from, Type to)
            {
                _from = from;
                _to = to;
            }

            public bool Equals(DynamicConversionKey other)
            {
                if (ReferenceEquals(other, this))
                {
                    return true;
                }
                
                if (ReferenceEquals(other, null))
                {
                    return false;
                }
                
                return _from == other._from && _to == other._to;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is DynamicConversionKey))
                {
                    return false;
                }
                return Equals((DynamicConversionKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((_from != null ? _from.GetHashCode() : 0)*397) ^ (_to != null ? _to.GetHashCode() : 0);
                }
            }
        }

        public static TTarget Convert<TSource, TTarget>(TSource source)
        {
            var converter = GenericType<TSource>.GetConverter<TTarget>();
            if (converter != null)
            {
                return converter(source);
            }

            if (!IsNull(source)) 
            {
                var sourceType = source.GetType();
                if (sourceType != typeof(TSource) && sourceType.IsValueType)
                {
                    Delegate convertDelegate;
                    var key = new DynamicConversionKey(sourceType, typeof(TTarget));
                    if (!BoxedTypeMap.TryGetValue(key, out convertDelegate))
                    {
                        convertDelegate = Delegate.CreateDelegate(typeof(Func<object, TTarget>),
                            CastAndConvertMethod.MakeGenericMethod(sourceType, typeof(TTarget)));
                        BoxedTypeMap.TryAdd(key, convertDelegate);
                    }
                    return ((Func<object, TTarget>)convertDelegate)(source);
                }
            }

            return (TTarget)(object)source;
        }

        public static bool IsNull<T>(T value) 
        {
            return GenericType<T>.IsNull(value);
        }

        // ReSharper disable UnusedMember.Local

        private static TTarget CastAndConvert<TSource, TTarget>(object o)
        {
            var converter = GenericType<TSource>.GetConverter<TTarget>();
            if (converter != null)
            {
                return converter((TSource)o);
            }
            return (TTarget)o;
        }

        // ReSharper restore UnusedMember.Local
    }
}
