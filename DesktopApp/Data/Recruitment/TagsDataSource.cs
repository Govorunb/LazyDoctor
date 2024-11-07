using System.Collections.Frozen;
using System.Reactive.Linq;
using DesktopApp.Recruitment;

namespace DesktopApp.Data.Recruitment;

public sealed class TagsDataSource : DataSource<Tag[]>
{
    public TagsDataSource(IDataSource<GachaTable> gachaTable)
    {
        gachaTable.Values
            .Select(Process)
            .Subscribe(Subject)
            .DisposeWith(this);
    }

    private static readonly FrozenSet<string> _excludedTags =
        EmbeddedTagsData.GetKnownTags()
            .Select(t => t.Name)
            .Concat(["Male", "Female"])
            .ToFrozenSet();

    private static Tag[] Process(GachaTable table)
    {
        var knownTags = EmbeddedTagsData.GetKnownTags();
        var unknownTags = table.RecruitmentTags?
            .Select(raw => raw.TagName!.Replace('-', ' '))
            .Where(name => !_excludedTags.Contains(name))
            .Select(name => new Tag(name, "Affix"))
            ?? [];
        return knownTags
            .Concat(unknownTags)
            .ToArray();
    }
}
