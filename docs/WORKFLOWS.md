# Common MemGuard Workflows

This document provides step-by-step workflows for common MemGuard use cases with the new advanced features.

## Workflow 1: Security Incident Investigation

**Scenario**: Your application crashed in production and you suspect a security issue.

### Steps:

```bash
# 1. Collect memory dump (if not already collected)
dotnet-dump collect -p <process-id> -o incident.dmp

# 2. Run comprehensive security analysis
memguard analyze incident.dmp \
  --provider claude \
  --api-key YOUR_CLAUDE_KEY \
  --export-json \
  --output incident-report.json

# 3. Review security findings
# - Check for IOC indicators
# - Look for exploit techniques (ROP chains, shellcode)
# - Review YARA rule matches

# 4. If threats detected, escalate to security team
# Export detailed technical report
memguard analyze incident.dmp \
  --provider gemini \
  --api-key YOUR_KEY \
  --export-pdf \
  --output security-report.pdf
```

**Expected Output**:
- IOC threat score
- Exploit heuristics findings
- YARA rule matches
- Executive summary for stakeholders

---

## Workflow 2: Memory Leak Detection & Fix

**Scenario**: Application memory usage keeps growing.

### Steps:

```bash
# 1. Take baseline dump
dotnet-dump collect -p <pid> -o baseline.dmp

# 2. Wait for memory to grow (run load tests, etc.)
# ...

# 3. Take second dump
dotnet-dump collect -p <pid> -o after-load.dmp

# 4. Compare dumps to find regression
memguard compare baseline.dmp after-load.dmp --output comparison.md

# 5. Analyze the second dump for leaks
memguard analyze after-load.dmp \
  --provider gemini \
  --api-key YOUR_KEY

# 6. Auto-fix detected issues
memguard fix after-load.dmp \
  --project ./MyApp \
  --provider gemini \
  --api-key YOUR_KEY

# 7. Review proposed fixes (dry-run first)
memguard fix after-load.dmp \
  --project ./MyApp \
  --dry-run \
  --provider gemini \
  --api-key YOUR_KEY
```

**Expected Findings**:
- Heap fragmentation levels
- Top memory consumers
- Suggested code fixes
- Object retention graphs

---

## Workflow 3: Compliance Audit Preparation

**Scenario**: Preparing for SOC 2 or HIPAA compliance audit.

### Steps:

```bash
# 1. Analyze production dumps with compliance mode
memguard analyze production.dmp \
  --provider claude \
  --api-key YOUR_KEY \
  --compliance soc2 \
  --redact-pii \
  --export-pdf \
  --output soc2-compliance-report.pdf

# 2. Generate audit trail
# MemGuard automatically creates chain-of-custody logs
# Check: ~/.memguard/audit-trail/

# 3. Review PII/PHI redaction
# Verify sensitive data was properly redacted

# 4. Generate executive summary for auditors
# The PDF includes:
# - Business impact assessment
# - Security posture
# - Compliance checklist
# - Audit metadata
```

**Deliverables**:
- Compliance-ready PDF report
- Audit trail logs
- PII/PHI redaction confirmation
- Executive-level summary

---

## Workflow 4: Deadlock Troubleshooting

**Scenario**: Application hangs intermittently.

### Steps:

```bash
# 1. Capture dump when  app is hung
dotnet-dump collect -p <pid> -o deadlock.dmp

# 2. Analyze with enhanced visualization
memguard analyze deadlock.dmp \
  --provider gemini \
  --api-key YOUR_KEY \
  --visualize

# 3. Review deadlock cycle visualization
# Look for:
# - ASCII art deadlock diagram
# - Thread interaction table
# - Lock acquisition order

# 4. Generate fix recommendations
memguard fix deadlock.dmp \
  --project ./MyApp \
  --provider claude \
  --api-key YOUR_KEY
```

**Visual Output**:
```
┌────────────────┐
│ Thread 1234    │
└────────────────┘
       │
       │ holds lock on
       │ MyService.Lock
       ↓
  (waits for)

┌────────────────┐
│ Thread 5678    │
└────────────────┘
       │
       │ holds lock on
       │ Database.Lock
       ↓
  (waits for Thread 1234)
       ↑
       └─────── [CYCLE]
```

---

## Workflow 5: Custom Malware Detection

**Scenario**: Need to detect custom malware patterns specific to your environment.

### Steps:

```bash
# 1. Create custom YARA rules
# Edit: ~/.memguard/yara-rules/custom/my_threats.yar

# 2. Add your patterns
cat > ~/.memguard/yara-rules/custom/my_threats.yar << 'EOF'
rule CustomThreat
{
    meta:
        description = "Our custom threat pattern"
        severity = "critical"
    
    strings:
        $pattern1 = "suspicious_api_call"
        $pattern2 = "unusual_registry_key"
    
    condition:
        all of them
}
EOF

# 3. Run analysis with custom rules
memguard analyze suspect.dmp \
  --provider gemini \
  --api-key YOUR_KEY

# 4. Check YARA matches in output
# Look for "CustomThreat" in results

# 5. Export findings for security team
memguard analyze suspect.dmp \
  --provider gemini \
  --api-key YOUR_KEY \
  --export-json \
  --output threat-report.json
```

---

## Workflow 6: Plugin Development & Testing

**Scenario**: Need custom detection logic for your specific use case.

### Steps:

```bash
# 1. Create plugin project
cd examples/SampleDetectorPlugin
dotnet build -c Release

# 2. Deploy plugin
copy bin\Release\net8.0\SampleDetectorPlugin.dll %USERPROFILE%\.memguard\plugins\

# 3. Test plugin
memguard analyze test.dmp \
  --provider gemini \
  --api-key YOUR_KEY

# 4. Verify plugin loaded
# Look for: "Loaded detector plugin: ExcessiveStringDetector v1.0.0"

# 5. Review plugin findings in output

# 6. Iterate on plugin logic
# Edit ExcessiveStringDetector.cs
# Rebuild and replace DLL
# Test again
```

---

## Workflow 7: CI/CD Integration

**Scenario**: Automated memory analysis in your build pipeline.

### Steps:

```bash
# 1. Add to your CI/CD script (e.g., Azure DevOps, GitHub Actions)

# Run tests and generate dumps
dotnet test --collect:"XPlat Code Coverage" --logger:"dump;DumpType=Full"

# Analyze any crash dumps
for dump in **/*.dmp; do
  memguard analyze $dump \
    --provider gemini \
    --api-key $MEMGUARD_API_KEY \
    --export-json \
    --output "${dump%.dmp}-analysis.json"
done

# 2. Check for critical issues
# Parse JSON output
# Fail build if critical security issues found

# 3. Archive reports as build artifacts
# Upload analysis.json files to artifact storage
```

**GitHub Actions Example**:
```yaml
- name: Analyze Memory Dumps
  run: |
    dotnet tool install -g memguard
    memguard analyze crash.dmp \
      --provider gemini \
      --api-key ${{ secrets.GEMINI_KEY }} \
      --export-json
```

---

## Workflow 8: Performance Regression Testing

**Scenario**: Track memory performance across releases.

### Steps:

```bash
# 1. Baseline (v1.0)
dotnet-dump collect -p <pid-v1> -o v1.0-baseline.dmp

# 2. After changes (v1.1)
dotnet-dump collect -p <pid-v1.1> -o v1.1-current.dmp

# 3. Compare versions
memguard compare v1.0-baseline.dmp v1.1-current.dmp \
  --format markdown \
  --output regression-report.md

# 4. Review metrics
# - Memory growth/reduction
# - Fragmentation delta
# - Object count changes

# 5. Fail release if regression > threshold
# Parse comparison output
# Check memory growth percentage
# Gate deployment based on results
```

---

## Workflow 9: Live Process Monitoring

**Scenario**: Monitor memory in real-time during load tests.

### Steps:

```bash
# 1. Start monitoring before load test
memguard monitor \
  --process MyApp \
  --interval 5 \
  --alert-threshold 500 \
  --output load-test-metrics.json &

# 2. Run load tests
# ...

# 3. Monitor real-time alerts
# MemGuard will  alert when memory exceeds 500MB

# 4. Stop monitoring (Ctrl+C after tests)

# 5. Analyze captured metrics
cat load-test-metrics.json | jq '.samples[] | select(.workingSetMB > 400)'

# 6. If issues found, capture dump
dotnet-dump collect -p <pid> -o high-memory.dmp

# 7. Analyze the dump
memguard analyze high-memory.dmp \
  --provider gemini \
  --api-key YOUR_KEY
```

---

## Workflow 10: Batch Cloud Analysis

**Scenario**: Analyze dumps from cloud storage (AWS S3).

### Steps:

```bash
# 1. Upload dumps to S3
aws s3 cp crash-dumps/ s3://my-dumps-bucket/crashes/ --recursive

# 2. Run batch analysis
memguard batch-analyze \
  --source s3://my-dumps-bucket/crashes/ \
  --output-bucket s3://my-dumps-bucket/results/ \
  --provider gemini \
  --api-key YOUR_KEY \
  --concurrency 4

# 3. Download results
aws s3 sync s3://my-dumps-bucket/results/ ./analysis-results/

# 4. Generate aggregate report
# Combine individual JSON reports
# Create summary dashboard
```

---

## Quick Reference Commands

```bash
# Basic analysis
memguard analyze dump.dmp --provider gemini --api-key KEY

# With visualization
memguard analyze dump.dmp --provider gemini --api-key KEY --visualize

# Export formats
memguard analyze dump.dmp --provider gemini --api-key KEY --export-json
memguard analyze dump.dmp --provider gemini --api-key KEY --export-pdf

# Auto-fix
memguard fix dump.dmp --project ./MyApp --provider gemini --api-key KEY

# Compare dumps
memguard compare before.dmp after.dmp --output comparison.md

# Live monitoring
memguard monitor --process MyApp --interval 5 --alert-threshold 500

# Compliance mode
memguard analyze dump.dmp --compliance hipaa --redact-pii --provider gemini --api-key KEY

# Agent mode
memguard agent --project ./MyApp --provider gemini --api-key KEY
```

## Tips

1. **Use Claude for complex analysis**: Claude excels at reasoning about code issues
2. **Use Gemini for speed**: Gemini is faster for quick checks
3. **Always redact PII in production dumps** before sharing with external teams
4. **Set up YARA rules** for your specific threat landscape
5. **Create custom plugins** for organization-specific detection logic
6. **Integrate with CI/CD** to catch issues before production
7. **Monitor regularly** during load tests to catch memory issues early
8. **Compare dumps** across releases to track performance trends
