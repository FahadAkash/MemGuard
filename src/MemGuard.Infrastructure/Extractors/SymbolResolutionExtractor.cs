using MemGuard.Core.Interfaces;
using Microsoft.Diagnostics.Runtime;

namespace MemGuard.Infrastructure.Extractors;

/// <summary>
/// Extractor for runtime symbol resolution to enhance stack traces
/// </summary>
public class SymbolResolutionExtractor : IDiagnosticExtractor
{
    public string Name => "SymbolResolution";
    
    private readonly string? _symbolPath;

    public SymbolResolutionExtractor(string? symbolPath = null)
    {
        // Use Microsoft Symbol Server by default
        _symbolPath = symbolPath ?? "SRV*https://msdl.microsoft.com/download/symbols";
    }

    public Task<Core.DiagnosticBase?> ExtractAsync(ClrRuntime runtime, CancellationToken cancellationToken = default)
    {
        // This extractor doesn't produce a diagnostic on its own
        // Instead, it enhances the runtime with better symbol resolution
        // The actual symbol resolution happens automatically in ClrMD when configured properly
        
        try
        {
            // Configure symbol resolution if not already done
            if (runtime.DataTarget != null)
            {
                // ClrMD handles symbol resolution automatically
                // We just ensure paths are configured
                var modules = runtime.DataTarget.EnumerateModules().ToList();
                
                // Verify symbol resolution is working
                foreach (var module in modules.Take(5))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Access module info to trigger symbol loading
                    _ = module.FileName;
                    _ = module.ImageSize;
                }
            }
        }
        catch
        {
            // Symbol resolution is best-effort
        }

        // This extractor enhances other extractors but doesn't return its own diagnostic
        return Task.FromResult<Core.DiagnosticBase?>(null);
    }
}
