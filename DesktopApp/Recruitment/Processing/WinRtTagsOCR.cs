using DesktopApp.Common.OCR;

namespace DesktopApp.Recruitment.Processing;

public sealed class WinRtTagsOCR(WinRtOCR inner) : ServiceBase, IRecruitTagsOCR
{
    public Task<OCRResult[]> Process(Stream imageData, string? lang = "en")
        => inner.Process(imageData, lang);
}
