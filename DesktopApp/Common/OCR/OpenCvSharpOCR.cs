using System.Reactive.Disposables;
using DesktopApp.Utilities.Helpers;
using JetBrains.Annotations;
using OpenCvSharp;
using OpenCvSharp.Text;
using Rect = Avalonia.Rect;

namespace DesktopApp.Common.OCR;

[PublicAPI]
public abstract class OpenCvSharpOCR : ReactiveObjectBase, IOCR<string>
{
    private readonly Dictionary<string, OCRTesseract> _ocrs = [];

    protected virtual Mat? ParseImage(Stream? imageData)
    {
        if (imageData is null) return null;
        ReadOnlySpan<byte> data;
        if (imageData is MemoryStream ms)
        {
            data = ms.TryGetBuffer(out var segment)
                ? segment.AsSpan()
                : ms.ToArray().AsSpan();
        }
        else
        {
            var arr = new byte[imageData.Length];
            imageData.ReadExactly(arr);
            data = arr;
        }
        return Cv2.ImDecode(data, ImreadModes.Grayscale);
    }

    protected virtual Mat? PrepareImage(Mat? image)
    {
        return image;
    }

    protected virtual Rect[]? GetRelevantRegions(Mat? image)
    {
        if (image is null)
            return null;

        // extract regions of interest (ROIs)
        // we're looking for rectangles that correspond to the tag buttons
        // methodology and code adapted from https://redrainkim.github.io/opencvsharp/opencvsharp-study-12/#

        using var blurred = image.GaussianBlur(new Size(9, 9), 0);
        using var threshold = blurred.Threshold(60, 255, ThresholdTypes.BinaryInv);

        Cv2.FindContours(threshold, out var contours, out _,
            RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        List<Rect> rects = [];
        foreach (var contour in contours)
        {
            if (!IsRectangle(contour, out var poly))
                continue;
            rects.Add(Cv2.BoundingRect(poly).ToAvaRect());
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
        return rects.ToArray();
    }

    private static bool IsRectangle(Point[] vertices, out Point[] poly)
    {
        var peri = Cv2.ArcLength(vertices, true);
        poly = Cv2.ApproxPolyDP(vertices, 0.04 * peri, true);
        return poly.Length == 4;
    }

    public virtual Task<OCRResult[]> Process(Stream imageData, string? lang = "en")
    {
        if (lang is null || lang.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            lang = "en";

        using var image = ParseImage(imageData);
        using var processed = PrepareImage(image);
        if (processed is null || GetRelevantRegions(processed) is not { } regions)
            return Task.FromResult(Array.Empty<OCRResult>());

        var results = regions.Select(region =>
        {
            using var roi = processed.SubMat(region.ToOpenCvRect());
            return PerformOCR(roi, lang!);
        }).ToArray();
        return Task.FromResult(results);
    }

    protected OCRResult PerformOCR(Mat image, string lang = "en", ComponentLevels componentLevels = ComponentLevels.Word)
    {
        var ocr = GetCachedOCR(lang);
        // ReSharper disable once UnusedVariable
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
        return new(text, rects.Zip(compTexts).Select(p =>
        {
            var (compRect, compText) = p;
            return new OCRResultComponent(compText, compRect.ToAvaRect());
        }).ToArray());
    }

    private OCRTesseract GetCachedOCR(string lang)
        => _ocrs.GetOrAdd(lang, () => CreateOCR(lang));

    private OCRTesseract CreateOCR(string lang) =>
        OCRTesseract.Create("data/tessdata/", lang, GetAllowedCharacters(lang))
            .DisposeWith(Disposables);

    protected virtual string? GetAllowedCharacters(string lang) => null;

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
