using System.Reactive.Disposables;
using DesktopApp.Utilities.Helpers;
using JetBrains.Annotations;
using OpenCvSharp;
using OpenCvSharp.Text;

namespace DesktopApp.Common;

[PublicAPI]
public record struct OCRResult(string FullText, Rect[] ComponentRects, string?[] ComponentTexts, float[] ComponentConfidences);

public abstract class OCRPipeline<TResult> : ReactiveObjectBase
{
    private readonly Dictionary<string, OCRTesseract> _ocrs = [];

    protected virtual Mat? ParseImage(ReadOnlySpan<byte> pngData)
    {
        return Cv2.ImDecode(pngData, ImreadModes.Grayscale);
    }
    protected abstract Mat? PrepareImage(Mat image);
    protected abstract Rect[]? GetRelevantRegions(Mat image);
    protected abstract TResult ParseResults(OCRResult[]? ocrResults);

    public virtual TResult Process(ReadOnlySpan<byte> pngData, string lang = "eng")
    {
        using var image = ParseImage(pngData);
        if (image is null) return ParseResults(null);

        using var processed = PrepareImage(image);
        if (processed is null) return ParseResults(null);

        var regions = GetRelevantRegions(processed);
        if (regions is null) return ParseResults(null);

        var results = regions.Select(region =>
        {
            using var roi = processed.SubMat(region);
            return PerformOCR(roi, lang);
        }).ToArray();
        return ParseResults(results);
    }

    protected OCRResult PerformOCR(Mat image, string lang = "eng", ComponentLevels componentLevels = ComponentLevels.Word)
    {
        var ocr = GetCachedOCR(lang);
        ocr.Run(image, out var text, out var rects, out var compTexts, out var compConfs, componentLevels);

#if DEBUG && DEBUG_OPENCV
        if (rects.Length > 0)
        {
            using var highlight = image.CvtColor(ColorConversionCodes.GRAY2RGB, 3);
            for (var i = 0; i < rects.Length; i++)
            {
                var color = compConfs[i] switch
                {
                    < 50 => Scalar.Red,
                    < 80 => Scalar.Yellow,
                    _ => Scalar.Green,
                };
                Cv2.Rectangle(highlight, rects[i], color, 2);
            }

            try
            {
                using var window = highlight.ShowWindow("OCR Result");
                Cv2.WaitKey();
            } catch (OpenCVException) { } // throws only when exiting the whole app
        }
#endif
        return new(text, rects, compTexts, compConfs);
    }

    private OCRTesseract GetCachedOCR(string lang)
        => _ocrs.GetOrAdd(lang, () => CreateOCR(lang));

    private OCRTesseract CreateOCR(string lang) =>
        OCRTesseract.Create("data/tessdata/", lang, GetAllowedCharacters(lang))
            .DisposeWith(Disposables);

    protected abstract string? GetAllowedCharacters(string lang);

    protected override void DisposeCore()
    {
        foreach (var ocr in _ocrs.Values.ToList())
        {
            ocr.Dispose();
        }
        _ocrs.Clear();

        base.DisposeCore();
    }
}
