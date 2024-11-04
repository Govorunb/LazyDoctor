namespace DesktopApp.Data;

public abstract class DataSource<T>
{
    protected abstract string DataPath { get; }
    public abstract T Value { get; }
}
