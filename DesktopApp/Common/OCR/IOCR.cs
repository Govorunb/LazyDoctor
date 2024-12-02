namespace DesktopApp.Common.OCR;

public interface IOCR<in TParam>
{
    Task<OCRResult[]> Process(Stream imageData, TParam? param = default);
}
