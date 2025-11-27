using System.Text;
using MemGuard.Core;

namespace MemGuard.Reporters;

public class MarkdownReporter : IReporter
{
    public string Format => "Markdown";

    public async Task<string> GenerateReportAsync(AnalysisResult result, string outputPath, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"# MemGuard Analysis Report");
        sb.AppendLine($"**Date:** {DateTime.Now}");
        sb.AppendLine($"**Confidence Score:** {result.ConfidenceScore:P0}");
        sb.AppendLine();

        sb.AppendLine("## Root Cause");
        sb.AppendLine(result.RootCause);
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(result.CodeFix))
        {
            sb.AppendLine("## Suggested Fix");
            sb.AppendLine("```diff");
            sb.AppendLine(result.CodeFix);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        sb.AppendLine("## Diagnostics");
        foreach (var diagnostic in result.Diagnostics)
        {
            sb.AppendLine($"### {diagnostic.Type} ({diagnostic.Severity})");
            sb.AppendLine(diagnostic.Description);
            
            if (diagnostic is HeapDiagnostic heap)
            {
                sb.AppendLine($"- **Fragmentation:** {heap.FragmentationLevel:P2}");
                sb.AppendLine($"- **Total Size:** {heap.TotalSize:N0} bytes");
            }
            else if (diagnostic is DeadlockDiagnostic deadlock)
            {
                sb.AppendLine($"- **Threads Involved:** {string.Join(", ", deadlock.ThreadIds)}");
                sb.AppendLine($"- **Locks:**");
                foreach (var lockObj in deadlock.LockObjects)
                {
                    sb.AppendLine($"  - {lockObj}");
                }
            }
            
            sb.AppendLine();
        }

        var fullPath = Path.ChangeExtension(outputPath, ".md");
        await File.WriteAllTextAsync(fullPath, sb.ToString(), cancellationToken);
        return fullPath;
    }
}
