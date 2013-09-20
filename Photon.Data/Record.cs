using System;
using System.Linq;

namespace Photon.Data
{
    public class Record : IRecord
    {
        #region Fields

        internal int Handle = -1;
		internal RecordSet Store;

        #endregion

        public object this[int index]
		{
			get { return Field<object>(index); }
			set { Field(index, value); }
		}
		
        public T Field<T>(int index)
		{
			ThrowIfDetached();
			return Store.Field<T>(Handle, index);
		}
		
		public void Field<T>(int index, T value)
		{
			ThrowIfDetached();
			Store.Field<T>(Handle, index, value);
		}

	    public Type FieldType(int index)
	    {
	        return Store.Columns[index];
	    }

	    public int FieldCount
	    {
	        get
	        {
	            return Store.Columns.Count;
	        }
	    }

	    protected void ThrowIfDetached()
		{
			if (Store == null)
			{
				throw new InvalidOperationException("The record is not attached to any storage.");
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
