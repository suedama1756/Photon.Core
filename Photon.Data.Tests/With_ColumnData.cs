using System;
using NUnit.Framework;
using System.Diagnostics;
using System.Data;

namespace Photon.Data.Tests
{
    [TestFixture]
    public class With_ColumnData
    {
         
    }

    public class ColumnDataSpecification 
    {
        private IColumnData _data;

//        public ColumnDataSpecification GivenData(Type dataType, bool requireNullable)  
//        {
//            _data = ColumnData.Create(dataType, requireNullable);
//        }
//
//        public ColumnDataSpecification ShouldHaveType<T>() 
//        {
//            Assert.Equals(_data.DataType);
//        }
    }


    [TestFixture]
    public class With_NullableColumnData 
    {
        [Test]
        public void DoSomething() 
        {
            RecordSet recordSet = new RecordSet(new Type[]
            {
                typeof(int), typeof(int?), typeof(bool), typeof(string)
            });

            var record = recordSet.Add();
            var sw = new Stopwatch();

            record.GetField<int>(0);
            record.GetField<string>(0);
            record.GetField<long>(0);
            sw.Start();
            for (var i=0; i<1000000; i++) {
                record.GetField<int>(0);
                record.GetField<string>(0);
                record.GetField<long>(0);
            }
            Console.WriteLine(sw.ElapsedMilliseconds);

            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("Test", typeof(int)));
            var row = dt.Rows.Add();
            row[0] = 1;
            var j = (int)row[0];
            var k = row[0].ToString();
            var l = (long)(int)row[0];
            sw.Restart();

            for (var i=0; i<1000000; i++) {
                 j = (int)row[0];
                 k = row[0].ToString();
                 l = (long)(int)row[0];

            }
            Console.WriteLine(sw.ElapsedMilliseconds);


//
//            Assert.AreEqual(0, record.GetField<int>(0));
//
//            recordSet.Columns.Insert(0, typeof(double));
//            record.SetField(0, 1.0);
//            Assert.AreEqual(1.0, record.GetField<double>(0));
        }


    }
}
