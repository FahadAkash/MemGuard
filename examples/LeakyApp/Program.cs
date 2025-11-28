// Enhanced by MemGuard AI
using System;
using System.Collections.Generic;
using System.Threading;

namespace LeakyApp
{
    class Program
    {
        static List<string> _leak = new List<string>();
        private const int MaxLeakSize = 1000; // Limit to prevent unbounded growth
        private const int LeakIterations = 500; // Number of iterations to leak

        static void Main(string[] args)
        {
            Console.WriteLine("LeakyApp started. PID: " + System.Diagnostics.Process.GetCurrentProcess().Id);

            for (int i = 0; i < LeakIterations; i++)
            {
                // Simulate a memory leak by adding strings to a list.
                _leak.Add(new string('x', 1024)); // 1KB string

                // Optionally limit the leak size to prevent excessive memory usage.
                if (_leak.Count > MaxLeakSize)
                {
                    _leak.RemoveAt(0); // Remove the oldest entry
                }

                Thread.Sleep(10); // Simulate some work
            }

            Console.WriteLine("Leaking complete. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
