using Microsoft.Diagnostics.Runtime;
using MemGuard.Core.Interfaces;

namespace MemGuard.Infrastructure;

/// <summary>
/// Parses memory dumps using ClrMD library
/// </summary>
public class DumpParser : IDumpParser
{
    private DataTarget? _dataTarget;
    private ClrRuntime? _runtime;

    /// <inheritdoc />
    public ClrRuntime LoadDump(string dumpPath)
    {
        ArgumentNullException.ThrowIfNull(dumpPath);

        if (!File.Exists(dumpPath))
            throw new FileNotFoundException($"Dump file not found: {dumpPath}");

        // Clean up previous session if any
        // Dispose(); // Removed: DumpParser is now Transient, no need to reuse instance

        try
        {
            _dataTarget = DataTarget.LoadDump(dumpPath);
            
            var clrVersion = _dataTarget.ClrVersions.FirstOrDefault() 
                ?? throw new InvalidOperationException("No .NET runtime found in the dump.");
                
            _runtime = clrVersion.CreateRuntime();
            return _runtime;
        }
        catch (Exception)
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        try 
        {
            _runtime?.Dispose();
            _runtime = null;
            
            _dataTarget?.Dispose();
            _dataTarget = null;
        }
        catch
        {
            // Ignore disposal errors to prevent crashing
        }
        
        GC.SuppressFinalize(this);
    }
}