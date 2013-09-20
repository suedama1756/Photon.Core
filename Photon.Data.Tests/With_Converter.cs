using NUnit.Framework;
using Photon.Data.Tests;
using System;

namespace Photon.Data.Tests
{
    [TestFixture]
    public class With_Converter
    {
        public enum Numbers 
        {
            None = 0,
            One = 1,
            Two = 2,
            Three = 3
        }

        public struct CanCastToInt
        {
            public int Value;

            public static implicit operator int(CanCastToInt value)
            {
                return  value.Value;
            }

            public static implicit operator CanCastToInt(int value)
            {
                return new CanCastToInt() {Value = value};
            }
        }

        public class BaseType 
        {
        }

        public class DescendentType : BaseType 
        {
        }

        public ConverterSpecification Specification
        {
            get;
            private set;
        }

        [SetUp]
        public void Setup() 
        {
            Specification = new ConverterSpecification();
        }

        [Test]
        public void Should_convert_nullable_null_to_string() 
        {
            Specification.GivenAValue<int?>(null)
                .WhenIConvertTo<string>()
                    .ShouldReturn<string>(null);
        }

        [Test]
        public void Should_convert_nullable_convertible_to_string() 
        {
            Specification.GivenAValue<int?>(1)
                .WhenIConvertTo<string>()
                    .ShouldReturn("1");
        }

        [Test]
        public void Should_convert_string_to_convertible() 
        {
            Specification.GivenAValue("1")
                .WhenIConvertTo<int>()
                    .ShouldReturn(1);
        }

        [Test]
        public void Should_convert_string_to_nullable_convertible() 
        {
            Specification.GivenAValue("1")
                .WhenIConvertTo<int?>()
                    .ShouldReturn<int?>(1);
        }

        [Test]
        public void Should_convert_convertible_to_string() 
        {
            Specification.GivenAValue(1)
                .WhenIConvertTo<string>()
                    .ShouldReturn("1");
        }

        [Test]
        public void Should_convert_null_to_string() 
        {
            Specification.GivenAValue<object>(null)
                .WhenIConvertTo<string>()
                    .ShouldReturn<string>(null);
        }

        [Test]
        public void Should_convert_convertible_to_nullable_equivalent() 
        {
            Specification.GivenAValue(1)
                .WhenIConvertTo<int?>()
                    .ShouldReturn((int?)1);
        }

        [Test]
        public void Should_convert_nullable_convertible_to_nullable_convertible_of_different_supported_type() 
        {
            Specification.GivenAValue((double?)1.1)
                .WhenIConvertTo<int>()
                    .ShouldReturn(1);
        }

        [Test]
        public void Should_convert_enum_to_underlying_type() 
        {
            Specification.GivenAValue(Numbers.One)
                .WhenIConvertTo<int>()
                .ShouldReturn(1);
        }

        [Test]
        public void Should_convert_enum_to_string() 
        {
            Specification.GivenAValue(Numbers.One)
                .WhenIConvertTo<string>()
                    .ShouldReturn("One");

        }

        [Test]
        public void Should_convert_enum_to_type_convertible_from_underlying_type() 
        {
            Specification.GivenAValue(Numbers.One)
                .WhenIConvertTo<long>()
                    .ShouldReturn(1L);
        }

        [Test]
        public void Should_convert_struct_to_type_via_cast_overload() 
        {
            Specification.GivenAValue(new CanCastToInt() { Value = 1} )
                .WhenIConvertTo<int>()
                    .ShouldReturn(1);
        }

        [Test]
        public void Should_convert_type_to_struct_via_cast_overload() 
        {
            Specification.GivenAValue(1)
                .WhenIConvertTo<CanCastToInt>()
                    .ShouldReturn(new CanCastToInt() { Value = 1});
        }

        [Test]
        public void Should_throw_converting_nullable_null_to_non_nullable_convertible() 
        {
            Specification.GivenAValue<int?>(null)
                .WhenIConvertTo<int>()
                    .ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void Should_throw_converting_overflow() 
        {

            Specification.GivenAValue<long>(Int64.MaxValue)
                .WhenIConvertTo<short>()
                    .ShouldThrow<OverflowException>();
        }

        [Test]
        public void Should_throw_converting_incompatible_convertible_types() 
        {
            Specification.GivenAValue<DateTime>(DateTime.Now)
                .WhenIConvertTo<short>()
                    .ShouldThrow<InvalidCastException>();
        }


        [Test]
        public void Should_convert_castable_class() 
        {
            var obj = new DescendentType();
            Specification.GivenAValue(obj)
                .WhenIConvertTo<BaseType>()
                    .ShouldReturn(obj);
        }

        [Test]
        public void Should_convert_object_null_to_nullable() 
        {
            Specification.GivenAValue<object>(null)
                .WhenIConvertTo<int?>()
                    .ShouldReturn<int?>(null);
        }

        [Test]
        public void Should_convert_object_to_convertible() 
        {
            Specification.GivenAValue<object>(1)
                .WhenIConvertTo<double>()
                    .ShouldReturn(1.0);
        }

        [Test]
        public void Should_convert_null_string_to_string() 
        {
            Specification.GivenAValue<string>(null)
                .WhenIConvertTo<string>()
                    .ShouldReturn<string>(null);
        }

    }
}

