namespace MemGuard.Core.Models;

public class RetainedObjectPath
{
    public ulong ObjectAddress { get; set; }
    public IReadOnlyList<string> PathFromRoot { get; set; } = Array.Empty<string>();
    public int PathDepth => PathFromRoot.Count;
}
