# All MemGuard Commands - Complete Reference

## Quick Command Index

```bash
# Analysis Commands
memguard analyze <dump> [options]           # Analyze memory dump
memguard fix <dump> [options]              # Auto-fix code issues
memguard compare <dump1> <dump2> [options] # Compare two dumps

# Monitoring Commands
memguard monitor [options]                  # Live process monitoring

# Agent Commands
memguard agent [options]                    # Start AI development assistant

# Utility Commands
memguard restore [options]                  # Restore backups
memguard --help                            # Show help
memguard --version                         # Show version
```

---

## Detailed Command Reference

### `analyze` - Memory Dump Analysis

Analyze .NET memory dumps with AI-powered insights and security detection.

```bash
memguard analyze <dump-file> [options]
```

#### Required Arguments
| Argument | Description |
|----------|-------------|
| `<dump-file>` | Path to .NET memory dump file (.dmp) |

#### Options
| Option | Description | Default |
|--------|-------------|---------|
| `--provider <name>` | AI provider: gemini, claude, grok, deepseek, ollama | gemini |
| `--api-key <key>` | API key for AI provider | (required) |
| `--model <name>` | Specific AI model to use | Provider default |
| `--export-json` | Export results as JSON | false |
| `--export-pdf` | Generate PDF report | false |
| `--output <path>` | Output file path | Console |
| `--visualize` | Enable ASCII art visualization |  false |
| `--compliance <framework>` | Compliance mode: hipaa, soc2, pci | none |
| `--redact-pii` | Redact PII/PHI before analysis | false |
| `--export-siem <type>` | Export to SIEM: splunk, sentinel, syslog | none |

#### Examples

**Basic Analysis:**
```bash
memguard analyze crash.dmp --provider gemini --api-key YOUR_KEY
```

**With Visualization:**
```bash
memguard analyze crash.dmp --provider gemini --api-key YOUR_KEY --visualize
```

**Compliance Mode with PII Redaction:**
```bash
memguard analyze production.dmp \
  --provider claude \
  --api-key YOUR_KEY \
  --compliance hipaa \
  --redact-pii \
  --export-pdf \
  --output hipaa-report.pdf
```

**JSON Export for CI/CD:**
```bash
memguard analyze crash.dmp \
  --provider gemini \
  --api-key YOUR_KEY \
  --export-json \
  --output analysis.json
```

**Security Analysis:**
```bash
memguard analyze suspect.dmp \
  --provider claude \
  --api-key YOUR_KEY \
  --export-siem splunk
```

#### Output Includes
- ✅ Root cause analysis (AI-powered)
- ✅ Memory leak detection
- ✅ Deadlock identification
- ✅ IOC detection (malicious IPs, domains, mutexes)
- ✅ YARA rule matches
- ✅ Exploit heuristics (ROP chains, shellcode, ETW tampering)
- ✅ Heap fragmentation analysis
- ✅ Thread analysis
- ✅ Code fix suggestions
- ✅ Confidence score

---

### `fix` - Auto-Fix Code

Automatically generate and apply code fixes based on dump analysis.

```bash
memguard fix <dump-file> [options]
```

#### Required Arguments
| Argument | Description |
|----------|-------------|
| `<dump-file>` | Path to memory dump file |

#### Options
| Option | Description | Default |
|--------|-------------|---------|
| `--provider <name>` | AI provider | gemini |
| `--api-key <key>` | API key | (required) |
| `--project <path>` | Project directory | (required) |
| `--dry-run` | Preview fixes without applying | false |
| `--auto-approve` | Apply fixes without confirmation | false |

#### Examples

**Preview Fixes (Dry Run):**
```bash
memguard fix crash.dmp \
  --project ./MyApp \
  --dry-run \
  --provider gemini \
  --api-key YOUR_KEY
```

**Apply Fixes with Confirmation:**
```bash
memguard fix crash.dmp \
  --project ./MyApp \
  --provider claude \
  --api-key YOUR_KEY
```

**Auto-Approve All Fixes:**
```bash
memguard fix crash.dmp \
  --project ./MyApp \
  --auto-approve \
  --provider gemini \
  --api-key YOUR_KEY
```

#### Features
- ✅ Automatic backup creation
- ✅ Unified diff display
- ✅ Safe code modification
- ✅ One-click rollback
- ✅ All affected files shown

---

### `compare` - Dump Comparison

Compare two memory dumps to find regressions and memory growth.

```bash
memguard compare <dump1> <dump2> [options]
```

####  Required Arguments
| Argument | Description |
|----------|-------------|
| `<dump1>` | Baseline dump file |
| `<dump2>` | Comparison dump file |

#### Options
| Option | Description | Default |
|--------|-------------|---------|
| `--output <path>` | Report output path | Console |
| `--format <type>` | Output format: markdown, json | markdown |

#### Examples

**Console Output:**
```bash
memguard compare baseline.dmp after-test.dmp
```

**Markdown Report:**
```bash
memguard compare v1.0.dmp v1.1.dmp \
  --output regression-report.md \
  --format markdown
```

**JSON Export:**
```bash
memguard compare before.dmp after.dmp \
  --format json \
  --output comparison.json
```

#### Output Includes
- Memory growth/reduction percentage
- Fragmentation delta
- Thread count changes
- New object types
- Color-coded metrics

---

### `monitor` - Live Process Monitoring

Monitor .NET processes in real-time with memory alerts.

```bash
memguard monitor [options]
```

#### Options
| Option | Description | Default |
|--------|-------------|---------|
| `--process <name>` | Process name to monitor | - |
| `--pid <number>` | Process ID to monitor | - |
| `--interval <seconds>` | Sampling interval | 5 |
| `--duration <seconds>` | Monitoring duration (0=forever) | 0 |
| `--alert-threshold <mb>` | Memory alert threshold (MB) | - |
| `--output <path>` | JSON export path | - |

#### Examples

**Monitor by Process Name:**
```bash
memguard monitor --process MyApp --interval 5
```

**With Memory Alerts:**
```bash
memguard monitor \
  --process MyApp \
  --alert-threshold 500 \
  --output monitoring.json
```

**Monitor by PID for 1 Hour:**
```bash
memguard monitor --pid 1234 --duration 3600
```

**Real-Time Dashboard:**
```bash
memguard monitor \
  --process MyApp \
  --interval 2 \
  --alert-threshold 400
```

#### Displays
- Working set memory
- Private bytes
- Virtual memory
- Thread count
- Handle count
- Real-time alerts
- Summary statistics

---

### `agent` - AI Development Assistant

Start an autonomous AI agent for interactive development tasks.

```bash
memguard agent [options]
```

#### Options
| Option | Description | Default |
|--------|-------------|---------|
| `--provider <name>` | AI provider | gemini |
| `--api-key <key>` | API key | (required) |
| `--project <path>` | Project directory | Current directory |
| `--autonomous` | No confirmations (runs autonomously) | false |
| `--max-turns <n>` | Maximum agent iterations | 50 |
| `--test` | Test mode (verify setup) | false |

#### Examples

**Interactive Mode:**
```bash
memguard agent --project ./MyApp --provider gemini --api-key YOUR_KEY
```

**Autonomous Mode:**
```bash
memguard agent \
  --project ./MyApp \
  --autonomous \
  --provider claude \
  --api-key YOUR_KEY
```

**Test Agent Setup:**
```bash
memguard agent --test --provider gemini --api-key YOUR_KEY
```

#### Available Tools
- 📄 `read_file` - Read files
- ✍️ `write_file` - Create/modify files (with backup)
- 📂 `list_directory` - Browse directories
- 🔍 `search_files` - Find files by pattern
- 🏗️ `analyze_project` - Understand architecture
- 💾 `analyze_dump` - Load memory dumps
- ▶️ `run_command` - Execute shell commands
- ✅ `verify_changes` - Build & test
- 🔙 `restore_backup` - Undo changes

#### Sample Tasks
```
Task > Fix memory leaks in UserService.cs
Task > Add unit tests for PaymentProcessor
Task > Analyze crash.dmp and fix the root cause
Task > Build the project and check for errors
```

---

### `restore` - Backup Management

Manage and restore file backups created by the agent.

```bash
memguard restore [options]
```

#### Options
| Option | Description | Default |
|--------|-------------|---------|
| `--list` | List all available backups | - |
| `--latest` | Restore latest backup | - |
| `--backup-id <id>` | Restore specific backup | - |
| `--project <path>` | Project directory | Current directory |

#### Examples

**List All Backups:**
```bash
memguard restore --list
```

**Restore Latest:**
```bash
memguard restore --latest --project ./MyApp
```

**Restore Specific Backup:**
```bash
memguard restore --backup-id 20251127_220000
```

---

## Environment Variables

Set API keys via environment variables:

```bash
# Windows (PowerShell)
$env:MEMGUARD_GEMINI_KEY="your_key_here"
$env:MEMGUARD_CLAUDE_KEY="your_key_here"
$env:MEMGUARD_GROK_KEY="your_key_here"
$env:MEMGUARD_DEEPSEEK_KEY="your_key_here"

# Linux/Mac
export MEMGUARD_GEMINI_KEY="your_key_here"
export MEMGUARD_CLAUDE_KEY="your_key_here"
export MEMGUARD_GROK_KEY="your_key_here"
export MEMGUARD_DEEPSEEK_KEY="your_key_here"
```

Then use without `--api-key`:
```bash
memguard analyze crash.dmp --provider gemini
```

---

## Advanced Usage Examples

### Security Investigation Workflow
```bash
# 1. Analyze with security focus
memguard analyze suspect.dmp \
  --provider claude \
  --api-key YOUR_KEY \
  --visualize

# 2. Export for security team
memguard analyze suspect.dmp \
  --provider claude \
  --api-key YOUR_KEY \
  --export-json \
  --output security-report.json
```

### CI/CD Integration
```bash
# In your pipeline
dotnet test --collect:"dump"

# Analyze any crash dumps
for dump in **/*.dmp; do
  memguard analyze $dump \
    --provider gemini \
    --api-key $MEMGUARD_KEY \
    --export-json
done
```

### Performance Regression Testing
```bash
# Compare releases
memguard compare v1.0-baseline.dmp v1.1-current.dmp \
  --format json \
  --output regression.json

# Fail build if memory growth > 20%
# Parse regression.json and check thresholds
```

### Compliance Audit
```bash
# Generate HIPAA-compliant report
memguard analyze production.dmp \
  --compliance hipaa \
  --redact-pii \
  --export-pdf \
  --output hipaa-audit-report.pdf
```

---

## Plugin Commands

### Build Sample Plugin
```bash
cd examples/SampleDetectorPlugin
dotnet build -c Release
```

### Deploy Plugin
```bash
# Windows
copy bin\Release\net8.0\*.dll %USERPROFILE%\.memguard\plugins\

# Linux/Mac
cp bin/Release/net8.0/*.dll ~/.memguard/plugins/
```

### Verify Plugin Loaded
```bash
memguard analyze any.dmp --provider gemini --api-key KEY
# Output will show: "Loaded detector plugin: PluginName v1.0.0"
```

---

## Tips & Tricks

**Best Practices:**
1. Use `--dry-run` before applying fixes
2. Always set `--redact-pii` for production dumps
3. Use environment variables for API keys (more secure)
4. Compare dumps across releases to track performance
5. Create custom YARA rules for your threat landscape
6. Monitor during load tests to catch issues early
7. Use Claude for complex reasoning, Gemini for speed

**Performance:**
- Analysis typically takes 30-60 seconds
- Large dumps (>1GB) may take 2-3 minutes
- Use `--export-json` for faster processing (no formatting)
- Run locally with Ollama for unlimited analysis

**Security:**
- PII redaction happens before AI processing
- Dumps are never uploaded (only analysis text)
- API keys are never logged
- Backups are encrypted

---

## Getting Help

```bash
# General help
memguard --help

# Command-specific help
memguard analyze --help
memguard fix --help
memguard monitor --help

# Version info
memguard --version
```

## Additional Documentation

- [Workflows Guide](docs/WORKFLOWS.md) - Common usage scenarios
- [YARA Rules](docs/YARA-RULES.md) - Custom rule creation
- [Plugin Development](examples/SampleDetectorPlugin/README.md) - Create custom detectors
- [API Reference](docs/API.md) - Programmatic usage

---

**For more information visit: https://github.com/FahadAkash/MemGuard**
