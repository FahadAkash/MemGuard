using Spectre.Console.Cli;
using MemGuard.Cli.Commands;
 
 
#pragma warning disable CA1861, CA2007   // We don't need those warnings in a tiny CLI tool

 
var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("memguard");
    
    // Analyze command
    config.AddCommand<AnalyzeDumpCommand>("analyze")
        .WithDescription("Analyze a memory dump and generate a report")
        .WithExample(new[] { "analyze", "crash.dmp", "--provider", "Gemini" });
    
    // Fix command
    config.AddCommand<FixDumpCommand>("fix")
        .WithDescription("Analyze a dump and apply AI-suggested fixes to code")
        .WithExample(new[] { "fix", "crash.dmp", "--project", "./MyApp", "--dry-run" });
    
    // Restore command
    config.AddCommand<RestoreCommand>("restore")
        .WithDescription("Restore files from a backup")
        .WithExample(new[] { "restore", "--list" })
        .WithExample(new[] { "restore", "--latest" });
});

return await app.RunAsync(args).ConfigureAwait(false);