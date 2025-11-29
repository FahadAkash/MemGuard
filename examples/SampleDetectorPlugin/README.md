# Sample MemGuard Detector Plugin

This is a tutorial example showing how to create custom detector plugins for MemGuard.

## What This Plugin Does

The `ExcessiveStringDetector` analyzes memory dumps to detect excessive string allocations, which might indicate:
- Memory leaks from string concatenation
- Caching issues
- Logging problems
- Data processing inefficiencies

## Creating Your Own Plugin

### 1. Create a New Class Library Project

```bash
dotnet new classlib -n MyCustomDetector
cd MyCustomDetector
```

### 2. Add MemGuard References

Edit your `.csproj` file:

```xml
<ItemGroup>
  <ProjectReference Include="path\to\MemGuard.Plugins\MemGuard.Plugins.csproj" />
  <ProjectReference Include="path\to\MemGuard.Core\MemGuard.Core.csproj" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.Diagnostics.Runtime" Version="3.0.442202" />
</ItemGroup>
```

### 3. Implement IDetectorPlugin

```csharp
using MemGuard.Core;
using MemGuard.Plugins;
using Microsoft.Diagnostics.Runtime;

public class MyCustomDetector : IDetectorPlugin
{
    public string Name => "MyCustomDetector";
    public string Version => "1.0.0";
    public string Description => "Describes what your detector does";

    public async Task<DiagnosticBase?> AnalyzeAsync(ClrRuntime runtime, CancellationToken cancellationToken)
    {
        // Your detection logic here
        // Return a DiagnosticBase-derived object if you find an issue
        // Return null if no issue is detected
        
        return null;
    }
}
```

### 4. Build Your Plugin

```bash
dotnet build -c Release
```

### 5. Deploy Your Plugin

Copy the compiled DLL to MemGuard's plugin directory:

**Windows:**
```bash
copy bin\Release\net8.0\MyCustomDetector.dll %USERPROFILE%\.memguard\plugins\
```

**Linux/Mac:**
```bash
cp bin/Release/net8.0/MyCustomDetector.dll ~/.memguard/plugins/
```

### 6. Verify Plugin is Loaded

Run MemGuard and check the startup output:

```bash
memguard analyze dump.dmp --provider gemini --api-key YOUR_KEY
```

You should see: `Loaded detector plugin: MyCustomDetector v1.0.0`

## Tips for Plugin Development

### Access Memory Objects

```csharp
foreach (var obj in runtime.Heap.EnumerateObjects())
{
    cancellationToken.ThrowIfCancellationRequested();
    
    if (obj.Type?.Name == "MyNamespace.MyClass")
    {
        // Analyze this object
        var fieldValue = obj.ReadObjectField("MyField");
    }
}
```

### Check Thread State

```csharp
foreach (var thread in runtime.Threads)
{
    foreach (var frame in thread.EnumerateStackTrace())
    {
        // Analyze stack frames
        var method = frame.Method?.Name;
    }
}
```

### Return Custom Diagnostics

You can use existing diagnostic types or create your own:

```csharp
return new PinnedObjectDiagnostic(
    count: issueCount,
    gcPressureLevel: "High"
);
```

## Building This Sample

```bash
cd examples\SampleDetectorPlugin
dotnet build
copy bin\Debug\net8.0\SampleDetectorPlugin.dll %USERPROFILE%\.memguard\plugins\
```

## Testing

1. Analyze any .NET memory dump
2. Check output for "ExcessiveStringDetector" findings
3. Verify string count and size are reported

## Best Practices

1. **Performance**: Limit iterations with early exits
2. **Error Handling**: Wrap in try-catch, don't crash the analysis
3. **Cancellation**: Check `cancellationToken.ThrowIfCancellationRequested()`
4. **Memory**: Don't allocate excessive memory during analysis
5. **Logging**: Use Console.WriteLine for  debug output

## Example Output

When excessive strings are detected:

```
[ExcessiveStringDetector] Warning
Found 75,432 string objects consuming 128.5 MB
This may indicate a memory leak or caching issue.
```

## Further Reading

- [MemGuard Plugin API Documentation](../../README.md#plugin-system)
- [ClrMD Documentation](https://github.com/microsoft/clrmd)
- [Creating Custom Diagnostics](../../src/MemGuard.Core/DomainModels.cs)
