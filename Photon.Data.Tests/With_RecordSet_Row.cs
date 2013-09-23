using System;
using NUnit.Framework;

namespace Photon.Data.Tests
{
	[TestFixture()]
	public class With_RecordSet_Row
	{
        public class TestSpecification 
        {
            private RecordSet _recordSet;
            private IRecord _record;

            public TestSpecification WhenIAddAColumnOf<TColumnValue>(TColumnValue value)
            {
                _recordSet.Columns.Add(new RecordSetColumn(typeof(TColumnValue)));
                _record.SetField(_recordSet.Columns.Count - 1, value);
                return this;
            }

            public TestSpecification WhenIInsertAColumn<TColumnValue>(int index, TColumnValue value)
            {
                _recordSet.Columns.Insert(index, new RecordSetColumn(typeof(TColumnValue)));
                _record.SetField(index, value);
                return this;
            }

            public TestSpecification WhenIRemoveAColumn(int index, out RecordSetColumn removed)
            {
                removed = _recordSet.Columns[index];
                _recordSet.Columns.RemoveAt(index);
                return this;
            }

            public TestSpecification WhenIInsertAColumn(int index, RecordSetColumn column)
            {
                _recordSet.Columns.Insert(index, column);
                return this;
            }

            public TestSpecification GivenOrRowOfType(params RecordSetColumn[] columns)
            {
                _recordSet = new RecordSet();
                _recordSet.Columns.AddRange(columns);
                var record = new Record();
                _recordSet.Add(record);
                _record = record;
                return this;
            }

            public TestSpecification GivenOrRowOfType(params Type[] types) 
            {
                _recordSet = new RecordSet();
                foreach (var type in types)
                {
                    _recordSet.Columns.Add(new RecordSetColumn(type));
                }
                var record = new Record();
                _recordSet.Add(record);
                _record = record;
                return this;
            }

            public TestSpecification GivenOrRow<TCol0>(TCol0 col0) 
            {
                GivenOrRowOfType(typeof(TCol0));
                _record.SetField(0, col0);
                return this;
            }

            public TestSpecification ShouldRead<TCol0>(TCol0 col0) 
            {
                Assert.AreEqual(col0, _record.GetField<TCol0>(0));
                return this;
            }

            public TestSpecification ShouldRead<TCol0, TCol1>(TCol0 col0, TCol1 col1) 
            {
                ShouldRead(col0);
                Assert.AreEqual(col1, _record.GetField<TCol1>(1));
                return this;
            }

            public TestSpecification ShouldRead<TCol0, TCol1, TCol2>(TCol0 col0, TCol1 col1, TCol2 col2) 
            {
                ShouldRead(col0, col1);
                Assert.AreEqual(col2, _record.GetField<TCol2>(2));
                return this;
            }

            public void ShouldRead()
            {
                Assert.AreEqual(0, _record.FieldCount);
            }

            public TestSpecification WhenISetByIndex<T>(int index, T value)
            {
                _record.SetField(index, value);
                return this;
            }

            public TestSpecification WhenISetByName<T>(string name, T value)
            {
                _record.SetField(name, value);
                return this;
            }

            public void ShouldReadByName<T>(string name, T value)
            {
                Assert.AreEqual(value, _record.GetField<T>(name));
            }
            
            public TestSpecification ShouldMapColumnNameToOrdinal(int index, string name)
            {
                Assert.AreEqual(name, _record.GetName(index));
                Assert.AreEqual(index, _record.GetOrdinal(name));
                return this;
            }

            public void ShouldHaveDetachedColumn(RecordSetColumn removed)
            {
                Assert.IsNull(removed.RecordSet);
                Assert.AreEqual(-1, removed.Ordinal);
            }

            public TestSpecification ShouldHaveColumnCount(int value)
            {
                Assert.AreEqual(value, _recordSet.Columns.Count);
                return this;
            }

            public void ShouldReadIsNull(params bool[] values)
            {
                for (int index = 0; index < values.Length; index++)
                {
                    var value = values[index];
                    Assert.AreEqual(value, _record.IsNull(index));
                }
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
                .GivenOrRowOfType(new RecordSetColumn[0])
                .ShouldRead();
        }

        [Test]
        public void Supports_adding_columns()
        {
            Specification
                .GivenOrRow(1)
                .WhenIAddAColumnOf("Hello")
                .ShouldRead(1, "Hello");
        }

        [Test]
        public void Supports_inserting_columns() 
        {
            Specification
                .GivenOrRow(1)
                .WhenIInsertAColumn(0, 1.1)
                .ShouldRead(1.1, 1)
                .WhenIInsertAColumn(1, "Hello")
                .ShouldRead(1.1, "Hello", 1);
        }

        [Test]
        public void Supports_default_population_on_insertion() 
        {
            Specification.GivenOrRowOfType(new[] { typeof(int), typeof(int?), typeof(string) })
                .ShouldRead(0, (int?)null, (string)null);
        }
        
        [Test]
        public void Supports_named_columns()
        {
            Specification
                .GivenOrRowOfType(new RecordSetColumn("Greeting", typeof (string)))
                .WhenISetByName("Greeting", "Hello")
                .ShouldReadByName("Greeting", "Hello");
        }

        [Test]
        public void Supports_inserting_columns_at_beginning()
        {
            Specification
                .GivenOrRowOfType(new RecordSetColumn("Greeting", typeof (string)))
                .WhenIInsertAColumn(0, new RecordSetColumn("Language", typeof (string)))
                .ShouldHaveColumnCount(2)
                .ShouldMapColumnNameToOrdinal(0, "Language")
                .ShouldMapColumnNameToOrdinal(1, "Greeting");
        }

        [Test]
        public void Supports_inserting_columns_at_end()
        {
            Specification
                .GivenOrRowOfType(new RecordSetColumn("Greeting", typeof(string)))
                .WhenIInsertAColumn(1, new RecordSetColumn("Language", typeof(string)))
                .ShouldHaveColumnCount(2)
                .ShouldMapColumnNameToOrdinal(0, "Greeting")
                .ShouldMapColumnNameToOrdinal(1, "Language");
        }

        [Test]
        public void Supports_inserting_columns_in_middle()
        {
            Specification
                .GivenOrRowOfType(
                    new RecordSetColumn("Greeting", typeof(string)),
                    new RecordSetColumn("Language", typeof(string)))
                .WhenIInsertAColumn(1, 
                    new RecordSetColumn("IsFormal", typeof(bool)))
                .ShouldHaveColumnCount(3)
                .ShouldMapColumnNameToOrdinal(0, "Greeting")
                .ShouldMapColumnNameToOrdinal(1, "IsFormal")
                .ShouldMapColumnNameToOrdinal(2, "Language");
        }

        [Test]
        public void Supports_removing_columns_at_beginning()
        {
            RecordSetColumn removed;
            Specification
                .GivenOrRowOfType(
                    new RecordSetColumn("Greeting", typeof (string)),
                    new RecordSetColumn("Language", typeof (string))
                )
                .WhenIRemoveAColumn(0, out removed)
                .ShouldHaveColumnCount(1)
                .ShouldMapColumnNameToOrdinal(0, "Language")
                .ShouldHaveDetachedColumn(removed);
        }

        [Test]
        public void Supports_removing_columns_at_end()
        {
            RecordSetColumn removed;
            Specification
                .GivenOrRowOfType(
                    new RecordSetColumn("Greeting", typeof(string)),
                    new RecordSetColumn("Language", typeof(string))
                )
                .WhenIRemoveAColumn(1, out removed)
                .ShouldHaveColumnCount(1)
                .ShouldMapColumnNameToOrdinal(0, "Greeting")
                .ShouldHaveDetachedColumn(removed);
        }

        [Test]
        public void Supports_removing_columns_in_middle()
        {
            RecordSetColumn removed;
            Specification
                .GivenOrRowOfType(
                    new RecordSetColumn("Greeting", typeof(string)),
                    new RecordSetColumn("IsFormal", typeof(bool)),
                    new RecordSetColumn("Language", typeof(string))
                )
                .WhenIRemoveAColumn(1, out removed)
                .ShouldHaveColumnCount(2)
                .ShouldMapColumnNameToOrdinal(0, "Greeting")
                .ShouldMapColumnNameToOrdinal(1, "Language")
                .ShouldHaveDetachedColumn(removed);
        }

        [Test]
        public void Supports_nullables()
        {
            Specification
                .GivenOrRow<int?>(1)
                .ShouldRead<int?>(1)
                .WhenISetByIndex<int?>(0, null)
                .ShouldRead<int?>(null)
                .ShouldReadIsNull(true);
        }
	}
}

