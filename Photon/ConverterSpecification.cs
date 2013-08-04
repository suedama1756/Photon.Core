using System;
using NUnit.Framework;
using Photon.Data;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

namespace Photon
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
                return new ConversionResult<TTarget>(Converter.Convert<T, TTarget>(_value));
            }
        }

        private abstract class ConversionResult 
        {
            public abstract void AssertEquals<TValue>(TValue value);
        }

        private class ConversionResult<T> : ConversionResult
        {
            private T _value;

            public ConversionResult(T value)
            {
                this._value = value;
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
    }
    
}
