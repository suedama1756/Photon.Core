using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Photon
{
    public static class Generics
    {
        private static readonly ConcurrentDictionary<Type, Delegate> BoxedTypeMap = new ConcurrentDictionary<Type, Delegate>();
        private static readonly MethodInfo CastAndConvertMethod = typeof(Generics)
            .GetMethod("CastAndConvert", BindingFlags.Static | BindingFlags.NonPublic);

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
                    if (!BoxedTypeMap.TryGetValue(sourceType, out convertDelegate))
                    {
                        convertDelegate = Delegate.CreateDelegate(typeof(Func<TSource, TTarget>),
                            CastAndConvertMethod.MakeGenericMethod(source.GetType(), typeof(TTarget)));
                        BoxedTypeMap.TryAdd(sourceType, convertDelegate);
                    }
                    return ((Func<TSource, TTarget>)convertDelegate)(source);
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
