using DesktopApp.Common.OCR;
using OpenCvSharp;
using Rect = Avalonia.Rect;

namespace DesktopApp.Recruitment.Processing;

public sealed class OpenCvTagsOCR : OpenCvSharpOCR, IRecruitTagsOCR
{
    protected override Rect[]? GetRelevantRegions(Mat? image)
    {
        // to-do: filter out small rects + layout
        var regions = base.GetRelevantRegions(image);
        return regions;
    }

    protected override string? GetAllowedCharacters(string lang)
    {
        return TextParsingUtils.GetAllowedCharactersInTags(lang);
    }
}
