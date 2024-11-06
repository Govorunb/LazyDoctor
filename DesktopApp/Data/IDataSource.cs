namespace DesktopApp.Data;

public interface IDataSource<T>
{
    IObservable<T> Values { get; }
}
