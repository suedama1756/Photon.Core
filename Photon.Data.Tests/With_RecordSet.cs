using System;
using System.Data;
using System.Diagnostics;
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

            public TestSpecification GivenARecordSetOfType(object type)
            {
                _recordSet = new RecordSet();
                foreach (var item in type.GetType().GetProperties())
                {
                    _recordSet.Columns.Add(new RecordSetColumn(item.Name, (Type)item.GetValue(type, null)));
                }
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
                        Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, "{0}", 
                            enumerator.Current[index]), values[index]);
                    }
                }

                return this;
            }

            public TestSpecification WhenIRemove(Func<RecordSetRecord, bool> func)
            {
                var items = _recordSet.Where(func).ToArray();
                foreach (var row in items)
                {
                    _recordSet.Remove(row);
                }
                return this;
            }

            public TestSpecification GivenARecordSet(object type, params string[] rows)
            {
                return GivenARecordSetOfType(type)
                    .WhenIAdd(rows);
            }

            public TestSpecification WhenIIncreaseCapacityTo(int value)
            {
                Assert.IsTrue(value > _recordSet.Capacity,
                    "Test pre-condition failed, expected recordset capacity to be higher.");
                _recordSet.Capacity = value;
                return this;
            }

            public TestSpecification WhenIDecreaseCapacityTo(int value)
            {
                Assert.IsTrue(value < _recordSet.Capacity, 
                    "Test pre-condition failed, expected recordset capacity to be higher.");
                _recordSet.Capacity = value;
                return this;
            }

            public void ShouldHaveCapacity(int value)
            {
                Assert.AreEqual(value, _recordSet.Capacity);
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
                .GivenARecordSet(new
                    {
                        Id = typeof(int),
                        Greeting = typeof(string)
                    },
                    "1|Hello",
                    "2|Goodbye",
                    "3|Farewell")
                .WhenIRemove(
                    x => x.GetValue<int>(0) == 2)
                .ShouldRead(
                    "1|Hello",
                    "3|Farewell")
                .WhenIAdd(
                    "4|Au revoir",
                    "5|Auf Wiedersehen")
                .ShouldRead(
                    "1|Hello",
                    "4|Au revoir",
                    "3|Farewell",
                    "5|Auf Wiedersehen");
        }

        [Test]
        public void Supports_increasing_capacity_with_pooled_items()
        {
            Specification
                .GivenARecordSet(new
                    {
                        Id = typeof (int),
                        Greeting = typeof(string)
                    }, 
                    "1|Goodbye",
                    "2|Au revoir",
                    "3|Auf Wiedersehen")
                .WhenIRemove(x =>
                             x.GetValue<int>(0) == 2)
                .WhenIIncreaseCapacityTo(5)
                .ShouldRead("1|Goodbye",
                            "3|Auf Wiedersehen")
                .ShouldHaveCapacity(5);
        }

        [Test]
        public void Supports_increasing_capacity()
        {
            Specification
                .GivenARecordSet(new
                    {
                        Id = typeof(int),
                        Greeting = typeof(string)
                    },
                    "1|Goodbye",
                    "2|Au revoir",
                    "3|Auf Wiedersehen",
                    "4|Dag")
                .WhenIIncreaseCapacityTo(5)
                .ShouldRead(
                    "1|Goodbye",
                    "2|Au revoir",
                    "3|Auf Wiedersehen",
                    "4|Dag")
                .ShouldHaveCapacity(5);
        }

        [Test]
        public void Supports_decreasing_compacity()
        {
            Specification
                .GivenARecordSet(
                    new
                        {
                            Id = typeof(int),
                            Greeting = typeof(string)
                        },
                    "1|Goodbye",
                    "2|Au revoir",
                    "3|Auf Wiedersehen")
                .WhenIDecreaseCapacityTo(3)
                .ShouldRead(
                    "1|Goodbye",
                    "2|Au revoir",
                    "3|Auf Wiedersehen")
                .ShouldHaveCapacity(3);
        }

        [Test]
        public void Support_compacting_when_decreasing_capacity()
        {
            Specification
                .GivenARecordSet(
                    new
                    {
                        Id = typeof(int),
                        Greeting = typeof(string)
                    },
                    "1|Goodbye",
                    "2|Au revoir",
                    "3|Auf Wiedersehen",
                    "4|Dag",
                    "5|Do svidaniya")
                .WhenIRemove(x => 
                    new[] {2, 3, 5}.Contains(x.GetValue<int>("Id")))
                .WhenIDecreaseCapacityTo(2)
                .ShouldRead(
                    "1|Goodbye",
                    "4|Dag")
                .ShouldHaveCapacity(2);
        }

        [Test]
        public void Support_better_performance()
        {
            // This test is really just an early warning system (not a very good one), 
            // we run everything twice to ensure we don't get jit timing.

            long t1 = 1;
            long t2 = 1;

            var sw = new Stopwatch();
            for (var j = 0; j < 2; j++)
            {
                sw.Restart();
            
                var recordSet = new RecordSet();
                recordSet.Columns.Add(new RecordSetColumn("A", typeof(int)));
                recordSet.Columns.Add(new RecordSetColumn("B", typeof(string)));
                recordSet.Columns.Add(new RecordSetColumn("C", typeof(double)));
                recordSet.Columns.Add(new RecordSetColumn("D", typeof(decimal)));
                for (var i = 0; i < 100000; i++)
                {
                    var record = new RecordSetRecord();
                    recordSet.Add(record);
                    record.SetValue(0, 1);
                    record.SetValue(1, 1);
                    record.SetValue(2, 1);
                    record.SetValue(3, 1);
                }
                t1 = sw.ElapsedTicks;
            }

            for (var j = 0; j < 2; j++)
            {
                sw.Restart();

                var dataTable = new DataTable();
                dataTable.Columns.Add(new DataColumn("A", typeof(int)));
                dataTable.Columns.Add(new DataColumn("B", typeof(string)));
                dataTable.Columns.Add(new DataColumn("C", typeof(double)));
                dataTable.Columns.Add(new DataColumn("D", typeof(decimal)));
                for (var i = 0; i < 100000; i++)
                {
                    var record = dataTable.Rows.Add();
                    record[0] = 1;
                    record[1] = 1;
                    record[2] = 1;
                    record[3] = 1;
                }
                t2 = sw.ElapsedTicks;
            }

            Assert.GreaterOrEqual((double)t2 / t1, 4.5);
        }
        
    }
}