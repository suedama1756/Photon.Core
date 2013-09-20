using System;
using System.Diagnostics;
using System.Linq;

namespace Photon.Data
{

	public class Record
	{
		internal int Handle;
		internal RecordSet Store;
		
		public object this[int index]
		{
			get { return Field<object>(index); }
			set { Field(index, value); }
		}
		
        public T Field<T>(int index)
		{
			ThrowIfDeleted();
			return Store.Field<T>(Handle, index);
		}
		
		public void Field<T>(int index, T value)
		{
			ThrowIfDeleted();
			Store.Field<T>(Handle, index, value);
		}
		
		protected void ThrowIfDeleted()
		{
			if (Store == null)
			{
				throw new ObjectDisposedException(typeof(Record).Name);
			}
		}

		public override string ToString()
		{
            if (Store == null)
            {
                return string.Empty;
            }
            return "[" + string.Join(", ", Store.Columns.Select((x, i) => Field<string>(i))) + "]";
		}
	}
}
