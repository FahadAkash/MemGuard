# MemGuard - All Commands Quick Reference

## 📋 Essential Commands

### Analysis
```bash
# Basic analysis
memguard analyze crash.dmp --provider gemini --api-key YOUR_KEY

# With security detection + visualization
memguard analyze crash.dmp --provider gemini --api-key YOUR_KEY --visualize

# Export as JSON
memguard analyze crash.dmp --provider gemini --api-key YOUR_KEY --export-json --output report.json

# Export as PDF
memguard analyze crash.dmp --provider gemini --api-key YOUR_KEY --export-pdf --output report.pdf

# Compliance mode
memguard analyze crash.dmp --provider claude --api-key YOUR_KEY --compliance hipaa --redact-pii
```

### Auto-Fix
```bash
# Preview fixes
memguard fix crash.dmp --project ./MyApp --dry-run --provider gemini --api-key YOUR_KEY

# Apply fixes
memguard fix crash.dmp --project ./MyApp --provider claude --api-key YOUR_KEY
```

### Compare Dumps
```bash
# Console output
memguard compare baseline.dmp current.dmp

# Markdown report
memguard compare baseline.dmp current.dmp --output regression.md
```

### Live Monitoring
```bash
# Monitor process
memguard monitor --process MyApp --interval 5

# With alerts
memguard monitor --process MyApp --alert-threshold 500 --output metrics.json
```

### AI Agent
```bash
# Interactive mode
memguard agent --project ./MyApp --provider gemini --api-key YOUR_KEY

# Autonomous mode
memguard agent --project ./MyApp --autonomous --provider claude --api-key YOUR_KEY
```

---

## 🛡️ Security Commands

```bash
# Full security scan (IOC + YARA + Exploit detection)
memguard analyze suspect.dmp --provider claude --api-key YOUR_KEY

# Export to SIEM
memguard analyze suspect.dmp --provider gemini --api-key YOUR_KEY --export-siem splunk
```

---

## 🔌 Plugin Commands

### Build Plugin
```bash
cd examples/SampleDetectorPlugin
dotnet build -c Release
```

### Deploy Plugin
```bash
# Windows
copy bin\Release\net8.0\SampleDetectorPlugin.dll %USERPROFILE%\.memguard\plugins\

# Linux/Mac
cp bin/Release/net8.0/SampleDetectorPlugin.dll ~/.memguard/plugins/
```

---

## 🧪 Testing Commands

```bash
# Run all tests
dotnet test

# Run extraction tests only
dotnet test --filter "FullyQualifiedName~Extractors"

# Build solution
dotnet build src\MemGuard.sln
```

---

## 📁 File Locations

| Item | Windows | Linux/Mac |
|------|---------|-----------|
| Plugins | `%USERPROFILE%\.memguard\plugins` | `~/.memguard/plugins` |
| YARA Rules | `%USERPROFILE%\.memguard\yara-rules` | `~/.memguard/yara-rules` |
| Backups | Project `.memguard-backups` folder | Project `.memguard-backups` folder |
| Audit Logs | `%USERPROFILE%\.memguard\audit-trail` | `~/.memguard/audit-trail` |

---

## 🔑 API Keys

### Via Command Line
```bash
memguard analyze crash.dmp --provider gemini --api-key YOUR_KEY_HERE
```

### Via Environment Variables (Recommended)
```powershell
# Windows (PowerShell)
$env:MEMGUARD_GEMINI_KEY="your_gemini_key"
$env:MEMGUARD_CLAUDE_KEY="your_claude_key"
$env:MEMGUARD_GROK_KEY="your_grok_key"
$env:MEMGUARD_DEEPSEEK_KEY="your_deepseek_key"
```

```bash
# Linux/Mac
export MEMGUARD_GEMINI_KEY="your_gemini_key"
export MEMGUARD_CLAUDE_KEY="your_claude_key"
export MEMGUARD_GROK_KEY="your_grok_key"
export MEMGUARD_DEEPSEEK_KEY="your_deepseek_key"
```

Then use without `--api-key`:
```bash
memguard analyze crash.dmp --provider gemini
```

---

## 📚 Documentation Links

- **[Complete Command Reference](docs/COMMANDS.md)** - All options and advanced usage
- **[Workflows Guide](docs/WORKFLOWS.md)** - 10 common usage scenarios
- **[YARA Rules](docs/YARA-RULES.md)** - Custom rule creation
- **[Plugin Development](examples/SampleDetectorPlugin/README.md)** - Build custom detectors
- **[Main README](README.md)** - Full documentation

---

## 🚀 Quick Start Examples

### 1. Analyze a Crash Dump
```bash
# Collect dump
dotnet-dump collect -p <process-id> -o crash.dmp

# Analyze with AI
memguard analyze crash.dmp --provider gemini --api-key YOUR_KEY

# Apply suggested fixes
memguard fix crash.dmp --project ./MyApp --provider claude --api-key YOUR_KEY
```

### 2. Security Investigation
```bash
# Comprehensive security analysis
memguard analyze suspect.dmp --provider claude --api-key YOUR_KEY --visualize

# Export for team
memguard analyze suspect.dmp --provider claude --api-key YOUR_KEY --export-json
```

### 3. Performance Regression Testing
```bash
# Take baseline
dotnet-dump collect -p <pid> -o v1.0.dmp

# After changes
dotnet-dump collect -p <pid> -o v1.1.dmp

# Compare
memguard compare v1.0.dmp v1.1.dmp --output regression-report.md
```

### 4. Live Monitoring During Load Test
```bash
# Start monitoring
memguard monitor --process MyApp --interval 5 --alert-threshold 500 &

# Run load tests
# ...

# Stop (Ctrl+C) and review alerts
```

### 5. Custom Plugin Development
```bash
# Create plugin (use template from examples/SampleDetectorPlugin)
dotnet new classlib -n MyDetector
# Edit .csproj and code

# Build
dotnet build -c Release

# Deploy
copy bin\Release\net8.0\MyDetector.dll %USERPROFILE%\.memguard\plugins\

# Test
memguard analyze test.dmp --provider gemini --api-key YOUR_KEY
```

---

## 💡 Tips

1. **Use Claude** for complex code analysis
2. **Use Gemini** for fast results
3. **Always `--redact-pii`** for production dumps
4. **Create custom YARA rules** for your threat landscape
5. **Monitor regularly** during development
6. **Compare dumps** across releases
7. **Build plugins** for custom detection logic
8. **Use `--dry-run`** before applying fixes

---

**For detailed information, see the full documentation links above.**

**GitHub:** https://github.com/FahadAkash/MemGuard
