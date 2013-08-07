using System;
using System.Linq;

namespace Photon.Data
{
	public class Record
	{
		internal int Handle;
		internal RecordSet RecordSet;
		
		public object this[int index]
		{
			get { return GetField<object>(index); }
			set { SetField(index, value); }
		}
		
        public T GetField<T>(int index)
		{
			CheckDisposed();
			return RecordSet.GetField<T>(Handle, index);
		}
		
		public void SetField<T>(int index, T value)
		{
			CheckDisposed();
			RecordSet.SetField<T>(Handle, index, value);
		}
		
		protected void CheckDisposed()
		{
			if (RecordSet == null)
			{
				throw new ObjectDisposedException(typeof(Record).Name);
			}
		}
		
		public override string ToString()
		{
            if (RecordSet == null)
            {
                return string.Empty;
            }
            return "[" + string.Join(", ", RecordSet.Columns.Select((x, i) => GetField<string>(i))) + "]";
		}
	}

}
