namespace DesktopApp.Data;

public interface IDataSource
{
    Task Reload();
}
public interface IDataSource<out T> : IDataSource
{
    IObservable<T> Values { get; }
}
