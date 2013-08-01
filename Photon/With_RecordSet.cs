using System;
using NUnit.Framework;
using System.Diagnostics;

namespace Photon.Data
{

    [TestFixture]
    public class With_Convert 
    {
        [Test]
        public void Should_convert_nullable_to_non_nullable_of_same_underlying_type() 
        {
            Assert.AreEqual((int?)1, Converter.Convert<int?, int>(1));
        }
    }

	[TestFixture()]
	public class With_RecordSet
	{
        private class RowSpecification 
        {
            public void ShouldThrowIfReadAs<T>(int index = 0) 
            {
                Assert.Throws<InvalidCastException>(() => {
                    _row.GetField<T>(index);
                });
            }

            private Record _row;

            public RowSpecification GivenARowOfType(params Type[] types) 
            {
                var rowSet = new RecordSet(types);
                _row = rowSet.Add();
                return this;
            }

            public RowSpecification GivenARowOfType<T1>(T1 col0) 
            {
                GivenARowOfType(typeof(T1));
                _row.SetField(0, col0);
                return this;
            }

            public void ShouldRead<T1>(T1 col0) 
            {
                Assert.AreEqual(col0, _row.GetField<T1>(0));
            }
        }

        public With_RecordSet()
        {
            Specification = new RowSpecification();
        }

        private RowSpecification Specification
        {
            get;
            set;
        }

        [Test]
        public void Should_read_write_with_no_conversion() 
        {
            Specification.GivenARowOfType(1)
                .ShouldRead(1);
        }

        [Test]
        public void Should_read_write_nullable_to_nullable_of_same_type() 
        {
            Specification.GivenARowOfType((int?)1)
                .ShouldRead((int?)1);

            Specification.GivenARowOfType((int?)null)
                .ShouldRead((int?)null);
        }

        [Test]
        public void Should_read_write_nullable_to_nullable_of_convertible_type() 
        {
            Specification.GivenARowOfType((int?)1)
                .ShouldRead((double?)1.0);

            Specification.GivenARowOfType((int?)null)
                .ShouldRead((double?)null);
        }

        [Test]
        public void Should_throw_if_null_to_non_nullable_of_same_underlying_type() 
        {
            Specification.GivenARowOfType((int?)null)
                .ShouldThrowIfReadAs<int>();
        }



		[Test()]
		public void TestCase()
		{
			var rowSet = new RecordSet(new Type[] {
				typeof(String),
				typeof(int),
				typeof(double)
			});

			var row = rowSet.Add();
			row.SetField(0, "1");
			row.SetField(1, 1);
			row.SetField(2, 1.2);
			row.GetField<int>(0);
            row.GetField<int>(1);
            row.GetField<int>(2);

			RunTest(row);
		}

	    private void RunTest(Record row)
	    {
	        Assert.IsTrue(TimeIt("ReadWithCast", () =>
	            {
	                for (var i = 0; i < 100000; i++)
	                {
	                    var result = row.GetField<int>(0)
	                                 + row.GetField<int>(1)
	                                 + row.GetField<int>(2);
	                    if (result != 3)
	                    {
	                        return false;
	                    }
	                }
	                return true;
	            }));
	    }

	    public void TimeIt(string name, Action action)
		{
			var sw = new Stopwatch();
			sw.Start();
			action();
			sw.Stop();
			Console.WriteLine("Action {0} took: {1}ms", name, sw.ElapsedMilliseconds);
		}
		
		public T TimeIt<T>(string name, Func<T> action)
		{
			T result = default(T);
			TimeIt(name, () =>
			       {
				result = action();
			});
			return result;
		}
	}
	
	
	
	
	


}

