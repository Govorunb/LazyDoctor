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

        // not needed apparently
        return image;
    }

    // todo: doesn't deal with background dots on senior/top op very well
    // also, selected tags get binarized to the opposite side of unselected ones
    protected override Rect[] GetRelevantRegions(Mat image)
    {
        // extract regions of interest (ROIs)
        // we're looking for rectangles that correspond to the tag buttons
        // methodology and code adapted from https://redrainkim.github.io/opencvsharp/opencvsharp-study-12/#

        using var blurred = image.GaussianBlur(new Size(5, 5), 0);
        using var threshold = blurred.Threshold(60, 255, ThresholdTypes.BinaryInv);

        Cv2.FindContours(threshold, out var contours, out _,
            RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        List<Rect> rects = [];
        foreach (var contour in contours)
        {
            if (!IsRectangle(contour, out var poly))
                continue;
            rects.Add(Cv2.BoundingRect(poly));
        }
#if DEBUG && DEBUG_OPENCV
        using var highlight = image.CvtColor(ColorConversionCodes.GRAY2RGB, 3);
        foreach (var rect in rects)
            Cv2.Rectangle(highlight, rect, Scalar.Blue, 2);
        try
        {
            using var window = highlight.ShowWindow();
            using var windBlurred = blurred.ShowWindow();
            using var windThresh = threshold.ShowWindow();
            Cv2.WaitKey();
        } catch (OpenCVException) { } // throws only when exiting the whole app
#endif
        // todo: filter out small rects and rects that don't match the pattern
        // the buttons are arranged as follows
        //   [    ] [    ] [    ]
        //   [    ] [    ]
        return rects.ToArray();
    }

    private static bool IsRectangle(Point[] vertices, out Point[] poly)
    {
        var peri = Cv2.ArcLength(vertices, true);
        poly = Cv2.ApproxPolyDP(vertices, 0.04 * peri, true);
        return poly.Length == 4;
    }

    protected override ICollection<Tag> ParseResults(OCRResult[]? ocrResults)
    {
        if (ocrResults is not { Length: >0 })
            return [];

        ReadOnlySpan<OCRResult> ocrs = ocrResults;
        if (ocrResults.Length > RecruitmentFilter.MaxTagsSelected)
        {
            this.Log().Warn($"{ocrResults.Length} results from OCR, should be at most {RecruitmentFilter.MaxTagsSelected}");
            // ocrs = ocrs[..RecruitmentFilter.MaxTagsSelected];
        }

        var tags = new List<Tag>(RecruitmentFilter.MaxTagsSelected);
        foreach (var res in ocrs)
        {
            if (tags.Count >= RecruitmentFilter.MaxTagsSelected)
                break;
            var text = res.FullText.Trim();
            if (string.IsNullOrEmpty(text))
                continue;

            if (tagSource.GetByName(text) is { } tag)
            {
                tags.Add(tag);
            }
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
