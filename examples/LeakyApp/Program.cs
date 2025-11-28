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

            // for (int i = 0; i < LeakIterations; i++)
            // {
            //     LeakMemory();
            //     Thread.Sleep(10); // Simulate some work
            // }

            Console.WriteLine("LeakyApp finished.");
            // Console.ReadKey(); // Keep the app running so we can observe memory usage
        }

        static void LeakMemory()
        {
            if (_leak.Count < MaxLeakSize)
            {
                _leak.Add(new string('A', 1024)); // Allocate 1KB string
            }
        }
    }
}