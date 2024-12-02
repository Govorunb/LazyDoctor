using DesktopApp.Common.OCR;

namespace DesktopApp.Recruitment.Processing;

public interface IRecruitTagsOCR : IOCR<string>
{
    Task<OCRResult[]> IOCR<string>.Process(Stream imageData, string? param)
        => Process(imageData, param!);
    new Task<OCRResult[]> Process(Stream imageData, string lang = "en");
}
