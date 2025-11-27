using MemGuard.Core;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MemGuard.Reporters;

public class PdfReporter : IReporter
{
    public string Format => "PDF";

    static PdfReporter()
    {
        // Set license to Community
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<string> GenerateReportAsync(AnalysisResult result, string outputPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.ChangeExtension(outputPath, ".pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text("MemGuard Analysis Report")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(20);

                        x.Item().Text($"Date: {DateTime.Now}");
                        x.Item().Text($"Confidence Score: {result.ConfidenceScore:P0}").Bold();

                        x.Item().Text("Root Cause").FontSize(16).SemiBold();
                        x.Item().Text(result.RootCause);

                        if (!string.IsNullOrWhiteSpace(result.CodeFix))
                        {
                            x.Item().Text("Suggested Fix").FontSize(16).SemiBold();
                            x.Item().Background(Colors.Grey.Lighten3).Padding(10)
                                .Text(result.CodeFix).FontFamily("Courier New");
                        }

                        x.Item().Text("Diagnostics").FontSize(16).SemiBold();
                        
                        foreach (var diagnostic in result.Diagnostics)
                        {
                            x.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                            {
                                c.Item().Text($"{diagnostic.Type} ({diagnostic.Severity})").Bold();
                                c.Item().Text(diagnostic.Description);
                                
                                if (diagnostic is HeapDiagnostic heap)
                                {
                                    c.Item().Text($"Fragmentation: {heap.FragmentationLevel:P2}");
                                    c.Item().Text($"Total Size: {heap.TotalSize:N0} bytes");
                                }
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
            });
        })
        .GeneratePdf(fullPath);

        return Task.FromResult(fullPath);
    }
}
