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
    public abstract class RowSetColumnType
    {
        protected RowSetColumnType() 
        {
        }

        public abstract T GetValue<T>(Array array, int index);
        
        public abstract bool SetValue<T>(Array array, int index, T value);
        
        public static RowSetColumnType Create(Type type)
        {
            var createType = typeof(RowSetColumnType<>).MakeGenericType(type);
            return (RowSetColumnType)Activator.CreateInstance(createType);
        }
    }

	public class RowSetColumnType<TColumn> : RowSetColumnType
	{
		private IEqualityComparer<TColumn> _equalityComparer = EqualityComparer<TColumn>.Default;

		public RowSetColumnType()
		{
        }

		public TColumn GetValue(Array array, int index)
		{
			return ((TColumn[])array)[index];
		}
		
		public override T GetValue<T>(Array array, int index)
		{
            return Converter.Convert<TColumn, T>(GetValue (array, index));
		}
		
		public bool SetValue(Array array, int index, TColumn value)
		{
			// cast array type to column type
			var typedArray = ((TColumn[])array);
			
			// modify if changed
			var oldValue = typedArray[index];
			if (!_equalityComparer.Equals(oldValue, value))
			{
				typedArray[index] = value;
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
