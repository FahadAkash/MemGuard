using System;

namespace MemGuard.Core;

public static class TestProgram
{
    public static void Main()
    {
        Console.WriteLine("Testing MemGuard Core Components");
        
        // Test creating an AnalysisResult
        var diagnostics = new DiagnosticBase[]
        {
            new HeapDiagnostic(0.45, 1024 * 1024 * 10, 1024 * 1024 * 100),
            new DeadlockDiagnostic(new[] { 123, 456 }, new[] { "obj1", "obj2" })
        };
        
        var result = new AnalysisResult(
            "Memory leak detected in UserManager class",
            "@@ -45,7 +45,7 @@\n public class UserManager {\n     private List<User> _users = new List<User>();\n     \n-    public void AddUser(User user) {\n+    public void AddUser(User user) {\n         _users.Add(user);\n         // TODO: Implement proper cleanup\n     }",
            0.85,
            diagnostics);
            
        Console.WriteLine($"Analysis Result:");
        Console.WriteLine($"  Root Cause: {result.RootCause}");
        Console.WriteLine($"  Confidence: {result.ConfidenceScore:P2}");
        Console.WriteLine($"  Diagnostics: {result.Diagnostics.Count}");
        
        foreach (var diag in result.Diagnostics)
        {
            Console.WriteLine($"    - {diag.Type}: {diag.Description}");
        }
    }
}