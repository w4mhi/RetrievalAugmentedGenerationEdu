using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Polly;

using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;
using OmniRAG.Infrastructure.PdfProcessing;
using OmniRAG.Infrastructure.Resilience;

namespace OmniRAG.Infrastructure.DocumentLoaders;

/// <summary>
/// Loads and processes PDF documents into chunks.
/// Open/Closed Principle: Open for extension (new document types), closed for modification.
/// Dependency Inversion Principle: Depends on ITextChunker abstraction, not concrete implementation.
/// </summary>
public sealed class PdfDocumentLoader : IDocumentLoader
{
    private readonly PdfTextExtractor pdfExtractor;
    private readonly ITextChunker chunker;
    private readonly IEmbeddingService embeddingService;
    private readonly ILogger<PdfDocumentLoader>? logger;
    private readonly IAsyncPolicy<IReadOnlyList<DocumentChunk>> documentLoaderPolicy;

    public PdfDocumentLoader(IEmbeddingService embeddingService, ITextChunker chunker, ILogger<PdfDocumentLoader>? logger = null)
    {
        this.embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        this.chunker = chunker ?? throw new ArgumentNullException(nameof(chunker));
        this.logger = logger;
        this.pdfExtractor = new PdfTextExtractor();
        
        // Initialize resilience policy for per-file error handling (Fallback)
        this.documentLoaderPolicy = ResiliencePolicies.CreateDocumentLoaderPipeline<IReadOnlyList<DocumentChunk>>(
            this.logger,
            Array.Empty<DocumentChunk>(),  // Return empty list if file fails
            "PdfDocumentLoading");
        
        this.logger?.LogInformation("PdfDocumentLoader initialized with chunker: {ChunkerType}", chunker.GetType().Name);
    }

    public async Task<IReadOnlyList<DocumentChunk>> LoadDocumentsAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        this.logger?.LogDebug("Loading documents from directory: {DirectoryPath}", directoryPath);

        try
        {
            string[] pdfFiles = Directory.GetFiles(directoryPath, "*.pdf", SearchOption.TopDirectoryOnly);
            
            if (pdfFiles.Length == 0)
            {
                this.logger?.LogWarning("No PDF files found in directory: {DirectoryPath}", directoryPath);
                return Array.Empty<DocumentChunk>();
            }

            this.logger?.LogInformation("Found {FileCount} PDF files to process", pdfFiles.Length);
            List<DocumentChunk> allChunks = new List<DocumentChunk>();

            foreach (string pdfFile in pdfFiles)
            {
                // Execute with resilience policy (Fallback for per-file errors)
                IReadOnlyList<DocumentChunk> chunks = await this.documentLoaderPolicy.ExecuteAsync(
                    async (context) => await LoadDocumentAsync(pdfFile, cancellationToken),
                    new Dictionary<string, object> { ["FilePath"] = pdfFile });
                allChunks.AddRange(chunks);
            }

            this.logger?.LogInformation("Completed loading {ChunkCount} total chunks from {FileCount} files", allChunks.Count, pdfFiles.Length);
            return allChunks;
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Error loading documents from directory {DirectoryPath}: {ErrorMessage}", directoryPath, ex.Message);
            throw;
        }
    }

    public async Task<IReadOnlyList<DocumentChunk>> LoadDocumentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        this.logger?.LogDebug("Processing PDF file: {FileName}", Path.GetFileName(filePath));

        try
        {
            Console.WriteLine($"Processing: {Path.GetFileName(filePath)}");

            // Extract text from PDF
            IReadOnlyList<ExtractedPage> pages = this.pdfExtractor.ExtractPages(filePath);
            this.logger?.LogDebug("Extracted {PageCount} pages from {FileName}", pages.Count, Path.GetFileName(filePath));
            
            List<DocumentChunk> documentChunks = new List<DocumentChunk>();

            // Process each page
            foreach (ExtractedPage page in pages)
            {
                // Chunk the page text
                IReadOnlyList<TextChunk> textChunks = this.chunker.ChunkText(page.Text, page.PageNumber, page.Headings);

                // Generate embeddings for chunks
                List<string> chunkTexts = textChunks.Select(c => c.Content).ToList();
                IReadOnlyList<float[]> embeddings = await this.embeddingService.GenerateEmbeddingsAsync(chunkTexts, cancellationToken);

                // Create DocumentChunk objects
                for (int i = 0; i < textChunks.Count; i++)
                {
                    TextChunk textChunk = textChunks[i];
                    float[] embedding = embeddings[i];

                    Dictionary<string, object> metadata = new Dictionary<string, object>
                    {
                        ["fileName"] = Path.GetFileName(filePath),
                        ["startIndex"] = textChunk.StartIndex,
                        ["endIndex"] = textChunk.EndIndex
                    };

                    DocumentChunk documentChunk = DocumentChunk.Create(
                        textChunk.Content,
                        filePath,
                        textChunk.PageNumber,
                        textChunk.SectionTitle,
                        embedding,
                        metadata);

                    documentChunks.Add(documentChunk);
                }
            }

            Console.WriteLine($"  Created {documentChunks.Count} chunks from {pages.Count} pages");
            this.logger?.LogInformation("Successfully processed {FileName}: {ChunkCount} chunks from {PageCount} pages", 
                Path.GetFileName(filePath), documentChunks.Count, pages.Count);

            return documentChunks;
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Error processing PDF file {FileName}: {ErrorMessage}", Path.GetFileName(filePath), ex.Message);
            throw;
        }
    }
}
