using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;

namespace Photon.Data.Tests
{
	[TestFixture()]
	public class With_RecordSet_Row
	{
        private class TestSpecification 
        {
            private RecordSet _recordSet;
            private Record _record;

            public TestSpecification WhenIAddAColumnOfType<TCol>(TCol value)
            {
                _recordSet.Columns.Add(typeof(TCol));
                _record.Field(_recordSet.Columns.Count - 1, value);
                return this;
            }

            public TestSpecification WhenIInsertAColumnOfType<TCol>(int index, TCol value)
            {
                _recordSet.Columns.Insert(index, typeof(TCol));
                _record.Field(index, value);
                return this;
            }

            public TestSpecification WhenIRemoveAColumn(int index) {
                _recordSet.Columns.RemoveAt(index);
                return this;
            }
            
            public void ShouldThrowInvalidOperationIfRead<T>(int index = 0) 
            {
                Assert.Throws<InvalidOperationException>(() => _record.Field<T>(index));
            }

            public TestSpecification GivenARowOfType(params Type[] types) 
            {
                _recordSet = new RecordSet(types);
                _record = new Record();
                _recordSet.Add(_record);
                return this;
            }

            public TestSpecification GivenARowOfType<TCol0>(TCol0 col0) 
            {
                GivenARowOfType(typeof(TCol0));
                _record.Field(0, col0);
                return this;
            }

            public TestSpecification GivenARowOfType<TCol0, TCol1>(TCol0 col0, TCol1 col1) 
            {
                GivenARowOfType(typeof(TCol0), typeof(TCol1));
                _record.Field(0, col0);
                _record.Field(1, col1);
                return this;
            }

            public TestSpecification GivenARowOfType<TCol0, TCol1, TCol2>(TCol0 col0, TCol1 col1, TCol2 col2) 
            {
                GivenARowOfType(typeof(TCol0), typeof(TCol1), typeof(TCol2));
                _record.Field(0, col0);
                _record.Field(1, col1);
                _record.Field(2, col2);
                return this;
            }
            
            public TestSpecification ShouldRead<TCol0>(TCol0 col0) 
            {
                Assert.AreEqual(col0, _record.Field<TCol0>(0));
                return this;
            }

            public TestSpecification ShouldRead<TCol0, TCol1>(TCol0 col0, TCol1 col1) 
            {
                ShouldRead(col0);
                Assert.AreEqual(col1, _record.Field<TCol1>(1));
                return this;
            }

            public TestSpecification ShouldRead<TCol0, TCol1, TCol2>(TCol0 col0, TCol1 col1, TCol2 col2) 
            {
                ShouldRead(col0, col1);
                Assert.AreEqual(col2, _record.Field<TCol2>(2));
                return this;
            }

            public void ShouldRead()
            {
                Assert.AreEqual(0, _record.FieldCount);
            }
        }

        private TestSpecification Specification
        {
            get;
            set;
        }
        
        [SetUp]
        public void Setup()
        {
            Specification = new TestSpecification();
        }

        [Test]
        public void Supports_no_columns()
        {
            Specification
                .GivenARowOfType()
                .ShouldRead();
        }

        [Test]
        public void Supports_adding_columns()
        {
            Specification
                .GivenARowOfType(1)
                .WhenIAddAColumnOfType("Hello")
                .ShouldRead(1, "Hello");
        }

        [Test]
        public void Supports_inserting_columns() 
        {
            Specification
                .GivenARowOfType(1)
                .WhenIInsertAColumnOfType(0, 1.1)
                .ShouldRead(1.1, 1)
                .WhenIInsertAColumnOfType(1, "Hello")
                .ShouldRead(1.1, "Hello", 1);
        }

        [Test]
        public void Supports_default_population_on_insertion() 
        {
            Specification.GivenARowOfType(new[] { typeof(int), typeof(int?), typeof(string) })
                .ShouldRead(0, (int?)null, (string)null);
        }

        [Test]
        public void Supports_removing_columns() 
        {
            Specification
                .GivenARowOfType(1, "Hello", 2.2)
                .ShouldRead(1, "Hello", 2.2)
                .WhenIRemoveAColumn(1)
                .ShouldRead(1, 2.2)
                .WhenIRemoveAColumn(1)
                .ShouldRead(1);
        }
	}

    [TestFixture]
    public class With_RecordSet
    {
        private class TestSpecification
        {
            private RecordSet _recordSet;

            public TestSpecification GivenARecordSetOfType<T1, T2, T3>()
            {
                _recordSet = new RecordSet(typeof(T1), typeof(T2), typeof(T3));
                return this;
            }

            public TestSpecification ShouldByEmpty()
            {
                Assert.AreEqual(0, _recordSet.Count);
                Assert.IsFalse(_recordSet.Any());
                return this;
            }

            public TestSpecification WhenIAdd(params string[] rows)
            {
                foreach (var row in rows)
                {
                    var values = row.Split('|');

                    var record = new Record();
                    _recordSet.Add(record);

                    for (int index = 0; index < values.Length; index++)
                    {
                        record.Field(index, values[index]);
                    }    
                }

                return this;
            }

            public TestSpecification ShouldRead(params string[] rows)
            {
                var enumerator = _recordSet.GetEnumerator();
                foreach (var row in rows)
                {
                    var values = row.Split('|');
                    
                    if (!enumerator.MoveNext())
                    {
                        Assert.Fail("Row count mismatch");
                    }

                    for (int index = 0; index < values.Length; index++)
                    {
                        Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, "{0}", enumerator.Current[index]), values[index]);
                    }
                }

                return this;
            }

            public TestSpecification WhenIRemove(Func<Record, bool> func)
            {
                var row = _recordSet.First(func);
                _recordSet.Remove(row);
                return this;
            }
        }

        [SetUp]
        public void TestSetup()
        {
            Specification = new TestSpecification();
        }

        private TestSpecification Specification { get; set; }

        [Test]
        public void Supports_initialization_to_empty()
        {
            Specification
                .GivenARecordSetOfType<int, string, double>()
                .ShouldByEmpty();
        }

        [Test]
        public void Supports_adding()
        {
            Specification
                .GivenARecordSetOfType<int, string, double>()
                .WhenIAdd("1|Hello|1.1")
                .ShouldRead("1|Hello|1.1");
        }

        [Test]
        public void Supports_adding_many()
        {
            Specification
                .GivenARecordSetOfType<int, string, double>()
                .WhenIAdd(
                    "1|Hello|1.1",
                    "2|Goodbye|1.3")
                .ShouldRead(
                    "1|Hello|1.1",
                    "2|Goodbye|1.3");
        }

        [Test]
        public void Supports_removing()
        {
            Specification
                .GivenARecordSetOfType<int, string, double>()
                .WhenIAdd(
                    "1|Hello|1.1",
                    "2|Goodbye|1.2",
                    "3|Farewell|1.3")
                .WhenIRemove(x => x.Field<int>(0) == 2)
                .ShouldRead(
                    "1|Hello|1.1",
                    "3|Farewell|1.3");
        }

        [Test]
        public void Supports_recycling()
        {
            Specification
                .GivenARecordSetOfType<int, string, double>()
                .WhenIAdd(
                    "1|Hello|1.1",
                    "2|Goodbye|1.2",
                    "3|Farewell|1.3")
                .WhenIRemove(x => x.Field<int>(0) == 2)
                .ShouldRead(
                    "1|Hello|1.1",
                    "3|Farewell|1.3")
                .WhenIAdd("4|Au revoir|1.4")
                .ShouldRead(
                    "1|Hello|1.1",
                    "4|Au revoir|1.4",
                    "3|Farewell|1.3");
        }

        [Test]
        public void Supports_appending_after_pool_exhausted()
        {
            Specification
                .GivenARecordSetOfType<int, string, double>()
                .WhenIAdd(
                    "1|Hello|1.1",
                    "2|Goodbye|1.2",
                    "3|Farewell|1.3")
                .WhenIRemove(x => x.Field<int>(0) == 2)
                .ShouldRead(
                    "1|Hello|1.1",
                    "3|Farewell|1.3")
                .WhenIAdd(
                    "4|Au revoir|1.4",
                    "5|Auf Wiedersehen|1.5")
                .ShouldRead(
                    "1|Hello|1.1",
                    "4|Au revoir|1.4",
                    "3|Farewell|1.3",
                    "5|Auf Wiedersehen|1.5");
        }
    }
}

