namespace Photon.Data
{
    public interface IRecordObserver
    {
        void Changed<T>(IRecord obj, int index, T oldValue, T newValue);
    }
}