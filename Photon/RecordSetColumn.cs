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
    public abstract class RecordSetColumn
    {
        public abstract void SetCapacity(int newSize);
        
        public RecordSetColumn()
        {
        }

        public abstract Type Type
        {
            get;
        }

        public abstract T GetValue<T>(Array array, int index);

        public abstract bool SetValue<T>(Array array, int index, T value);

        public static RecordSetColumn Create(Type type)
        {
            var createType = typeof(RecordSetColumn<>).MakeGenericType(type);
            return (RecordSetColumn)Activator.CreateInstance(createType);
        }
    }
        
    public class RecordSetColumn<TColumn> : RecordSetColumn
	{
		private IEqualityComparer<TColumn> _equalityComparer = EqualityComparer<TColumn>.Default;
        private TColumn[] _data;

        public RecordSetColumn()
		{
        }

        public override Type Type
        {
            get
            {
                return typeof(TColumn);
            }
        }

        public override void SetCapacity(int newSize)
        {
            var newData = new TColumn[newSize];
            var oldData = _data;

            if (oldData != null)
            {
                Array.Copy(oldData, newData, oldData.Length);
            }
            _data = newData;
        }

		public TColumn GetValue(Array array, int index)
		{
			return _data[index];
		}
		
		public override T GetValue<T>(Array array, int index)
		{
            return Converter.Convert<TColumn, T>(_data[index]);
		}
		
		public bool SetValue(Array array, int index, TColumn value)
		{
			// modify if changed
			var oldValue = _data[index];
			if (!_equalityComparer.Equals(oldValue, value))
			{
				_data[index] = value;
				return true;
			}
			return false;
		}
		
		public override bool SetValue<T>(Array array, int index, T value)
		{
            return SetValue (array, index, Converter.Convert<T, TColumn>(value));
		}
	}
}
