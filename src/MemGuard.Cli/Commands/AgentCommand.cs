using Spectre.Console;
using Spectre.Console.Cli;
using MemGuard.AI;
using MemGuard.Cli.Models;
using MemGuard.Core.Interfaces;
using MemGuard.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace MemGuard.Cli.Commands;

public sealed class AgentCommand : AsyncCommand<AgentSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AgentSettings settings)
    {
        try
        {
            // Display welcome banner
            AnsiConsole.Write(new FigletText("MemGuard AI Agent").Color(Color.Green));
            AnsiConsole.MarkupLine("[grey]Your AI-Powered Development Assistant[/]");
            AnsiConsole.WriteLine();

            // Setup services
            var services = new ServiceCollection();
            services.AddHttpClient();
            services.AddSingleton<IFileManager, FileManager>();
            services.AddSingleton<IDumpParser, DumpParser>();
            var serviceProvider = services.BuildServiceProvider();
            
            var factory = new LLMProviderFactory(serviceProvider.GetRequiredService<IHttpClientFactory>());
            var fileManager = serviceProvider.GetRequiredService<IFileManager>();
            var dumpParser = serviceProvider.GetRequiredService<IDumpParser>();
            
            // Get API key
            var apiKey = settings.ApiKey ?? GetApiKeyFromEnvironment(settings.Provider);
            if (string.IsNullOrEmpty(apiKey) && settings.Provider != "ollama")
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] No API key provided for {settings.Provider}");
                AnsiConsole.MarkupLine($"[yellow]Set environment variable:[/] MEMGUARD_{settings.Provider.ToUpper()}_KEY");
                AnsiConsole.MarkupLine($"[yellow]Or use:[/] --api-key YOUR_KEY");
                return 1;
            }

            var ai = factory.CreateClient(settings.Provider, apiKey ?? "", settings.Model);

            // Display configuration
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("Setting");
            table.AddColumn("Value");
            table.AddRow("AI Provider", settings.Provider);
            table.AddRow("Model", settings.Model ?? LLMProviderFactory.GetDefaultModel(settings.Provider));
            table.AddRow("Project", settings.ProjectPath ?? "Not set");
            table.AddRow("Mode", settings.Autonomous ? "Autonomous" : "Interactive");
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            // Analyze project if provided
            string? projectContext = null;
            if (!string.IsNullOrEmpty(settings.ProjectPath) && Directory.Exists(settings.ProjectPath))
            {
                AnsiConsole.MarkupLine($"[green]Analyzing project:[/] {settings.ProjectPath}");
                projectContext = await AnalyzeProjectAsync(settings.ProjectPath, fileManager);
                AnsiConsole.MarkupLine("[green]✓[/] Project analyzed!");
                AnsiConsole.WriteLine();
            }

            // Welcome message
            AnsiConsole.MarkupLine("[green]Welcome![/] I'm your AI assistant. I can help you:");
            AnsiConsole.MarkupLine("  • [cyan]Analyze your project[/] - 'analyze project' or 'scan files'");
            AnsiConsole.MarkupLine("  • [cyan]Read files[/] - 'read UserService.cs' or 'show me Program.cs'");
            AnsiConsole.MarkupLine("  • [cyan]Analyze dumps[/] - 'analyze dump crash.dmp'");
            AnsiConsole.MarkupLine("  • [cyan]Fix bugs[/] - 'how do I fix memory leaks?'");
            AnsiConsole.MarkupLine("  • [cyan]Optimize code[/] - 'suggest optimizations'");
            AnsiConsole.MarkupLine("  • [cyan]Answer questions[/] - 'explain async/await'");
            AnsiConsole.WriteLine();

            var conversationHistory = new List<string>();
            var turnCount = 0;

            // Main conversation loop
            while (turnCount < settings.MaxTurns)
            {
                // Get user input
                var userInput = AnsiConsole.Ask<string>("[cyan]You:[/]");
                
                if (string.IsNullOrWhiteSpace(userInput))
                    continue;

                // Check for exit commands
                if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    userInput.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                    userInput.Equals("bye", StringComparison.OrdinalIgnoreCase))
                {
                    AnsiConsole.MarkupLine("[green]Goodbye! 👋[/]");
                    break;
                }

                // Process special commands
                string? specialResponse = null;
                
                // Read file command
                if (userInput.StartsWith("read ", StringComparison.OrdinalIgnoreCase) ||
                    userInput.Contains("show me", StringComparison.OrdinalIgnoreCase))
                {
                    specialResponse = await HandleReadFileCommand(userInput, settings.ProjectPath, fileManager);
                }
                // Analyze project command
                else if (userInput.Contains("analyze project", StringComparison.OrdinalIgnoreCase) ||
                         userInput.Contains("scan files", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(settings.ProjectPath))
                    {
                        projectContext = await AnalyzeProjectAsync(settings.ProjectPath, fileManager);
                        specialResponse = "✓ Project analyzed! I now have context about your project structure and files.";
                    }
                    else
                    {
                        specialResponse = "Please specify a project path using --project option when starting the agent.";
                    }
                }
                // Analyze dump command
                else if (userInput.Contains("analyze dump", StringComparison.OrdinalIgnoreCase))
                {
                    specialResponse = await HandleAnalyzeDumpCommand(userInput, dumpParser);
                }

                if (specialResponse != null)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[yellow]Agent:[/]");
                    AnsiConsole.WriteLine(specialResponse);
                    AnsiConsole.WriteLine();
                    conversationHistory.Add($"User: {userInput}");
                    conversationHistory.Add($"Assistant: {specialResponse}");
                    turnCount++;
                    continue;
                }

                // Add to history
                conversationHistory.Add($"User: {userInput}");

                // Build context-aware prompt
                var prompt = BuildPrompt(userInput, conversationHistory, settings.ProjectPath, projectContext);

                // Get AI response
                string aiResponse = "";
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Thinking...", async ctx =>
                    {
                        aiResponse = await ai.GenerateResponseAsync(prompt);
                        conversationHistory.Add($"Assistant: {aiResponse}");
                    });

                // Display AI response
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Agent:[/]");
                DisplayFormattedResponse(aiResponse);
                AnsiConsole.WriteLine();
                
                turnCount++;
            }

            if (turnCount >= settings.MaxTurns)
            {
                AnsiConsole.MarkupLine("[yellow]Maximum conversation turns reached.[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }
    }

    private static async Task<string> AnalyzeProjectAsync(string projectPath, IFileManager fileManager)
    {
        var structure = await fileManager.GetProjectStructureAsync(projectPath);
        return JsonSerializer.Serialize(structure, new JsonSerializerOptions { WriteIndented = true });
    }

    private static async Task<string> HandleReadFileCommand(string userInput, string? projectPath, IFileManager fileManager)
    {
        try
        {
            // Extract filename from command
            var filename = ExtractFilename(userInput);
            if (string.IsNullOrEmpty(filename))
                return "Please specify a filename. Example: 'read UserService.cs'";

            // Find file
            string? filePath = null;
            if (Path.IsPathRooted(filename) && File.Exists(filename))
            {
                filePath = filename;
            }
            else if (!string.IsNullOrEmpty(projectPath))
            {
                var files = Directory.GetFiles(projectPath, filename, SearchOption.AllDirectories);
                filePath = files.FirstOrDefault();
            }

            if (filePath == null)
                return $"File '{filename}' not found.";

            var content = await fileManager.ReadFileAsync(filePath);
            var lines = content.Split('\n');
            var preview = lines.Length > 50 
                ? string.Join('\n', lines.Take(50)) + $"\n\n... ({lines.Length - 50} more lines)"
                : content;

            return $"File: {Path.GetFileName(filePath)}\n" +
                   $"Path: {filePath}\n" +
                   $"Lines: {lines.Length}\n\n" +
                   $"```csharp\n{preview}\n```";
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }

    private static async Task<string> HandleAnalyzeDumpCommand(string userInput, IDumpParser dumpParser)
    {
        try
        {
            var filename = ExtractFilename(userInput);
            if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
                return $"Dump file '{filename}' not found. Please provide a valid dump file path.";

            AnsiConsole.MarkupLine("[yellow]Loading dump file...[/]");
            using var runtime = dumpParser.LoadDump(filename);
            
            var info = $"Dump Analysis:\n" +
                      $"- Heap segments: {runtime.Heap.Segments.Count()}\n" +
                      $"- Total heap size: {runtime.Heap.Segments.Sum(s => (long)s.Length):N0} bytes\n" +
                      $"- Can walk heap: {runtime.Heap.CanWalkHeap}\n" +
                      $"- Threads: {runtime.Threads.Count()}";

            return info;
        }
        catch (Exception ex)
        {
            return $"Error analyzing dump: {ex.Message}";
        }
    }

    private static string ExtractFilename(string input)
    {
        // Try to extract filename from various patterns
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Look for file extensions
        foreach (var word in words)
        {
            if (word.Contains('.'))
                return word.Trim('"', '\'');
        }

        // Look after "read" or "show"
        var readIndex = Array.FindIndex(words, w => 
            w.Equals("read", StringComparison.OrdinalIgnoreCase) ||
            w.Equals("show", StringComparison.OrdinalIgnoreCase));
        
        if (readIndex >= 0 && readIndex < words.Length - 1)
            return words[readIndex + 1].Trim('"', '\'');

        return "";
    }

    private static string? GetApiKeyFromEnvironment(string provider)
    {
        var envVar = $"MEMGUARD_{provider.ToUpper()}_KEY";
        return Environment.GetEnvironmentVariable(envVar);
    }

    private static string BuildPrompt(string userInput, List<string> history, string? projectPath, string? projectContext)
    {
        var prompt = new System.Text.StringBuilder();
        
        prompt.AppendLine("You are MemGuard AI Agent, an expert .NET development assistant.");
        prompt.AppendLine("You help developers analyze, fix, and optimize their .NET projects.");
        prompt.AppendLine();
        prompt.AppendLine("Your capabilities:");
        prompt.AppendLine("- Analyze code for bugs, memory leaks, and performance issues");
        prompt.AppendLine("- Suggest code fixes and optimizations");
        prompt.AppendLine("- Explain complex code");
        prompt.AppendLine("- Generate unit tests");
        prompt.AppendLine("- Answer questions about .NET development");
        prompt.AppendLine();

        if (!string.IsNullOrEmpty(projectPath))
        {
            prompt.AppendLine($"Current project: {projectPath}");
            if (!string.IsNullOrEmpty(projectContext))
            {
                prompt.AppendLine("Project structure:");
                prompt.AppendLine(projectContext);
            }
            prompt.AppendLine();
        }

        // Add recent conversation history (last 10 turns)
        if (history.Count > 0)
        {
            prompt.AppendLine("Recent conversation:");
            foreach (var msg in history.TakeLast(10))
            {
                prompt.AppendLine(msg);
            }
            prompt.AppendLine();
        }

        prompt.AppendLine($"User's current request: {userInput}");
        prompt.AppendLine();
        prompt.AppendLine("Provide a helpful, concise response. If suggesting code changes, use markdown code blocks.");
        
        return prompt.ToString();
    }

    private static void DisplayFormattedResponse(string response)
    {
        // Simple markdown-style formatting
        var lines = response.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.StartsWith("**") && line.EndsWith("**"))
            {
                // Bold headers
                var text = line.Trim('*');
                AnsiConsole.MarkupLine($"[bold]{text.EscapeMarkup()}[/]");
            }
            else if (line.StartsWith("```"))
            {
                // Code blocks
                AnsiConsole.MarkupLine("[grey]{0}[/]", line.EscapeMarkup());
            }
            else if (line.StartsWith("- ") || line.StartsWith("• "))
            {
                // Bullet points
                AnsiConsole.MarkupLine($"  [green]•[/] {line.TrimStart('-', '•', ' ').EscapeMarkup()}");
            }
            else if (line.StartsWith("#"))
            {
                // Headers
                var text = line.TrimStart('#', ' ');
                AnsiConsole.MarkupLine($"[bold cyan]{text.EscapeMarkup()}[/]");
            }
            else
            {
                // Regular text
                AnsiConsole.WriteLine(line);
            }
        }
    }
}
