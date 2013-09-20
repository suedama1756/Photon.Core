using System;
using NUnit.Framework;
using System.Diagnostics;

namespace Photon.Data
{
	[TestFixture()]
	public class With_DataRow
	{
        private class DataRowSpecification 
        {
            private RecordSet _rowSet;
            private Record _row;

            public DataRowSpecification WhenIAddAColumnOfType<TCol>(TCol value)
            {
                _rowSet.Columns.Add(typeof(TCol));
                _row.Field(_rowSet.Columns.Count - 1, value);
                return this;
            }

            public DataRowSpecification WhenIInsertAColumnOfType<TCol>(int index, TCol value)
            {
                _rowSet.Columns.Insert(index, typeof(TCol));
                _row.Field(index, value);
                return this;
            }

            public DataRowSpecification WhenIRemoveAColumn(int index) {
                _rowSet.Columns.RemoveAt(index);
                return this;
            }
            
            public void ShouldThrowInvalidOperationIfRead<T>(int index = 0) 
            {
                Assert.Throws<InvalidOperationException>(() => {
                    _row.Field<T>(index);
                });
            }

            public DataRowSpecification GivenARowOfType(params Type[] types) 
            {
                _rowSet = new RecordSet(types);
                var record = new Record();
                _rowSet.Add(record);
                return this;
            }

            public DataRowSpecification GivenARowOfType<TCol0>(TCol0 col0) 
            {
                GivenARowOfType(typeof(TCol0));
                _row.Field(0, col0);
                return this;
            }

            public DataRowSpecification GivenARowOfType<TCol0, TCol1>(TCol0 col0, TCol1 col1) 
            {
                GivenARowOfType(typeof(TCol0), typeof(TCol1));
                _row.Field(0, col0);
                _row.Field(1, col1);
                return this;
            }

            public DataRowSpecification GivenARowOfType<TCol0, TCol1, TCol2>(TCol0 col0, TCol1 col1, TCol2 col2) 
            {
                GivenARowOfType(typeof(TCol0), typeof(TCol1), typeof(TCol2));
                _row.Field(0, col0);
                _row.Field(1, col1);
                _row.Field(2, col2);
                return this;
            }


            public DataRowSpecification ShouldRead<TCol0>(TCol0 col0) 
            {
                Assert.AreEqual(col0, _row.Field<TCol0>(0));
                return this;
            }

            public DataRowSpecification ShouldRead<TCol0, TCol1>(TCol0 col0, TCol1 col1) 
            {
                ShouldRead(col0);
                Assert.AreEqual(col1, _row.Field<TCol1>(1));
                return this;
            }

            public DataRowSpecification ShouldRead<TCol0, TCol1, TCol2>(TCol0 col0, TCol1 col1, TCol2 col2) 
            {
                ShouldRead(col0, col1);
                Assert.AreEqual(col2, _row.Field<TCol2>(2));
                return this;
            }
        }

        public With_DataRow()
        {
            Specification = new DataRowSpecification();
        }

        private DataRowSpecification Specification
        {
            get;
            set;
        }

        [Test]
        public void Supports_reading() 
        {
            Specification.GivenARowOfType(1)
                .ShouldRead(1);
        }

        [Test]
        public void Supports_reading_nullables() 
        {
            Specification.GivenARowOfType((int?)1)
                .ShouldRead((int?)1);

            Specification.GivenARowOfType((int?)null)
                .ShouldRead((int?)null);
        }

        [Test]
        public void Supports_reading_nullables_from_nullables_of_a_convertible_type() 
        {
            Specification.GivenARowOfType((int?)1)
                .ShouldRead((double?)1.0);

            Specification.GivenARowOfType((int?)null)
                .ShouldRead((double?)null);
        }

        [Test]
        public void Supports_invalid_operation_detection_reading_non_nullable_from_null() 
        {
            Specification.GivenARowOfType((int?)null)
                .ShouldThrowInvalidOperationIfRead<int>();
        }

        [Test]
        public void Supports_adding_columns() 
        {
            Specification.GivenARowOfType(1)
                .WhenIAddAColumnOfType(1.1)
                .ShouldRead(1, 1.1);
        }

        [Test]
        public void Supports_no_columns() 
        {
//            Record row;
//            row.
//            Specification.GivenARowOfType()

        }

        [Test]
        public void Supports_inserting_columns() 
        {
            Specification.GivenARowOfType(1)
                .WhenIInsertAColumnOfType(0, 1.1)
                    .ShouldRead(1.1, 1)
                .WhenIInsertAColumnOfType(1, "Hello")
                    .ShouldRead(1.1, "Hello", 1);
        }

        [Test]
        public void Supports_default_population_on_insertion() 
        {
            Specification.GivenARowOfType(new Type[] { typeof(int), typeof(int?), typeof(string) })
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
}

