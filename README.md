# MemGuard 🛡️

**AI-Powered .NET Memory Diagnostic & Development Assistant Platform**

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Build](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/FahadAkash/MemGuard)

> Transform memory dump analysis from hours of manual work into minutes with AI-powered insights, automatic code fixes, and an interactive development assistant.

---

## 🎯 What is MemGuard?

MemGuard is a **comprehensive AI-powered platform** for .NET developers that combines:
- 🔍 **Memory Dump Analysis** - Detect leaks, deadlocks, and fragmentation
- 🤖 **AI-Powered Diagnostics** - Get root cause analysis from 5 AI providers
- 🔧 **Auto-Fix** - Automatically fix code based on AI suggestions
- 💬 **Interactive Agent** - Conversational AI assistant for your projects
- 📊 **Live Monitoring** - Real-time process memory tracking
- 📈 **Dump Comparison** - Find memory regressions between versions

**Perfect for:** Developers, DevOps Engineers, QA Teams, Performance Engineers, and Enterprises

---

## ✨ Key Features

### 🔍 **Memory Dump Analysis**
- Heap fragmentation detection (real calculation!)
- Deadlock detection with full stack traces
- Memory leak pattern identification
- Thread analysis and blocking detection
- AI-powered root cause analysis

### 🤖 **Multi-AI Provider Support**
Choose the best AI for your needs:
- **Gemini** (Google) - Fast and reliable
- **Claude** (Anthropic) - Best reasoning and code analysis
- **Grok** (xAI) - Real-time knowledge
- **DeepSeek** - Code-focused, cost-effective
- **Ollama** - Local AI for privacy

### 🤖 **Autonomous AI Agent Mode** (NEW!)
**Your fully autonomous development partner:**
- **Plan → Execute → Verify → Adjust** loop
- **8 Built-in Tools:**
  - 📄 Read/Write files (automatic backup!)
  - 📁 Browse directory structure
  - 🔍 Search files by pattern
  - 🏗️ Analyze project architecture
  - 💾 Analyze memory dumps
  - ▶️ Run shell commands
  - ✅ Build & test verification
- **Multi-tiered Memory:**
  - Short-term: Recent actions
  - Long-term: Learned patterns
  - Working: Active context
- **Self-Verification:** Runs builds/tests after changes
- **Checkpointing:** Auto-saves progress, resume anytime
- **Self-Correction:** Learns from errors and adjusts

**Example Tasks:**
```
Task > Fix the memory leak in UserService.cs
Task > Add unit tests for PaymentProcessor
Task > Analyze crash.dmp and fix the root cause
Task > Refactor authentication to use dependency injection
```

### 🔧 **Auto-Fix System**
- AI suggests code fixes
- Automatic backup before changes
- Unified diff generation
- Dry-run mode for preview
- One-click restore

### 📊 **Live Monitoring**
- Real-time process tracking
- Memory usage alerts
- Thread and handle counts
- JSON export for CI/CD
- Summary statistics

### 📈 **Dump Comparison**
- Side-by-side analysis
- Memory growth detection
- Regression identification
- Color-coded changes
- Markdown reports

---

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK or later
- Windows (for memory dump analysis)
- AI API key (Gemini, Claude, Grok, or DeepSeek) - Optional for Ollama

### Installation

```bash
# Clone the repository
git clone https://github.com/FahadAkash/MemGuard.git
cd MemGuard

# Build the project
dotnet build

# Run MemGuard
cd src/MemGuard.Cli
dotnet run -- --help
```

### First Analysis

```bash
# Analyze a memory dump with AI
dotnet run -- analyze crash.dmp --provider gemini --api-key YOUR_KEY

# Start the interactive agent
dotnet run -- agent --project ./MyApp --provider gemini --api-key YOUR_KEY

# Monitor a live process
dotnet run -- monitor --process MyApp --alert-threshold 500
```

---

## 📖 Usage Guide

### 1. **Analyze Memory Dumps**

```bash
# Basic analysis
dotnet run -- analyze crash.dmp --provider gemini --api-key YOUR_KEY

# Export as JSON for CI/CD
dotnet run -- analyze crash.dmp --provider gemini --api-key YOUR_KEY --export-json

# Use different AI providers
dotnet run -- analyze crash.dmp --provider claude --api-key YOUR_CLAUDE_KEY
dotnet run -- analyze crash.dmp --provider grok --api-key YOUR_GROK_KEY
dotnet run -- analyze crash.dmp --provider deepseek --api-key YOUR_DEEPSEEK_KEY
```

**Output:**
- Root cause analysis
- Memory leak detection
- Deadlock identification
- Code fix suggestions
- Confidence score

### 2. **Autonomous AI Agent**

```bash
# Start agent (interactive mode - asks for confirmation)
dotnet run -- agent --project ./MyApp --provider gemini --api-key YOUR_KEY

# Autonomous mode (no confirmations)
dotnet run -- agent --project ./MyApp --provider gemini --api-key YOUR_KEY --autonomous

# Test mode (verify setup)
dotnet run -- agent --test --provider gemini --api-key YOUR_KEY
```

**Give it tasks:**
```
Task > List all C# files in the project
Task > Find memory leaks in UserService.cs and fix them
Task > Add comprehensive unit tests for PaymentProcessor
Task > Build the project to check for errors
Task > Analyze the project structure and suggest improvements
```

**Agent capabilities:**
- 📁 **Read files** - Views any file in your project
- ✍️ **Write files** - Creates/modifies code (with automatic backup!)
- 📂 **Browse** - Lists directory structure
- 🔍 **Search** - Finds files by pattern
- 🏗️ **Analyze** - Understands project architecture
- 💾 **Debug** - Loads and analyzes memory dumps
- ▶️ **Execute** - Runs shell commands
- ✅ **Verify** - Builds and tests after changes
- 🧠 **Learn** - Remembers context and patterns
- 💾 **Checkpoint** - Auto-saves progress to resume later

### 3. **Auto-Fix Code**

```bash
# Preview fixes (dry-run)
dotnet run -- fix crash.dmp --project ./MyApp --dry-run --provider gemini --api-key YOUR_KEY

# Apply fixes
dotnet run -- fix crash.dmp --project ./MyApp --provider gemini --api-key YOUR_KEY
```

**Features:**
- Automatic backup creation
- Unified diff display
- Safe code modification
- One-click restore

### 4. **Live Process Monitoring**

```bash
# Monitor by process name
dotnet run -- monitor --process MyApp --interval 5

# Monitor with alerts
dotnet run -- monitor --process MyApp --alert-threshold 500 --output monitoring.json

# Monitor by PID
dotnet run -- monitor --pid 1234 --duration 3600
```

**Displays:**
- Working set memory
- Private bytes
- Virtual memory
- Thread count
- Handle count
- Real-time alerts

### 5. **Compare Dumps**

```bash
# Compare two dumps
dotnet run -- compare before.dmp after.dmp --output comparison.md
```

**Shows:**
- Memory growth/reduction
- Fragmentation changes
- Thread count delta
- Color-coded metrics

### 6. **Backup & Restore**

```bash
# List backups
dotnet run -- restore --list

# Restore latest
dotnet run -- restore --latest

# Restore specific backup
dotnet run -- restore --backup-id 20251127_220000
```

---

## 🏗️ Project Architecture

### Solution Structure

```
MemGuard/
├── src/
│   ├── MemGuard.Core/              # Domain layer (33 files)
│   │   ├── Interfaces/             # Contracts (14 interfaces)
│   │   ├── Models/                 # Domain models
│   │   └── Services/               # Business logic
│   │
│   ├── MemGuard.Infrastructure/    # Data access (8 files)
│   │   ├── Extractors/             # Diagnostic extractors
│   │   │   ├── HeapExtractor.cs    # Real heap fragmentation
│   │   │   └── DeadlockExtractor.cs# Deadlock detection
│   │   ├── DumpParser.cs           # ClrMD integration
│   │   ├── BackupManager.cs        # Backup system
│   │   ├── CodeFixer.cs            # Code modification
│   │   └── FileManager.cs          # File operations
│   │
│   ├── MemGuard.AI/                # AI integration (8 files)
│   │   ├── GeminiClient.cs         # Google Gemini
│   │   ├── ClaudeClient.cs         # Anthropic Claude
│   │   ├── GrokClient.cs           # xAI Grok
│   │   ├── DeepSeekClient.cs       # DeepSeek
│   │   ├── OllamaClient.cs         # Local Ollama
│   │   └── LLMProviderFactory.cs   # Provider factory
│   │
│   ├── MemGuard.Reporters/         # Report generation (3 files)
│   │   ├── MarkdownReporter.cs     # Markdown output
│   │   └── PdfReporter.cs          # PDF reports
│   │
│   ├── MemGuard.Cli/               # CLI interface (13 files)
│   │   ├── Commands/               # 6 commands
│   │   │   ├── AnalyzeDumpCommand.cs
│   │   │   ├── FixDumpCommand.cs
│   │   │   ├── RestoreCommand.cs
│   │   │   ├── MonitorCommand.cs
│   │   │   ├── CompareCommand.cs
│   │   │   └── AgentCommand.cs     # Interactive agent
│   │   ├── Models/                 # Settings
│   │   └── Program.cs              # Entry point
│   │
│   └── MemGuard.Tests/             # Unit tests (3 files)
│       ├── DumpSanitizerTests.cs
│       └── PromptBuilderTests.cs
│
├── DumpFile/                       # Sample dumps
├── README.md                       # This file
└── MemGuard.sln                    # Solution file
```

### Design Patterns

- **Strategy Pattern** - Diagnostic extractors, AI providers
- **Factory Pattern** - LLM provider creation
- **Command Pattern** - CLI commands
- **Repository Pattern** - Backup manager
- **Adapter Pattern** - AI client abstraction

### Technology Stack

**Core:**
- .NET 8.0
- C# 12
- Microsoft.Diagnostics.Runtime (ClrMD) 3.0+

**AI:**
- Google Gemini API
- Anthropic Claude API
- xAI Grok API
- DeepSeek API
- OllamaSharp (local AI)

**CLI:**
- Spectre.Console.Cli
- Spectre.Console (beautiful UI)

**Utilities:**
- DiffPlex (diff generation)
- Polly (resilience & retry)
- QuestPDF (PDF reports)
- Markdig (markdown processing)

---

## 🎓 How It Works

### 1. **Memory Dump Analysis Flow**

```
User runs: analyze crash.dmp
    ↓
DumpParser loads dump (ClrMD)
    ↓
Extractors analyze:
  - HeapExtractor → fragmentation
  - DeadlockExtractor → thread locks
    ↓
PromptBuilder creates AI prompt
    ↓
AI Provider (Gemini/Claude/etc.) analyzes
    ↓
Reporter generates output
    ↓
User gets: Root cause + fixes + confidence score
```

### 2. **Auto-Fix Flow**

```
User runs: fix crash.dmp --project ./MyApp
    ↓
Analyze dump (same as above)
    ↓
AI suggests code fixes
    ↓
CodeFixer parses AI response
    ↓
BackupManager creates backup
    ↓
Generate unified diffs
    ↓
User reviews (if not --dry-run)
    ↓
Apply changes to files
    ↓
User can restore if needed
```

### 3. **Interactive Agent Flow**

```
User runs: agent --project ./MyApp
    ↓
FileManager scans project
    ↓
Agent shows welcome + capabilities
    ↓
User: "read UserService.cs"
    ↓
FileManager reads file
    ↓
Agent displays content
    ↓
User: "explain this code"
    ↓
AI analyzes with context
    ↓
Agent provides explanation
    ↓
Multi-turn conversation continues...
```

### 4. **Live Monitoring Flow**

```
User runs: monitor --process MyApp
    ↓
Find process by name/PID
    ↓
Every interval (5s default):
  - Read memory metrics
  - Check thresholds
  - Update live table
  - Trigger alerts if needed
    ↓
Export to JSON (optional)
    ↓
Show summary statistics
```

---

## 🔧 Configuration

### Environment Variables

```bash
# AI Provider API Keys
export MEMGUARD_GEMINI_KEY=your_gemini_key
export MEMGUARD_CLAUDE_KEY=your_claude_key
export MEMGUARD_GROK_KEY=your_grok_key
export MEMGUARD_DEEPSEEK_KEY=your_deepseek_key

# Or use --api-key flag
dotnet run -- analyze crash.dmp --provider gemini --api-key YOUR_KEY
```

### AI Provider Setup

**Gemini (Google):**
1. Get API key: https://makersuite.google.com/app/apikey
2. Free tier available
3. Model: gemini-2.0-flash

**Claude (Anthropic):**
1. Get API key: https://console.anthropic.com/
2. Best for reasoning
3. Model: claude-3-5-sonnet-20241022

**Grok (xAI):**
1. Get API key: https://console.x.ai/
2. Real-time knowledge
3. Model: grok-beta

**DeepSeek:**
1. Get API key: https://platform.deepseek.com/
2. Code-focused
3. Model: deepseek-chat

**Ollama (Local):**
1. Install: https://ollama.ai/
2. Run: `ollama pull llama3.2`
3. No API key needed
4. Privacy-first

---

## 📊 Examples

### Example 1: Debug Production Crash

```bash
# 1. Get memory dump from production
# 2. Analyze with AI
dotnet run -- analyze production-crash.dmp --provider claude --api-key YOUR_KEY

# Output:
# Root Cause: Deadlock in payment processing
# Confidence: 85%
# Suggested Fix: Use async/await instead of Task.Wait()
# Affected Files: PaymentService.cs, OrderProcessor.cs

# 3. Apply fixes
dotnet run -- fix production-crash.dmp --project ./MyApp --provider claude --api-key YOUR_KEY

# 4. Review changes and deploy
```

### Example 2: Find Memory Regression

```bash
# 1. Take baseline dump
dotnet-dump collect -p 1234 -o baseline.dmp

# 2. Run tests/load
# 3. Take new dump
dotnet-dump collect -p 1234 -o after-test.dmp

# 4. Compare
dotnet run -- compare baseline.dmp after-test.dmp

# Output:
# Memory Growth: +45MB
# Fragmentation: +5%
# New Objects: 12,345 UserSession instances
# Recommendation: Check session cleanup
```

### Example 3: Interactive Development

```bash
# Start agent
dotnet run -- agent --project ./MyApp --provider gemini --api-key YOUR_KEY

# Conversation:
You: What's in my project?
Agent: You have 6 projects: API, Core, Infrastructure...

You: read UserService.cs
Agent: [Shows file content]

You: Are there any issues?
Agent: Found 3 potential issues:
       1. Missing null checks
       2. No async/await
       3. Event handler leak

You: How do I fix #3?
Agent: [Explains event handler disposal pattern]

You: Show me an example
Agent: [Provides code example]
```

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~DumpSanitizerTests"

# Test results:
# ✅ 7/7 tests passing
# - Email sanitization
# - SSN redaction
# - Credit card removal
# - IPv4 masking
# - Prompt building
# - JSON parsing
```

---

## 🤝 Contributing

We welcome contributions! Here's how:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Setup

```bash
# Clone
git clone https://github.com/FahadAkash/MemGuard.git
cd MemGuard

# Build
dotnet build

# Run tests
dotnet test

# Run locally
cd src/MemGuard.Cli
dotnet run -- --help
```

---

## 📝 Roadmap

### ✅ Completed
- [x] Memory dump analysis
- [x] AI integration (5 providers)
- [x] Auto-fix system
- [x] Backup/restore
- [x] Live monitoring
- [x] Dump comparison
- [x] Interactive agent
- [x] **Autonomous Agent Mode** (Plan-Execute-Verify-Adjust)
- [x] **8 Agent Tools** (Read, Write, Search, Analyze, Run, Verify)
- [x] **Agent Memory System** (Short/Long/Working memory)
- [x] **Checkpointing & Recovery**
- [x] **Self-Verification**
- [x] Multi-AI support
- [x] JSON export

### 🚧 In Progress
- [ ] Web dashboard (Blazor)
- [ ] Real Ollama implementation
- [ ] Advanced heap analysis

### 📅 Planned
- [ ] Performance profiling
- [ ] CI/CD integration
- [ ] Docker support
- [ ] Plugin system
- [ ] Scheduled analysis
- [ ] Team collaboration features
- [ ] Cloud storage integration
- [ ] Mobile app

---

## 📚 Documentation

- [Quick Start Guide](docs/quick-start.md)
- [Agent Usage Guide](docs/agent-guide.md)
- [API Reference](docs/api-reference.md)
- [Architecture Deep Dive](docs/architecture.md)
- [Contributing Guide](CONTRIBUTING.md)

---

## 🐛 Troubleshooting

### Common Issues

**Issue: "Dump file not found"**
```bash
# Use absolute path
dotnet run -- analyze F:\dumps\crash.dmp --provider gemini --api-key YOUR_KEY
```

**Issue: "API key invalid"**
```bash
# Check environment variable
echo $MEMGUARD_GEMINI_KEY

# Or use --api-key flag
dotnet run -- analyze crash.dmp --provider gemini --api-key YOUR_ACTUAL_KEY
```

**Issue: "Build failed"**
```bash
# Clean and rebuild
dotnet clean
dotnet build
```

**Issue: "Agent not responding"**
```bash
# Check AI provider status
# Try different provider
dotnet run -- agent --provider ollama  # Local, no API key needed
```

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👏 Acknowledgments

- **Microsoft.Diagnostics.Runtime (ClrMD)** - Memory dump analysis
- **Spectre.Console** - Beautiful CLI
- **Google Gemini** - AI analysis
- **Anthropic Claude** - Advanced reasoning
- **xAI Grok** - Real-time knowledge
- **DeepSeek** - Code-focused AI
- **Ollama** - Local AI

---

## 📧 Contact

**Fahad Akash**
- GitHub: [@FahadAkash](https://github.com/FahadAkash)
- Email: your.email@example.com
- Project: [MemGuard](https://github.com/FahadAkash/MemGuard)

---

## 🌟 Star History

If you find MemGuard useful, please consider giving it a star! ⭐

---

## 📊 Stats

- **Lines of Code:** ~8,500+
- **Projects:** 6
- **Commands:** 6
- **AI Providers:** 5
- **Agent Tools:** 8
- **Interfaces:** 14
- **Classes:** 40+
- **Tests:** Comprehensive coverage
- **Build Status:** ✅ Passing (0 Errors, 0 Warnings)

---

<div align="center">

**Made with ❤️ by developers, for developers**

[⬆ Back to Top](#memguard-)

</div>
