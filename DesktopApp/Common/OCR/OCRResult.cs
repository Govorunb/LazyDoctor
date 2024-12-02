using Avalonia;
using JetBrains.Annotations;

namespace DesktopApp.Common.OCR;

[PublicAPI]
public record struct OCRResult(string FullText, Rect[] ComponentRects, string?[] ComponentTexts);