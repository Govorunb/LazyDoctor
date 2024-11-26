using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DesktopApp.Recruitment;
using DesktopApp.Utilities.Helpers;
using DynamicData;
using DynamicData.Binding;

namespace DesktopApp.Data.Recruitment;

public sealed class TagsDataSource : DataSource<Tag[]>
{
    private readonly IDataSource<GachaTable> _gachaTable;

    private static readonly FrozenSet<string> _excludedTags =
        EmbeddedTagsData.GetKnownTags()
            .Select(t => t.Name)
            .Concat(["Male", "Female"])
            .ToFrozenSet();

    private readonly SourceList<Tag> _tags = new();
    public IObservableList<Tag> Tags => _tags.AsObservableList();

    private Dictionary<string, Tag> _byName = [];

    private readonly ObservableCollectionExtended<TagCategory> _categories = [];
    public ReadOnlyObservableCollection<TagCategory> Categories { get; }

    internal List<Tag> UnknownTags { get; } = [];

    public TagsDataSource(IDataSource<GachaTable> gachaTable)
    {
        _gachaTable = gachaTable;
        Categories = new(_categories);

        gachaTable.Values
            .Select(Process)
            .Subscribe(Subject)
            .DisposeWith(this);

        Subject.Subscribe(v => _tags.EditDiff(v));

        _tags.Connect()
            .GroupOnProperty(tag => tag.Category)
            .Transform(group => new TagCategory(group.GroupKey, group.List.Items))
            .SortByDescending(c => c.Name)
            .OnMainThread()
            .Bind(_categories)
            .Subscribe();
    }

    private Tag[] Process(GachaTable table)
    {
        UnknownTags.Clear();
        UnknownTags.AddRange(
            table.RecruitmentTags?
                .Select(raw => raw.TagName!)
                .Where(name => !_excludedTags.Contains(name))
                .Select(name => new Tag(name, "Affix"))
            ?? []
        );

        var allTags = EmbeddedTagsData.GetKnownTags()
            .Concat(UnknownTags)
            .ToArray();

        _byName = allTags.ToDictionary(t => t.Name);

        return allTags;
    }

    public Tag? GetByName(string name) => _byName.GetValueOrDefault(name);
    public override Task Reload() => _gachaTable.Reload();
}
