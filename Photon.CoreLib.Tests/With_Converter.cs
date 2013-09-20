using NUnit.Framework;
using System;
using Photon.Tests.Specifications;

namespace Photon.Tests
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

        public struct CanParseType : IEquatable<CanParseType>
        {
            public CanParseType(int value) : this()
            {
                Value = value;
            }

            public static CanParseType Parse(string value)
            {
                return new CanParseType(Int32.Parse(value));
            }

            public int Value { get; private set; }
            
            public bool Equals(CanParseType other)
            {
                return this.Value == other.Value;
            }
        }

        public struct CanTryParseType : IEquatable<CanParseType>
        {
            public CanTryParseType(int value)
                : this()
            {
                Value = value;
            }

            public static bool TryParse(string value, out CanTryParseType output)
            {
                int i;
                if (Int32.TryParse(value, out i))
                {
                    output = new CanTryParseType(i);
                    return true;
                }
                output = default(CanTryParseType);
                return false;
            }

            public int Value { get; private set; }

            public bool Equals(CanParseType other)
            {
                return this.Value == other.Value;
            }
        }
        
        public struct CanCastToIntType
        {
            public int Value;

            public static implicit operator int(CanCastToIntType value)
            {
                return  value.Value;
            }

            public static implicit operator CanCastToIntType(int value)
            {
                return new CanCastToIntType() {Value = value};
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
        public void Can_convert_null_from_nullable_convertible_to_string()
        {
            Specification.ShouldConvert<int?, string>(null, null);
        }

        [Test]
        public void Can_convert_non_null_from_nullable_convertible_to_string() 
        {
            Specification.ShouldConvert<int?, string>(1, "1");
        }

        [Test]
        public void Can_convert_non_null_from_string_to_convertible() 
        {
            Specification.ShouldConvert("1", 1);
        }

        [Test]
        public void Can_convert_non_null_from_string_to_nullable_convertible() 
        {
            Specification.ShouldConvert<string, int?>("1", 1);            
        }

        [Test]
        public void Can_convert_from_value_type_to_string() 
        {
            Specification.ShouldConvert(1, "1");
        }

        [Test]
        public void Can_convert_null_from_class_to_string() 
        {
            Specification.ShouldConvert<object, string>(null, null);
        }

        [Test]
        public void Can_convert_non_null_from_nullable_type_to_underlying_type() 
        {
            Specification.ShouldConvert<int, int?>(1, 1);
        }

        [Test]
        public void Can_convert_non_null_from_nullable_to_non_nullable_convertible() 
        {
            Specification.ShouldConvert<double?, int>(1.0, 1);
        }

        [Test]
        public void Can_convert_enum_to_underlying_type() 
        {
            Specification.ShouldConvert(Numbers.One, 1);
        }

        [Test]
        public void Can_convert_enum_to_string()
        {
            Specification.ShouldConvert(Numbers.One, "One");
        }

        [Test]
        public void Can_convert_enum_to_type_compatible_with_underlying_type() 
        {
            Specification.ShouldConvert(Numbers.One, 1L);
        }

        [Test]
        public void Can_convert_from_struct_with_cast_operator_overload()
        {
            Specification.ShouldConvert(new CanCastToIntType {Value = 1}, 1);
        }

        [Test]
        public void Can_convert_to_struct_with_cast_operator_overload() 
        {
            Specification.ShouldConvert(1, new CanCastToIntType { Value = 1});
        }

        [Test]
        public void Can_convert_castable_class()
        {
            var obj = new DescendentType();
            Specification.ShouldConvert(obj, (BaseType)obj);
        }

        [Test]
        public void Can_convert_null_reference_type_to_nullable()
        {
            Specification.ShouldConvert<object, int?>(null, null);
        }

        [Test]
        public void Can_convert_boxed_type_to_compatible_type()
        {
            Specification.ShouldConvert<object, double>(1, 1.0);
        }

        [Test]
        public void Can_convert_null_string_to_string()
        {
            Specification.ShouldConvert<string, string>(null, null);
        }

        [Test]
        public void Can_convert_string_to_enum()
        {
            Specification.ShouldConvert("One", Numbers.One);
        }

        [Test]
        public void Can_convert_parsable()
        {
            Specification.ShouldConvert("1", new CanParseType(1));
        }

        [Test]
        public void Can_convert_try_parsable()
        {
            Specification.ShouldConvert("1", new CanTryParseType(1));
        }

        [Test]
        public void Throws_converting_null_from_nullable_to_non_nullable() 
        {
            Specification.ShouldThrow<int?, int, InvalidOperationException>(null);
        }

        [Test]
        public void Can_convert_class_to_string()
        {
            Specification.ShouldConvert(new object(), "System.Object");
        }

        [Test]
        public void Throws_if_overflow_detected()
        {
            Specification.ShouldThrow<Int64, Int16, OverflowException>(Int64.MaxValue);
        }

        [Test]
        public void Throws_converting_incompatible_convertible_types()
        {
            Specification.ShouldThrow<DateTime, short, InvalidCastException>(DateTime.Now);
        }

        [Test]
        public void Can_convert_convertibles()
        {
            Specification.ShouldConvert(1, (byte) 1);
            Specification.ShouldConvert(1, (sbyte)1);
            Specification.ShouldConvert(1, (short)1);
            Specification.ShouldConvert(1, (ushort)1);
            Specification.ShouldConvert(1L, 1);
            Specification.ShouldConvert(1L, (uint)1);
            Specification.ShouldConvert(1, 1L);
            Specification.ShouldConvert(1, (ulong)1);
            Specification.ShouldConvert(1, 1.0);
            Specification.ShouldConvert(1, (float)1.0);
            Specification.ShouldConvert(1, true);
            Specification.ShouldConvert(1, (char)1);
            Specification.ShouldConvert(1, (decimal)1);
        }
    }
}

