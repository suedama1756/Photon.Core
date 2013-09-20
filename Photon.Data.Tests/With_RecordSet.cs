using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;

namespace Photon.Data.Tests
{
    [TestFixture]
    public class With_RecordSet
    {
        private class TestSpecification
        {
            private RecordSet _recordSet;

            public TestSpecification GivenARecordSetOfType<T1, T2, T3>()
            {
                _recordSet = new RecordSet();
                _recordSet.Columns.Add(new RecordSetColumn(typeof(T1)));
                _recordSet.Columns.Add(new RecordSetColumn(typeof(T2)));
                _recordSet.Columns.Add(new RecordSetColumn(typeof(T3)));
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

                    var record = new RecordSetRecord();
                    _recordSet.Add(record);

                    for (int index = 0; index < values.Length; index++)
                    {
                        record.SetValue(index, values[index]);
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

            public TestSpecification WhenIRemove(Func<RecordSetRecord, bool> func)
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
                .WhenIRemove(x => x.GetValue<int>(0) == 2)
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
                .WhenIRemove(x => x.GetValue<int>(0) == 2)
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
                .WhenIRemove(x => x.GetValue<int>(0) == 2)
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