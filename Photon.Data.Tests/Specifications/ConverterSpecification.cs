using System;
using NUnit.Framework;
using Photon.Data;

namespace Photon.Data.Tests
{
    public class ConverterSpecification : ContextBoundObject
    {
        private ConversionResult _result;
        private Conversion _conversion;

        private abstract class Conversion 
        {
            public abstract ConversionResult<TTarget> To<TTarget>();
        }

        private class Conversion<T> : Conversion
        {
            private T _value;

            public Conversion(T value) 
            {
                _value = value;
            }

            public override ConversionResult<TTarget> To<TTarget>() 
            {
                try
                {
                    return new ConversionResult<TTarget>(Converter.Convert<T, TTarget>(_value));
                }
                catch (Exception e)
                {
                    return new ConversionResult<TTarget>(e);
                }

            }
        }

        private abstract class ConversionResult 
        {
            public abstract void AssertThrows<TException>() where TException : Exception;
            
            public abstract void AssertEquals<TValue>(TValue value);
        }

        private class ConversionResult<T> : ConversionResult
        {
            private T _value;
            private Exception _exception;

            public ConversionResult(Exception exception)
            {
                this._exception = exception;
            }


            public ConversionResult(T value)
            {
                this._value = value;
            }

            public override void AssertThrows<TException>()
            {
                Assert.IsTrue(_exception != null && _exception is TException, "Expected exception {0}, actual: {1}", 
                              typeof(TException).Name, _exception != null ? _exception.GetType().Name : "null");
            }

            public override void AssertEquals<TValue>(TValue value)
            {
                Assert.AreEqual(value, _value);
            }
        }

        public ConverterSpecification GivenAValue<T>(T value) 
        {
            _conversion = new Conversion<T>(value);
            return this;
        }

        public ConverterSpecification WhenIConvertTo<TTarget>() 
        {
            _result = _conversion.To<TTarget>();
            return this;
        }

        public ConverterSpecification ShouldReturn<TTarget>(TTarget value) 
        {
            _result.AssertEquals(value);
            return this;
        }

        public ConverterSpecification ShouldThrow<T>() where T:Exception
        {
            _result.AssertThrows<T>();
            return this;
        }
    }
    
}
