using Spectre.Console.Cli;
using MemGuard.Cli.Commands;
 

#pragma warning disable CA1861, CA2007   // We don't need those warnings in a tiny CLI tool

 

var app = new CommandApp<AnalyzeDumpCommand>();
app.Configure(config =>
{
    config.SetApplicationName("memguard");
    config.AddExample(new[] { "analyze", "crash.dmp" });
});

return await app.RunAsync(args).ConfigureAwait(false);