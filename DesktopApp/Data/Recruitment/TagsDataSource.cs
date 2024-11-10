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
    private readonly SourceList<Tag> _tags = new();
    public IObservableList<Tag> Tags => _tags.AsObservableList();
    private readonly ObservableCollectionExtended<TagCategory> _categories = [];
    public ReadOnlyObservableCollection<TagCategory> Categories { get; }

    private static readonly FrozenSet<string> _excludedTags =
        EmbeddedTagsData.GetKnownTags()
            .Select(t => t.Name)
            .Concat(["Male", "Female"])
            .ToFrozenSet();

    public TagsDataSource(IDataSource<GachaTable> gachaTable)
    {
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

    private static Tag[] Process(GachaTable table)
    {
        var unknownTags = table.RecruitmentTags?
            .Select(raw => raw.TagName!)
            .Where(name => !_excludedTags.Contains(name))
            .Select(name => new Tag(name, "Affix"))
            ?? [];
        return EmbeddedTagsData.GetKnownTags()
            .Concat(unknownTags)
            .ToArray();
    }
}
