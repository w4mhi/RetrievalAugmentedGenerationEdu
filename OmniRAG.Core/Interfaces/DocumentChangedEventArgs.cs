namespace OmniRAG.Core.Interfaces;

/// <summary>
/// Event arguments for document change notifications.
/// </summary>
public class DocumentChangedEventArgs : EventArgs
{
    public string FilePath { get; }
    public FileChangeType ChangeType { get; }
    public DateTime Timestamp { get; }

    public DocumentChangedEventArgs(string filePath, FileChangeType changeType)
    {
        FilePath = filePath;
        ChangeType = changeType;
        Timestamp = DateTime.UtcNow;
    }
}
