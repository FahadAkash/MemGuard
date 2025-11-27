# MemGuard Installation Guide

## 🚀 **Quick Install (Global Tool)**

### **Option 1: Install from Local Build**

```bash
# 1. Clone and build
git clone https://github.com/FahadAkash/MemGuard.git
cd MemGuard

# 2. Pack as global tool
dotnet pack src/MemGuard.Cli/MemGuard.Cli.csproj -c Release

# 3. Install globally
dotnet tool install --global --add-source src/MemGuard.Cli/nupkg MemGuard

# 4. Use from anywhere!
memguard --help
```

### **Option 2: Install from NuGet (Future)**

```bash
# Once published to NuGet.org
dotnet tool install --global MemGuard

# Use immediately
memguard --help
```

---

## ✅ **Verify Installation**

```bash
# Check if installed
dotnet tool list --global

# Should show:
# Package Id      Version      Commands
# MemGuard        1.0.0        memguard

# Test it
memguard --help
```

---

## 🎯 **Usage After Installation**

### **From Any Directory:**

```bash
# Analyze dumps
memguard analyze F:\dumps\crash.dmp --provider gemini --api-key YOUR_KEY

# Start agent
memguard agent --project . --provider claude --api-key YOUR_KEY

# Monitor process
memguard monitor --process MyApp --alert-threshold 500

# Compare dumps
memguard compare before.dmp after.dmp

# List backups
memguard restore --list

# All commands work from anywhere!
```

---

## 🔄 **Update MemGuard**

```bash
# Uninstall old version
dotnet tool uninstall --global MemGuard

# Install new version
dotnet tool install --global --add-source src/MemGuard.Cli/nupkg MemGuard
```

---

## 🗑️ **Uninstall**

```bash
dotnet tool uninstall --global MemGuard
```

---

## 🛠️ **For Developers**

### **Build and Test Locally:**

```bash
# Build
dotnet build src/MemGuard.Cli/MemGuard.Cli.csproj

# Run without installing
dotnet run --project src/MemGuard.Cli/MemGuard.Cli.csproj -- analyze crash.dmp

# Pack
dotnet pack src/MemGuard.Cli/MemGuard.Cli.csproj -c Release

# Install from local pack
dotnet tool install --global --add-source src/MemGuard.Cli/nupkg MemGuard --version 1.0.0
```

---

## 📦 **Publish to NuGet (Maintainers)**

```bash
# 1. Build release
dotnet pack src/MemGuard.Cli/MemGuard.Cli.csproj -c Release

# 2. Get API key from nuget.org

# 3. Push to NuGet
dotnet nuget push src/MemGuard.Cli/nupkg/MemGuard.1.0.0.nupkg --api-key YOUR_NUGET_KEY --source https://api.nuget.org/v3/index.json

# 4. Users can now install:
dotnet tool install --global MemGuard
```

---

## 🌍 **Environment Variables**

Set once, use everywhere:

```bash
# Windows (PowerShell)
$env:MEMGUARD_GEMINI_KEY = "your_key"
$env:MEMGUARD_CLAUDE_KEY = "your_key"
$env:MEMGUARD_GROK_KEY = "your_key"
$env:MEMGUARD_DEEPSEEK_KEY = "your_key"

# Or add to system environment variables permanently
# Then just use:
memguard analyze crash.dmp --provider gemini
# No --api-key needed!
```

```bash
# Linux/Mac
export MEMGUARD_GEMINI_KEY="your_key"
export MEMGUARD_CLAUDE_KEY="your_key"

# Add to ~/.bashrc or ~/.zshrc for persistence
```

---

## 💡 **Tips**

### **1. Create Aliases:**

```bash
# PowerShell
Set-Alias mg memguard

# Now use:
mg analyze crash.dmp
```

### **2. Add to PATH (already done by dotnet tool):**

The `dotnet tool install --global` command automatically adds MemGuard to your PATH, so `memguard` works from any directory.

### **3. Tab Completion:**

```bash
# Type and press Tab
memguard ana<Tab>  # Completes to 'analyze'
memguard --<Tab>   # Shows all options
```

---

## 🔧 **Troubleshooting**

### **Issue: "memguard not found"**

```bash
# Check if installed
dotnet tool list --global

# If not listed, install:
dotnet tool install --global --add-source src/MemGuard.Cli/nupkg MemGuard

# If still not found, check PATH
echo $env:PATH  # Windows
echo $PATH      # Linux/Mac

# Should include: C:\Users\YourName\.dotnet\tools
```

### **Issue: "Version conflict"**

```bash
# Uninstall first
dotnet tool uninstall --global MemGuard

# Then reinstall
dotnet tool install --global --add-source src/MemGuard.Cli/nupkg MemGuard
```

### **Issue: "Access denied"**

```bash
# Run as administrator (Windows)
# Or use sudo (Linux/Mac)
sudo dotnet tool install --global MemGuard
```

---

## 📊 **What Gets Installed**

```
C:\Users\YourName\.dotnet\tools\
├── memguard.exe              # Main executable
├── MemGuard.Cli.dll          # CLI assembly
├── MemGuard.Core.dll         # Core library
├── MemGuard.AI.dll           # AI providers
├── MemGuard.Infrastructure.dll
├── MemGuard.Reporters.dll
└── [dependencies...]         # All NuGet packages
```

**Total size:** ~15-20 MB

---

## ✅ **Complete Installation Example**

```bash
# 1. Clone repository
git clone https://github.com/FahadAkash/MemGuard.git
cd MemGuard

# 2. Build and pack
dotnet pack src/MemGuard.Cli/MemGuard.Cli.csproj -c Release

# 3. Install globally
dotnet tool install --global --add-source src/MemGuard.Cli/nupkg MemGuard

# 4. Verify
memguard --help

# 5. Set API keys (optional)
$env:MEMGUARD_GEMINI_KEY = "AIzaSyBwO0jyYU9LBexFLO2W6yU6JLovY8HgqXo"

# 6. Use from anywhere!
cd C:\MyProjects\WebApp
memguard analyze crash.dmp --provider gemini

cd D:\OtherProject
memguard agent --project . --provider gemini

# Works everywhere! 🎉
```

---

## 🎓 **For CI/CD**

```yaml
# GitHub Actions example
- name: Install MemGuard
  run: dotnet tool install --global MemGuard

- name: Analyze dump
  run: memguard analyze crash.dmp --provider gemini --api-key ${{ secrets.GEMINI_KEY }} --export-json

- name: Upload results
  uses: actions/upload-artifact@v2
  with:
    name: analysis-results
    path: analysis.json
```

---

## 🌟 **Benefits of Global Installation**

✅ **One command:** `memguard` works everywhere
✅ **No path issues:** Automatically in PATH
✅ **Easy updates:** `dotnet tool update --global MemGuard`
✅ **Clean uninstall:** `dotnet tool uninstall --global MemGuard`
✅ **Version management:** Multiple versions supported
✅ **Cross-platform:** Works on Windows, Linux, Mac

---

## 📝 **Summary**

```bash
# Install once
dotnet tool install --global --add-source src/MemGuard.Cli/nupkg MemGuard

# Use everywhere
memguard analyze crash.dmp
memguard agent --project .
memguard monitor --process MyApp
memguard restore --list

# Update anytime
dotnet tool update --global MemGuard

# Uninstall if needed
dotnet tool uninstall --global MemGuard
```

**That's it! MemGuard is now a global command!** 🚀
