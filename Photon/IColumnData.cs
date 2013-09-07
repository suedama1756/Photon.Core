using System;

namespace Photon.Data
{
    public interface IColumnData
	{
        /// <summary>
        /// Gets the data column's capacity 
        /// </summary>
        /// <value>The capacity.</value>
        int Capacity 
        {
            get;
        }

        /// <summary>
        /// Resizes the column data
        /// </summary>
        /// <param name="capacity">The new capacity.</param>
        /// <param name="count">The maximum number of items to copy from the old data.</param>
        /// <remarks>
        /// By setting count, the column data can be truncated efficiently.
        /// </remarks>
        void Resize(int capacity, int count);
        
        Type DataType
        {
            get;
        }

        T GetValue<T>(int index);

        bool SetValue<T>(int index, T value);

        bool Clear(int index);

        bool IsNull(int index);
    }

    public interface IColumnData<TDataType> : IColumnData
    {
        TDataType GetValue(int index);

        bool SetValue(int index, TDataType value);
    }
}
