# MemGuard Fix Report
**Date:** 11/28/2025 10:45:57 AM
**Dump File:** crash.dmp
**Project:** f:\gihtub\MemGuard\examples\LeakyApp
**AI Provider:** Gemini
**Mode:** Applied
**Duration:** 4.4s

## Diagnostics Found

### Heap (Warning)
Heap fragmentation: 0.82%
- **Fragmentation:** 0.82%
- **Total Size:** 353,238,848 bytes

### Deadlock (Critical)
Deadlock detected between threads 1, 2, 4, 5
- **Threads Involved:** 1, 2, 4, 5
- **Locks:**
  - Thread 1 (Locks: 4294967295) - Unknown -> Unknown -> ReadFile
  - Thread 2 (Locks: 4294967295) - Unknown
  - Thread 4 (Locks: 4294967295) - SleepInternal -> Sleep -> <Main>b__1_0
  - Thread 5 (Locks: 4294967295) - Unknown

## Fixes Applied

| File | Lines Added | Lines Removed | Lines Modified | Status |
|------|-------------|----------------|----------------|--------|
| Program.cs | 1 | 3 | 0 | Applied |

**Total Changes:** +1 -3 ~0

## Code Changes

### Program.cs

```diff
━━━ Program.cs ━━━
@@ Lines 4-11 @@
   4   
   5   namespace LeakyApp
   6   {
   7 -     dwfw
   8 -     static List<string> _leak = new List<string>();
   9       class Program
  10       {
  11           static List<string> _leak = new List<string>();

@@ Lines 27-33 @@
  27                       Thread.Sleep(100);
  28                   }
  29               });
  30 +             t.IsBackground = true; //Mark the thread as background
  31               t.Start();
  32   
  33               Console.ReadLine();

@@ Lines 34-37 @@
  34           }
  35       }
  36   }
  37 - 


```

## Backup Information

**Backup ID:** `20251128_104557`

To restore the backup:
```bash
memguard restore --backup-id 20251128_104557
```

## Summary

- **Files Modified:** 1
- **Success:** ✓
- **Backup Created:** ✓

