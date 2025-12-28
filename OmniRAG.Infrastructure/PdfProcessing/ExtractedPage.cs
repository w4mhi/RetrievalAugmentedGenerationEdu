using System.Collections.Generic;

namespace OmniRAG.Infrastructure.PdfProcessing;

/// <summary>
/// Represents a page extracted from a PDF document with text content and headings.
/// </summary>
/// <param name="PageNumber">The page number in the PDF document.</param>
/// <param name="Text">The extracted text content from the page.</param>
/// <param name="Headings">The extracted headings/titles found on the page.</param>
public record ExtractedPage(int PageNumber, string Text, IReadOnlyList<string> Headings);
