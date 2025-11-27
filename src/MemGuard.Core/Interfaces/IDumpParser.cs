using Microsoft.Diagnostics.Runtime;

namespace MemGuard.Core.Interfaces;

/// <summary>
/// Abstraction for loading and parsing memory dumps
/// </summary>
public interface IDumpParser : IDisposable
{
    /// <summary>
    /// Loads a memory dump from the specified path
    /// </summary>
    /// <param name="dumpPath">Path to the dump file</param>
    /// <returns>ClrRuntime instance</returns>
    ClrRuntime LoadDump(string dumpPath);
}
