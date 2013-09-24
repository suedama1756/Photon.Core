namespace Photon.Data
{
    public interface IRecordObserver
    {
        void Changed<T>(IRecord source, int ordinal, T oldValue, T newValue);
    }
}