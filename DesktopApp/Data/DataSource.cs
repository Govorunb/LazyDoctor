using System.Reactive.Subjects;

namespace DesktopApp.Data;
public class DataSource<T> : ReactiveObjectBase, IDataSource<T>
{
    protected ReplaySubject<T> Subject { get; } = new(1);
    public IObservable<T> Values => Subject;

    protected override void DisposeCore()
    {
        Subject.Dispose();
        base.DisposeCore();
    }
}
