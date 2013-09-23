namespace Photon.Data
{
    internal interface IColumnDataObserver
    {
        void Changed<T>(IColumnData data, T oldValue, T newValue);
    }
}