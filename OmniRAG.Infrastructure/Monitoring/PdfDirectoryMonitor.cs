using Microsoft.Extensions.Logging;
using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;

namespace OmniRAG.Infrastructure.Monitoring;

/// <summary>
/// Monitors a directory for PDF file changes using FileSystemWatcher.
/// Design Patterns: Observer (for file events), Adapter (wraps FileSystemWatcher).
/// SOLID Principles:
/// - Single Responsibility: Only monitors PDF files
/// - Open/Closed: Extensible via events, closed for modification
/// - Dependency Inversion: Depends on ILogger abstraction
/// </summary>
public class PdfDirectoryMonitor : IDocumentMonitor
{
    private readonly string directoryPath;
    private readonly ILogger<PdfDirectoryMonitor>? logger;
    private FileSystemWatcher? watcher;
    private bool isRunning;
    private readonly object lockObject = new();

    public event EventHandler<DocumentChangedEventArgs>? DocumentChanged;

    public bool IsRunning
    {
        get
        {
            lock (this.lockObject)
            {
                return this.isRunning;
            }
        }
    }

    public string MonitoredPath => this.directoryPath;

    public PdfDirectoryMonitor(string directoryPath, ILogger<PdfDirectoryMonitor>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Directory path cannot be null or empty.", nameof(directoryPath));
        }

        // Support relative paths
        this.directoryPath = Path.IsPathRooted(directoryPath) 
            ? directoryPath 
            : Path.GetFullPath(directoryPath);
        
        this.logger = logger;
        this.logger?.LogInformation("PdfDirectoryMonitor initialized for path: {Path}", this.directoryPath);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (this.lockObject)
        {
            if (this.isRunning)
            {
                this.logger?.LogWarning("Monitor is already running for path: {Path}", this.directoryPath);
                return Task.CompletedTask;
            }

            // Ensure directory exists
            if (!Directory.Exists(this.directoryPath))
            {
                Directory.CreateDirectory(this.directoryPath);
                this.logger?.LogInformation("Created monitoring directory: {Path}", this.directoryPath);
            }

            // Create and configure FileSystemWatcher
            this.watcher = new FileSystemWatcher(this.directoryPath)
            {
                Filter = "*.pdf",
                NotifyFilter = NotifyFilters.FileName | 
                              NotifyFilters.LastWrite | 
                              NotifyFilters.CreationTime,
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };

            // Subscribe to events
            this.watcher.Created += OnFileCreated;
            this.watcher.Changed += OnFileChanged;
            this.watcher.Deleted += OnFileDeleted;
            this.watcher.Error += OnError;

            this.isRunning = true;
            this.logger?.LogInformation("Started monitoring PDF directory: {Path}", this.directoryPath);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        lock (this.lockObject)
        {
            if (!this.isRunning)
            {
                return Task.CompletedTask;
            }

            if (this.watcher != null)
            {
                this.watcher.EnableRaisingEvents = false;
                
                // Unsubscribe from events
                this.watcher.Created -= OnFileCreated;
                this.watcher.Changed -= OnFileChanged;
                this.watcher.Deleted -= OnFileDeleted;
                this.watcher.Error -= OnError;
                
                this.watcher.Dispose();
                this.watcher = null;
            }

            this.isRunning = false;
            this.logger?.LogInformation("Stopped monitoring PDF directory: {Path}", this.directoryPath);
        }

        return Task.CompletedTask;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        this.logger?.LogDebug("PDF file created: {FilePath}", e.Name);
        RaiseDocumentChanged(e.FullPath, FileChangeType.Added);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        this.logger?.LogDebug("PDF file modified: {FilePath}", e.Name);
        RaiseDocumentChanged(e.FullPath, FileChangeType.Modified);
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        this.logger?.LogDebug("PDF file deleted: {FilePath}", e.Name);
        RaiseDocumentChanged(e.FullPath, FileChangeType.Deleted);
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        Exception? exception = e.GetException();
        this.logger?.LogError(exception, "File system watcher error occurred");
    }

    private void RaiseDocumentChanged(string filePath, FileChangeType changeType)
    {
        try
        {
            DocumentChangedEventArgs args = new DocumentChangedEventArgs(filePath, changeType);
            DocumentChanged?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Error raising DocumentChanged event for {FilePath}", filePath);
        }
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }
}