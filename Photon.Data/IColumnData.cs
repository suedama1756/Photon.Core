using System;

namespace Photon.Data
{
    internal interface IColumnData
    {
        int Capacity { get; }

        void Resize(int capacity, int preserve);

        Type DataType { get; }

        T GetValue<T>(int index);

        bool SetValue<T>(int index, T value);

        bool Clear(int index);

        bool IsNull(int index);
        
        void Move(int sourceIndex, int targetIndex);

        void Subscribe(IColumnDataObserver observer);
    }

    internal interface IColumnData<TDataType> : IColumnData
    {
        TDataType GetValue(int index);

        bool SetValue(int index, TDataType value);
    }
}