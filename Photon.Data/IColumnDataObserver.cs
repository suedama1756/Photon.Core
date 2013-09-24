namespace Photon.Data
{
    internal interface IColumnDataObserver
    {
        void Changed<T>(IColumnData data, int index, T oldValue, T newValue);
    }
}