using System;
using System.Collections.Generic;

namespace Photon.Data
{
    internal abstract class ColumnDataBase<TDataType> : IColumnData<TDataType>
    {
        #region Fields

        private readonly IEqualityComparer<TDataType> _equalityComparer;
        private readonly bool _isNullable;
        private IColumnDataObserver[] _observers;

        #endregion

        protected ColumnDataBase(IEqualityComparer<TDataType> equalityComparer)
        {
            if (equalityComparer == null)
            {
                throw new ArgumentNullException("equalityComparer");
            }

            _equalityComparer = equalityComparer;

            _isNullable = !typeof(TDataType).IsValueType ||
                Nullable.GetUnderlyingType(typeof(TDataType)) != null;
        }

        public abstract void Resize(int capacity, int preserve);
        
        public abstract int Capacity { get; }

        protected abstract TDataType GetValueRaw(int index);

        protected abstract void SetValueRaw(int index, TDataType value);

        public Type DataType { get { return typeof(TDataType); } }

        public IEqualityComparer<TDataType> EqualityComparer
        {
            get { return _equalityComparer; }
        }

        public T GetValue<T>(int index)
        {
            return Generics.Convert<TDataType, T>(GetValueRaw(index));
        }

        public bool SetValue<T>(int index, T value)
        {
            return SetValue(index, Generics.Convert<T, TDataType>(value));
        }

        public bool Clear(int index)
        {
            return SetValue(index, default(TDataType));
        }

        public bool IsNull(int index)
        {
            return _isNullable && Generics.IsNull(GetValueRaw(index));
        }

        public void Move(int sourceIndex, int targetIndex)
        {
            SetValueRaw(targetIndex, GetValueRaw(sourceIndex));
        }

        public TDataType GetValue(int index)
        {
            return GetValueRaw(index);
        }

        public bool SetValue(int index, TDataType value)
        {
            var oldValue = GetValueRaw(index);
            if (!EqualityComparer.Equals(oldValue, value))
            {
                SetValueRaw(index, value);
                Changed(index, oldValue, value);
                return true;
            }
            return false;
        }

        public void Subscribe(IColumnDataObserver observer)
        {
            _observers = Arrays.Concat(_observers, observer);
        }

        public bool Unsubscribe(IColumnDataObserver observer)
        {
            var observers = _observers;
            _observers = Arrays.Remove(_observers, observer);
            return _observers != observers;
        }

        protected void Changed(int index, TDataType oldValue, TDataType newValue)
        {
            // take copy of observers (for correct handling of re-entrant subscribe/unsubscribe)
            var observers = _observers;
            if (observers != null)
            {
                foreach (var observer in observers)
                {
                    observer.Changed(this, index, oldValue, newValue);
                }    
            }
        }
    }
}