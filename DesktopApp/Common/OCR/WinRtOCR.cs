using System.Runtime.InteropServices;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace DesktopApp.Common.OCR;

public sealed class WinRtOCR : ServiceBase, IOCR<string>
{
    // thanks to https://ziviz.us/tools/win32errors.html
    private static readonly Dictionary<uint, string> _comExceptionMessages = new()
    {
        [0x88982F07] = "The image format is unknown",
        [0x88982F50] = "The component cannot be found",
        [0x88982F72] = "Failed to read from the stream",
    };
    public async Task<OCRResult[]> Process(Stream imageData, string? lang = null)
    {
        var ocrResult = await Process(imageData.AsRandomAccessStream(), lang);
        return ocrResult.HasValue ? [ocrResult.Value] : [];
    }

    public async Task<OCRResult?> Process(IRandomAccessStream stream, string? lang = null)
    {
        var engine = GetEngine(lang) ?? throw new ArgumentException($"Could not create an OCR engine for language {lang}.");

        SoftwareBitmap? bitmap = null;
        for (var tries = 1; tries <= 5; tries++)
        {
            try
            {
                var decoder = await BitmapDecoder.CreateAsync(stream);
                bitmap = await decoder.GetSoftwareBitmapAsync();
                break;
            }
            catch (COMException e)
            {
                var exMsg = e.Message;
                if (string.IsNullOrEmpty(e.Message))
                    exMsg = _comExceptionMessages.GetValueOrDefault((uint)e.ErrorCode) ?? "Unknown";
                this.Log().Warn($"Decoding failed {tries}/5: ({e.ErrorCode:X}) {exMsg}");
                await Task.Delay(10);
            }
        }

        if (bitmap is null)
        {
            this.Log().Error("Could not decode image. Thank you COMException very cool.");
            return null;
        }

        this.Log().Debug($"Performing OCR with {engine.RecognizerLanguage.DisplayName} language pack.");

        using (bitmap)
        {
            var winRtOcrResult = await engine.RecognizeAsync(bitmap);
            var components = winRtOcrResult.Lines
                .SelectMany(l => l.Words)
                .Select(word => new OCRResultComponent(word.Text, word.BoundingRect.ToAvaRect()))
                .ToArray();
            return new(winRtOcrResult.Text, components);
        }
    }

    private static OcrEngine? GetEngine(string? lang)
    {
        if (string.IsNullOrEmpty(lang))
        {
            return OcrEngine.TryCreateFromUserProfileLanguages()
                ?? throw new InvalidOperationException("You do not have any OCR packs installed.");
        }

        if (!Language.IsWellFormed(lang))
            throw new ArgumentException($"Language {lang} is not a valid BCP-47 language tag.");

        Language language = new(lang);
        if (!OcrEngine.IsLanguageSupported(language))
            throw new NotSupportedException($"Could not find an installed OCR pack for language {lang}.");

        return OcrEngine.TryCreateFromLanguage(language); // theoretically also uses the IsLanguageSupported guard
    }
}
