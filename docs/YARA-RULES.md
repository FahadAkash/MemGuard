# YARA Rules Documentation for MemGuard

## Overview

MemGuard integrates YARA rule-based pattern matching to detect known malicious patterns, malware signatures, and suspicious code in .NET memory dumps.

## Rule Location

YARA rules are stored in:
- **Windows**: `%USERPROFILE%\.memguard\yara-rules\`
- **Linux/Mac**: `~/.memguard/yara-rules/`

MemGuard automatically creates this directory and includes default rules on first run.

## Default Rules

MemGuard includes two default rule sets:

### 1. Suspicious Strings Rule
Detects common attack-related strings:

```yara
rule SuspiciousStrings
{
    meta:
        description = "Detects suspicious strings in memory"
        severity = "medium"
    
    strings:
        $s1 = "cmd.exe" nocase
        $s2 = "powershell" nocase
        $s3 = "mimikatz" nocase
        $s4 = "password" nocase

    condition:
        any of them
}
```

### 2. Potential Ransomware Rule
Detects ransomware-related patterns:

```yara
rule PotentialRansomware
{
    meta:
        description = "Detects potential ransomware patterns"
        severity = "critical"
    
    strings:
        $enc1 = "encrypt" nocase
        $enc2 = "decrypt" nocase
        $ext1 = ".locked"
        $ext2 = ".encrypted"
        $btc = "bitcoin" nocase

    condition:
        2 of ($enc*) or any of ($ext*) or $btc
}
```

## Creating Custom Rules

### Rule Structure

```yara
rule RuleName
{
    meta:
        description = "What this rule detects"
        severity = "critical|high|medium|low"
        author = "Your Name"
        date = "2025-01-01"
    
    strings:
        $string1 = "exact match"
        $string2 = "case insensitive" nocase
        $string3 = /regex pattern/
        $hex_pattern = { 4D 5A 90 00 }  // PE header
    
    condition:
        all of them or
        $string1 and $string2
}
```

### Example: Detecting Memory Injection

```yara
rule MemoryInjection
{
    meta:
        description = "Detects potential memory injection techniques"
        severity = "critical"
    
    strings:
        $api1 = "VirtualAllocEx" nocase
        $api2 = "WriteProcessMemory" nocase
        $api3 = "CreateRemoteThread" nocase
        $api4 = "NtCreateThreadEx" nocase
    
    condition:
        2 of them
}
```

### Example: Detecting Credential Dumping

```yara
rule CredentialDumping
{
    meta:
        description = "Detects LSASS credential dumping"
        severity = "critical"
    
    strings:
        $lsass1 = "lsass.exe" nocase
        $lsass2 = "lsass.dmp" nocase
        $tool1 = "mimikatz" nocase
        $tool2 = "procdump" nocase
        $cred = "password" nocase
    
    condition:
        ($lsass1 or $lsass2) and ($ tool1 or $tool2 or $cred)
}
```

## Rule Best Practices

### 1. Use Meaningful Names
```yara
rule SQL_Injection_Attempt  // Good
rule Rule1                   // Bad
```

### 2. Add Metadata
Always include `description` and `severity`:
```yara
meta:
    description = "Clear explanation of what this detects"
    severity = "critical"
    reference = "https://attack.mitre.org/techniques/T1234"
```

### 3. Optimize Conditions
```yara
// Efficient
condition:
    any of ($api*) and $suspicious_call

// Less efficient (matches everything)
condition:
    any of them
```

### 4. Test Rules
Before deploying, test your rules:
```bash
# Test on a known-good dump
memguard analyze good.dmp --provider gemini --api-key KEY

# Test on a known-bad dump
memguard analyze malicious.dmp --provider gemini --api-key KEY
```

## String Modifiers

| Modifier | Description | Example |
|----------|-------------|---------|
| `nocase` | Case-insensitive | `$s = "password" nocase` |
| `wide` | UTF-16 strings | `$s = "unicode" wide` |
| `ascii` | ASCII only | `$s = "text" ascii` |
| `fullword` | Whole words only | `$s = "admin" fullword` |

## Hex Patterns

Detect binary patterns:

```yara
strings:
    // MZ header (PE file)
    $mz = { 4D 5A }
    
    // NOP sled
    $nop_sled = { 90 90 90 90 90 }
    
    // Wildcards
    $pattern = { 4D 5A ?? ?? 00 00 }
    
    // Jumps (0-3 bytes)
    $jump = { E9 [0-3] }
```

## Advanced Conditions

### Counting Matches
```yara
condition:
    #suspicious_string > 5  // Must appear more than 5 times
```

### File Size
```yara
condition:
    filesize < 1000KB and $pattern
```

### Offset Requirements
```yara
condition:
    $mz at 0  // MZ header at file start
```

## Organizing Rules

### Directory Structure
```
~/.memguard/yara-rules/
├── malware/
│   ├── ransomware.yar
│   ├── trojans.yar
│   └── rootkits.yar
├── exploits/
│   ├── code_injection.yar
│   └── privilege_escalation.yar
├── suspicious/
│   ├── obfuscation.yar
│   └── anti_debug.yar
└── custom/
    └── my_rules.yar
```

All `.yar` files in subdirectories are automatically loaded.

## Rule Performance

### Optimize for Speed

1. **Limit String Count**: Keep under 20 strings per rule
2. **Use Specific Patterns**: Avoid overly broad matches
3. **Early Exit Conditions**: Put quick checks first
4. **Avoid Regex**: Use hex patterns when possible

### Example Optimization

```yara
// Slow
condition:
    /pattern.*match/ and other_checks

// Fast
condition:
    $quick_check and /pattern.*match/
```

## Testing Your Rules

### 1. Validate Syntax
```bash
# If you have YARA installed locally
yara rule_file.yar test_file
```

### 2. Test with MemGuard
```bash
# Run analysis with your custom rules
memguard analyze dump.dmp --provider gemini --api-key KEY
```

### 3. Check Rule Loading
MemGuard logs loaded rules at startup:
```
[YARA] Loaded 25 rules from ~/.memguard/yara-rules/
[YARA] Loaded: SuspiciousStrings, PotentialRansomware, MemoryInjection...
```

## Common Use Cases

### Detecting Malware Families

```yara
rule Emotet_Patterns
{
    meta:
        description = "Emotet malware patterns"
        severity = "critical"
    
    strings:
        $api1 = "URLDownloadToFileA"
        $api2 = "WinExec"
        $mutex = "Global\\M01C2Z6"
    
    condition:
        all of them
}
```

### Detecting Data Exfiltration

```yara
rule DataExfiltration
{
    strings:
        $upload = "POST" nocase
        $network = "System.Net.Http" nocase
        $file = "System.IO.File" nocase
    
    condition:
        all of them
}
```

## Troubleshooting

### Rules Not Loading

1. Check file extension is `.yar` or `.yara`
2. Verify file has no syntax errors
3. Check file permissions (must be readable)
4. Look for error messages in MemGuard output

### False Positives

Refine your conditions:
```yara
// Instead of:
condition: any of them

// Try:
condition: 3 of them  // Require multiple matches
```

### Rule Not Matching

1. Verify strings exist in dump with hex viewer
2. Check string modifiers (nocase, wide, etc.)
3. Test with simplified condition
4. Add logging to debug matches

## Additional Resources

- [Official YARA Documentation](https://yara.readthedocs.io/)
- [YARA Rules Repository](https://github.com/Yara-Rules/rules)
- [Writing YARA Rules](https://yara.readthedocs.io/en/stable/writingrules.html)
- [MemGuard YARA Integration](../README.md#yara-detection)

## Example: Complete Malware Detection Rule

```yara
import "pe"
import "math"

rule Advanced_Malware_Detection
{
    meta:
        description = "Comprehensive malware detection"
        author = "MemGuard Team"
        date = "2025-01-01"
        severity = "critical"
        reference = "https://attack.mitre.org/techniques/T1055"
    
    strings:
        // Process injection APIs
        $api1 = "VirtualAllocEx" ascii wide nocase
        $api2 = "WriteProcessMemory" ascii wide nocase
        $api3 = "CreateRemoteThread" ascii wide nocase
        
        // Suspicious strings
        $sus1 = "mimikatz" ascii wide nocase
        $sus2 = "admin" ascii wide nocase fullword
        
        // Hex patterns
        $shellcode = { 31 C0 50 68 }
        $pe_header = { 4D 5A 90 00 }
    
    condition:
        // Must have at least 2 API calls
        2 of ($api*) and
        
        // And one suspicious string or shellcode
        (any of ($sus*) or $shellcode) and
        
        // Optional PE header check
        ($pe_header at 0 or not $pe_header)
}
```

This rule combines multiple detection techniques for comprehensive coverage.
