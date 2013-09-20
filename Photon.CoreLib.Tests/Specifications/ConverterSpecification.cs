using System;
using NUnit.Framework;

namespace Photon.Tests.Specifications
{
    public class ConverterSpecification
    {
        public ConverterSpecification ShouldConvert<TSource, TTarget>(TSource source, TTarget target)
        {
            Assert.AreEqual(target, Generics.Convert<TSource, TTarget>(source));
            return this;
        }

        public ConverterSpecification ShouldThrow<TSource, TTarget, TException>(TSource source)
        {
            try
            {
                Generics.Convert<TSource, TTarget>(source);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is TException, "Expected exception {0}, actual: {1}", 
                    typeof(TException).Name, e.GetType().Name);
                return this;
            }
            
            Assert.Fail("Expected exception type {0}.", typeof(TException).Name);
            return this;
        }
    }
}
