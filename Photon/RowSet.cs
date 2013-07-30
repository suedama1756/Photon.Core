using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq.Expressions;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Photon.Data
{
	
	public class RowSet
	{
		private List<Row> rows = new List<Row>();
		private int size;

		private class RowSetColumn
		{
			public Type Type { get; private set; }
			
			public RowSetColumnType ColumnType
			{
				get;
				set;
			}
			
			public RowSetColumn(Type type)
			{
				Type = type;
				ColumnType = RowSetColumnType.Create(type);
			}
			
			public Array Data;
		}
		
		public RowSet(params Type[] types)
		{
			Columns = types.Select(x => new RowSetColumn(x)).ToArray();
		}
		
		private RowSetColumn[] Columns { get; set; }
		
		public Row Add()
		{
			if (rows.Count == size)
			{
				for (var i = 0; i < Columns.Length; i++)
				{
					var column = Columns[i];
					
					var newData = Array.CreateInstance(column.Type, size + 16);
					size += 16;
					
					var oldData = column.Data;
					if (oldData != null)
					{
						Array.Copy(oldData, newData, oldData.Length);
					}
					column.Data = newData;
				}
			}
			
			var result = new Row
			{
				Handle = rows.Count,
				RowSet = this
			};
			rows.Add(result);
			return result;
		}
		
		public T GetField<T>(int handle, int index)
		{
			var column = Columns[index];
			return column.ColumnType.GetValue<T>(column.Data, handle);
		}
		
		public bool SetField<T>(int handle, int index, T value)
		{
			var column = Columns[index];
			return column.ColumnType.SetValue<T>(column.Data, handle, value);
		}
	}
}
