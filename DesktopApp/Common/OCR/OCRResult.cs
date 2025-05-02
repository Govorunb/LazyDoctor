using Avalonia;

namespace DesktopApp.Common.OCR;

[PublicAPI]
public record struct OCRResultComponent(string? Text, Rect Rect);
[PublicAPI]
public record struct OCRResult(string FullText, OCRResultComponent[] Components);
