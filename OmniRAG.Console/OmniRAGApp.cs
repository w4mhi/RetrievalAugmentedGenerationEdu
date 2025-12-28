using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;

using Spectre.Console;

namespace OmniRAG.Console;

/// <summary>
/// Main application class that orchestrates the RAG workflow.
/// Single Responsibility: Handles user interaction and command routing.
/// Design Pattern: Facade pattern for coordinating RAG engine and document monitoring.
/// </summary>
public sealed class OmniRAGApp : IDisposable
{
    private readonly IRagEngine ragEngine;
    private readonly IConfiguration configuration;
    private readonly IDocumentMonitor? documentMonitor;
    private readonly IDocumentRepository? documentRepository;
    private readonly ILogger<OmniRAGApp>? logger;
    private bool monitoringEnabled;

    public OmniRAGApp(
        IConfiguration configuration,
        IRagEngine ragEngine,
        IDocumentMonitor? documentMonitor = null,
        IDocumentRepository? documentRepository = null,
        ILogger<OmniRAGApp>? logger = null)
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.ragEngine = ragEngine ?? throw new ArgumentNullException(nameof(ragEngine));
        this.documentMonitor = documentMonitor;
        this.documentRepository = documentRepository;
        this.logger = logger;
        this.logger?.LogInformation("OmniRAGApp initialized");
    }

    public async Task RunAsync()
    {
        this.logger?.LogDebug("Starting OmniRAGApp.RunAsync");

        try
        {
            // Start document monitoring if available
            await StartDocumentMonitoringAsync();

            // Check if we need to index documents
            await EnsureDocumentsIndexedAsync();

            // Main query loop
            await RunQueryLoopAsync();
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Fatal error in OmniRAGApp: {ErrorMessage}", ex.Message);
            AnsiConsole.MarkupLine($"[red]Fatal error: {ex.Message}[/]");
            AnsiConsole.WriteException(ex);
        }
        finally
        {
            // Stop monitoring on exit
            await StopDocumentMonitoringAsync();
            this.logger?.LogDebug("OmniRAGApp.RunAsync completed");
        }
    }

    private async Task StartDocumentMonitoringAsync()
    {
        if (this.documentMonitor == null)
        {
            return;
        }

        bool enableMonitoring = this.configuration.GetValue<bool>("OmniRAG:EnableAutoIndexing", true);
        if (!enableMonitoring)
        {
            AnsiConsole.MarkupLine("[dim]ℹ Auto-indexing disabled[/]");
            return;
        }

        // Subscribe to document changes
        this.documentMonitor.DocumentChanged += this.OnDocumentChanged;

        // Start monitoring
        await this.documentMonitor.StartAsync();
        this.monitoringEnabled = true;

        AnsiConsole.MarkupLine($"[green]✓[/] Auto-indexing enabled for: [cyan]{this.documentMonitor.MonitoredPath}[/]");
    }

    private async Task StopDocumentMonitoringAsync()
    {
        if (this.documentMonitor != null && this.monitoringEnabled)
        {
            this.documentMonitor.DocumentChanged -= this.OnDocumentChanged;
            await this.documentMonitor.StopAsync();
            this.monitoringEnabled = false;
        }
    }

    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        Task.Run(async () =>
        {
            try
            {
                await this.HandleDocumentChangeAsync(e);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error auto-indexing document: {ex.Message}[/]");
            }
        });
    }

    /// <summary>
    /// Handles document change events.
    /// Reduces method complexity (Rule 8).
    /// </summary>
    private async Task HandleDocumentChangeAsync(DocumentChangedEventArgs e)
    {
        switch (e.ChangeType)
        {
            case FileChangeType.Added:
                await this.HandleDocumentAddedAsync(e.FilePath);
                break;

            case FileChangeType.Modified:
                await this.HandleDocumentModifiedAsync(e.FilePath);
                break;

            case FileChangeType.Deleted:
                HandleDocumentDeleted(e.FilePath);
                break;
        }
    }

    private async Task HandleDocumentAddedAsync(string filePath)
    {
        AnsiConsole.MarkupLine($"\n[yellow]📄 New PDF detected:[/] [cyan]{Path.GetFileName(filePath)}[/]");
        await this.IndexSingleDocumentAsync(filePath);
        AnsiConsole.MarkupLine("[green]✓ Document indexed automatically[/]\n");
    }

    private async Task HandleDocumentModifiedAsync(string filePath)
    {
        AnsiConsole.MarkupLine($"\n[yellow]📝 PDF modified:[/] [cyan]{Path.GetFileName(filePath)}[/]");
        await this.IndexSingleDocumentAsync(filePath);
        AnsiConsole.MarkupLine("[green]✓ Document re-indexed automatically[/]\n");
    }

    private static void HandleDocumentDeleted(string filePath)
    {
        AnsiConsole.MarkupLine($"\n[red]🗑 PDF deleted:[/] [cyan]{Path.GetFileName(filePath)}[/]");
    }

    private async Task IndexSingleDocumentAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        // Create temporary directory with single file for indexing
        string? tempDir = Path.GetDirectoryName(filePath);
        if (tempDir != null)
        {
            await this.ragEngine.IndexDocumentsAsync(tempDir);
        }
    }

    private async Task EnsureDocumentsIndexedAsync()
    {
        (int totalChunks, DateTime? lastIndexed) = await this.ragEngine.GetIndexStatsAsync();

        if (totalChunks == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No documents indexed. Starting indexing process...[/]");
            await this.IndexDocumentsAsync();
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Found {totalChunks} indexed chunks");
            if (lastIndexed.HasValue)
            {
                AnsiConsole.MarkupLine($"[dim]Last indexed: {lastIndexed.Value:yyyy-MM-dd HH:mm:ss} UTC[/]");
            }

            // Ask if user wants to re-index
            if (AnsiConsole.Confirm("Do you want to re-index documents?", false))
            {
                await this.IndexDocumentsAsync();
            }
        }

        AnsiConsole.WriteLine();
    }

    private async Task IndexDocumentsAsync()
    {
        string pdfDirectory = this.configuration["OmniRAG:PdfDirectory"] 
            ?? throw new InvalidOperationException("PDF directory not configured");

        string fullPath = Path.GetFullPath(pdfDirectory);

        // Use repository if available, otherwise fall back to direct file system access
        if (this.documentRepository != null)
        {
            this.logger?.LogDebug("Using document repository for indexing");

            // Refresh repository to detect new files
            await this.documentRepository.RefreshAsync();

            IEnumerable<DocumentMetadata> pdfDocuments = await this.documentRepository.GetByExtensionAsync(".pdf");
            int documentCount = pdfDocuments.Count();

            if (documentCount == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No PDF files found in repository[/]");
                return;
            }

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Indexing {documentCount} PDF files...", async ctx =>
                {
                    await this.ragEngine.IndexDocumentsAsync(fullPath);
                });

            (int indexedChunks, DateTime? _) = await this.ragEngine.GetIndexStatsAsync();
            AnsiConsole.MarkupLine($"[green]✓[/] Successfully indexed {indexedChunks} chunks from {documentCount} documents");
        }
        else
        {
            // Fallback to direct file system access (backward compatibility)
            this.logger?.LogDebug("Using direct file system access for indexing");

            if (!Directory.Exists(fullPath))
            {
                AnsiConsole.MarkupLine($"[red]PDF directory not found: {fullPath}[/]");
                AnsiConsole.MarkupLine("[yellow]Creating directory...[/]");
                Directory.CreateDirectory(fullPath);
                AnsiConsole.MarkupLine($"[yellow]Please add PDF files to: {fullPath}[/]");
                return;
            }

            string[] pdfFiles = Directory.GetFiles(fullPath, "*.pdf");
            if (pdfFiles.Length == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No PDF files found in: {fullPath}[/]");
                return;
            }

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Indexing {pdfFiles.Length} PDF files...", async ctx =>
                {
                    await this.ragEngine.IndexDocumentsAsync(fullPath);
                });

            (int fallbackChunks, DateTime? _) = await this.ragEngine.GetIndexStatsAsync();
            AnsiConsole.MarkupLine($"[green]✓[/] Successfully indexed {fallbackChunks} chunks from {pdfFiles.Length} documents");
        }
    }

    private async Task RunQueryLoopAsync()
    {
        AnsiConsole.MarkupLine("[cyan]━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━[/]");
        AnsiConsole.MarkupLine("[cyan]Ready to answer questions! (Type ''exit'' to quit)[/]");
        AnsiConsole.MarkupLine("[cyan]━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━[/]");
        AnsiConsole.WriteLine();

        while (true)
        {
            string query = AnsiConsole.Ask<string>("[bold blue]Your question:[/]");

            if (string.IsNullOrWhiteSpace(query))
            {
                continue;
            }

            if (query.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                query.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
                break;
            }

            // Handle special commands
            if (query.Equals("index", StringComparison.OrdinalIgnoreCase))
            {
                await this.IndexDocumentsAsync();
                continue;
            }

            if (query.Equals("stats", StringComparison.OrdinalIgnoreCase))
            {
                await this.ShowStatsAsync();
                continue;
            }

            // Process query
            await this.ProcessQueryAsync(query);
            AnsiConsole.WriteLine();
        }
    }

    private async Task ProcessQueryAsync(string query)
    {
        try
        {
            RagResponse response = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Searching documentation...", async ctx =>
                {
                    return await this.ragEngine.QueryAsync(query);
                });

            // Display answer
            Panel panel = new Panel(response.Answer)
                .Header("[bold green]Answer[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green);
            AnsiConsole.Write(panel);

            // Display sources
            if (response.Sources.Count > 0)
            {
                AnsiConsole.MarkupLine("\n[dim]Sources:[/]");
                Table table = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Grey);

                table.AddColumn("Rank");
                table.AddColumn("Document");
                table.AddColumn("Page");
                table.AddColumn("Section");
                table.AddColumn("Score");

                foreach (SearchResult source in response.Sources)
                {
                    table.AddRow(
                        source.Rank.ToString(),
                        Path.GetFileName(source.Chunk.SourceFilePath),
                        source.Chunk.PageNumber.ToString(),
                        source.Chunk.SectionTitle,
                        $"{source.RelevanceScore:P1}");
                }

                AnsiConsole.Write(table);
            }

            AnsiConsole.MarkupLine($"\n[dim]Processing time: {response.ProcessingTime.TotalSeconds:F2}s[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error processing query: {ex.Message}[/]");
        }
    }

    private async Task ShowStatsAsync()
    {
        (int totalChunks, DateTime? lastIndexed) = await this.ragEngine.GetIndexStatsAsync();

        Table statsTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue);

        statsTable.AddColumn("Metric");
        statsTable.AddColumn("Value");

        statsTable.AddRow("Total Chunks", totalChunks.ToString());
        statsTable.AddRow("Last Indexed", lastIndexed?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never");

        if (this.documentMonitor != null && this.monitoringEnabled)
        {
            statsTable.AddRow("Auto-Indexing", "Enabled ✓");
            statsTable.AddRow("Monitored Path", this.documentMonitor.MonitoredPath);
        }
        else
        {
            statsTable.AddRow("Auto-Indexing", "Disabled");
        }

        AnsiConsole.Write(statsTable);
    }

    public void Dispose()
    {
        this.StopDocumentMonitoringAsync().GetAwaiter().GetResult();
        this.documentMonitor?.Dispose();
    }
}
