using System;
using System.Collections.Generic;
using System.Threading;

namespace LeakyApp
{
    class Program
    {
        static List<string> _leak = new List<string>();
        private const int MaxLeakSize = 1000; // Limit to prevent unbounded growth

        static void Main(string[] args)
        {
            Console.WriteLine("LeakyApp started. PID: " + System.Diagnostics.Process.GetCurrentProcess().Id);
            Console.WriteLine("Press Enter to stop...");
            Console.WriteLine($"Memory leak limited to {MaxLeakSize} items");

            // Leak memory (but with a bound)
            var t = new Thread(() =>
            {
                while (true)
                {
                    _leak.Add(DateTime.Now.ToString());

                    // Keep the list bounded to prevent excessive memory consumption
                    if (_leak.Count > MaxLeakSize)
                    {
                        _leak.RemoveAt(0); // Remove oldest item
                    }

                    Thread.Sleep(100);
                }
            });
            t.Start();

            Console.ReadLine();
            Console.WriteLine("Stopping...");
        }
    }
}
