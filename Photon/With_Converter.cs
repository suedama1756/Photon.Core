using System;
using NUnit.Framework;
using Photon.Data;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

namespace Photon
{

    [TestFixture]
    public class With_Converter
    {
        public With_Converter()
        {
            Specification = new ConverterSpecification();
        }

        public ConverterSpecification Specification
        {
            get;
            set;
        }

        [Test]
        public void Should_convert_nullable_null_to_string() 
        {
            Specification.GivenAValue((int?)null)
                .WhenIConvertTo<string>()
                    .ShouldReturn((string)null);
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
                    .ShouldReturn((int?)1);
        }

        [Test]
        public void Should_convert_primitive_to_nullable_equivalent() 
        {
            Specification.GivenAValue(1)
                .WhenIConvertTo<int?>()
                    .ShouldReturn((int?)1);

        }

        [Test]
        public void Should_convert_convertible_to_string() 
        {
            Specification.GivenAValue(1)
                .WhenIConvertTo<string>()
                    .ShouldReturn("1");
        }

        [Test]
        public void Should_convert_nullable_convertible_to_string() 
        {
            Specification.GivenAValue((int?)1)
                .WhenIConvertTo<string>()
                    .ShouldReturn("1");

            Specification.GivenAValue((int?)null)
                .WhenIConvertTo<string>()
                    .ShouldReturn((string)null);
        }

        [Test]
        public void Should_convert_nullable_convertible_to_nullable_convertible_of_different_supported_type() 
        {
            Specification.GivenAValue((double?)1.1)
                .WhenIConvertTo<int>()
                    .ShouldReturn(1);
        }

        public enum TestEnum 
        {
            None = 0,
            One = 1,
            Two = 2,
            Three = 3
        }

        public struct CanCastToInt
        {
            public int Value;

            public static implicit operator int(CanCastToInt value)  // implicit digit to byte conversion operator
            {
                return  value.Value;
            }

            public static implicit operator CanCastToInt(int value)  // implicit digit to byte conversion operator
            {
                return  new CanCastToInt() {Value = value};
            }
        }

        [Test]
        public void Should_convert_enum_to_int() 
        {
            Specification.GivenAValue(TestEnum.One)
                .WhenIConvertTo<int>()
                .ShouldReturn(1);
        }

        [Test]
        public void Should_convert_enum_to_string() 
        {
            Specification.GivenAValue(TestEnum.One)
                .WhenIConvertTo<string>()
                    .ShouldReturn("One");
        }

        [Test]
        public void Should_convert_enum_to_long() 
        {
            Specification.GivenAValue(TestEnum.One)
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
        public void Should_convert_int_to_struct_via_cast_overload() 
        {
            Specification.GivenAValue(1)
                .WhenIConvertTo<CanCastToInt>()
                    .ShouldReturn(new CanCastToInt() { Value = 1});
        }
    }
}

