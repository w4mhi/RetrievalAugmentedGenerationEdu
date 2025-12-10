using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace OmniRAG.Infrastructure.PdfProcessing;

/// <summary>
/// Extracts text and metadata from PDF documents.
/// Strategy Pattern: Can be extended with different extraction strategies.
/// </summary>
public sealed class PdfTextExtractor
{
    private readonly ILogger<PdfTextExtractor>? logger;

    public PdfTextExtractor(ILogger<PdfTextExtractor>? logger = null)
    {
        this.logger = logger;
        this.logger?.LogInformation("PdfTextExtractor initialized");
    }

    public IReadOnlyList<ExtractedPage> ExtractPages(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"PDF file not found: {filePath}");
        }

        this.logger?.LogDebug("Extracting pages from PDF: {FilePath}", Path.GetFileName(filePath));

        try
        {
            List<ExtractedPage> pages = new List<ExtractedPage>();

            using PdfDocument document = PdfDocument.Open(filePath);
            foreach (Page page in document.GetPages())
            {
                string text = page.Text;
                IReadOnlyList<string> headings = ExtractHeadings(page);
                pages.Add(new ExtractedPage(page.Number, text, headings));
            }

            this.logger?.LogDebug("Successfully extracted {PageCount} pages from {FilePath}", pages.Count, Path.GetFileName(filePath));
            return pages;
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Error extracting pages from PDF {FilePath}: {ErrorMessage}", Path.GetFileName(filePath), ex.Message);
            throw;
        }
    }

    private static IReadOnlyList<string> ExtractHeadings(Page page)
    {
        List<string> headings = new List<string>();
        
        // Simple heuristic: larger font size or bold text likely indicates headings
        // This is a simplified approach - can be enhanced with more sophisticated analysis
        IEnumerable<Word> words = page.GetWords();
        
        if (!words.Any())
        {
            return headings;
        }

        // Use Letters collection which has font information
        IReadOnlyList<Letter> letters = page.Letters;
        double avgFontSize = letters.Any() ? letters.Average(l => l.PointSize) : 12.0;
        
        foreach (Word word in words)
        {
            // Get letters for this word and check if they're larger than average
            List<Letter> wordLetters = letters.Where(l => 
                l.StartBaseLine.X >= word.BoundingBox.Left && 
                l.StartBaseLine.X <= word.BoundingBox.Right &&
                l.StartBaseLine.Y >= word.BoundingBox.Bottom &&
                l.StartBaseLine.Y <= word.BoundingBox.Top).ToList();
            
            if (wordLetters.Any())
            {
                double wordFontSize = wordLetters.Average(l => l.PointSize);
                if (wordFontSize > avgFontSize * 1.2) // 20% larger than average
                {
                    headings.Add(word.Text);
                }
            }
        }

        return headings;
    }
}
