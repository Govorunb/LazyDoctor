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

    protected virtual Mat ParseImage(ReadOnlySpan<byte> pngData)
    {
        return Cv2.ImDecode(pngData, ImreadModes.Grayscale);
    }
    protected abstract Mat PrepareImage(Mat image);
    protected abstract Rect[] GetRelevantRegions(Mat image);
    protected abstract TResult ParseResults(OCRResult[] ocrResults);

    public virtual TResult Process(ReadOnlySpan<byte> pngData, string lang = "eng")
    {
        // there's no feeling in the world quite like writing python in C#
        using var image = ParseImage(pngData);
        /*
        var sizeMulti = 150.0 / image.Height;
        Console.WriteLine($"size: {image.Size()}; multi: {sizeMulti}");
        using var resized = image.Resize(default, sizeMulti, sizeMulti, InterpolationFlags.Cubic);
        using var edges = resized.Threshold(0, 255, ThresholdTypes.Otsu);
        using var letters = edges.MorphologyEx(MorphTypes.Close, null);

        var used = (~resized).ToMat();
        */
        using var processed = PrepareImage(image);
        var regions = GetRelevantRegions(processed);
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

#if DEBUG
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
            using var window = new Window("OCR Result", highlight);
            Cv2.WaitKey();
        } catch (OpenCVException) { } // throws only when exiting the whole app
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
