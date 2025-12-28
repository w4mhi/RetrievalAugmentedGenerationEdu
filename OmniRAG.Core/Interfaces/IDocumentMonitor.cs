using System;
using System.Threading;
using System.Threading.Tasks;

using OmniRAG.Core.Models;

namespace OmniRAG.Core.Interfaces;

/// <summary>
/// Monitors a directory for document changes and triggers automatic indexing.
/// Design Pattern: Observer pattern for file system events.
/// SOLID: Single Responsibility - only monitors files, delegates indexing to IRagEngine.
/// </summary>
public interface IDocumentMonitor : IDisposable
{
    /// <summary>
    /// Event raised when a document is added or modified.
    /// </summary>
    event EventHandler<DocumentChangedEventArgs>? DocumentChanged;

    /// <summary>
    /// Starts monitoring the directory.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops monitoring the directory.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the monitor is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the path being monitored.
    /// </summary>
    string MonitoredPath { get; }
}
