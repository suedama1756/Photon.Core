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
	
	public class Row
	{
		internal int Handle;
		internal RowSet RowSet;
		
		public object this[int index]
		{
			get { return GetField<object>(index); }
			set { SetField(index, value); }
		}
		
		public T GetField<T>(int index)
		{
			CheckDisposed();
			return RowSet.GetField<T>(Handle, index);
		}
		
		public void SetField<T>(int index, T value)
		{
			CheckDisposed();
			RowSet.SetField<T>(Handle, index, value);
		}
		
		private void CheckDisposed()
		{
			if (RowSet == null)
			{
				throw new ObjectDisposedException(typeof(Row).Name);
			}
		}
		
		//public override string ToString()
		//{
		//    return "[" + string.Join(RowSet.)
		//}
	}

}
