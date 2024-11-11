using DesktopApp.Data.Recruitment;
using OpenCvSharp;

namespace DesktopApp.Recruitment.Processing;

public sealed class TagsOCR(TagsDataSource tagSource) : OCRPipeline<ICollection<Tag>>
{
    private static readonly Dictionary<string, string> _allowedCharacters = new()
    {
        ["eng"] = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-",
    };

    protected override Mat PrepareImage(Mat image)
    {
        // crop source image to the portion of the UI that has the recruitment tags
        // to do this, we have to find the visual "signature" of the UI element
        throw new NotImplementedException();
    }

    protected override Rect[] GetRelevantRegions(Mat image)
    {
        // extract ROIs of text from the buttons
        throw new NotImplementedException();
    }

    protected override ICollection<Tag> ParseResults(OCRResult[] ocrResults)
    {
        if (ocrResults.Length == 0)
            return [];

        ReadOnlySpan<OCRResult> ocrs = ocrResults;
        if (ocrResults.Length > RecruitmentFilter.MaxTagsSelected)
        {
            this.Log().Warn($"{ocrResults.Length} results from OCR");
            ocrs = ocrs[..RecruitmentFilter.MaxTagsSelected];
        }

        var tags = new List<Tag>(RecruitmentFilter.MaxTagsSelected);
        foreach (var res in ocrs)
        {
            if (tags.Count >= RecruitmentFilter.MaxTagsSelected)
                break;

            if (tagSource.GetByName(res.FullText) is { } tag)
                tags.Add(tag);
            if (res.ComponentConfidences.FirstOrDefault(conf => conf < 80, -1) > -1)
            {
                this.Log().Warn($"OCR confidence kinda low: {res.FullText}"
                              + $" ({string.Join(',', res.ComponentTexts)})"
                              + $" ({string.Join(',', res.ComponentConfidences)})");
            }
        }

        return tags;
    }

    protected override string? GetAllowedCharacters(string lang)
        => _allowedCharacters.GetValueOrDefault(lang);
}
