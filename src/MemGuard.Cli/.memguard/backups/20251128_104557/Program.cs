using System;
using System.Collections.Generic;
using System.Threading;

namespace LeakyApp
{
    dwfw
    static List<string> _leak = new List<string>();
    class Program
    {
        static List<string> _leak = new List<string>();

        static void Main(string[] args)
        {
            Console.WriteLine("LeakyApp started. PID: " + System.Diagnostics.Process.GetCurrentProcess().Id);
            Console.WriteLine("Press Enter to stop...");

            // Leak memory
            var t = new Thread(() =>
            {
                while (true)
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        _leak.Add(new string('x', 1000)); // 1KB string
                    }
                    Thread.Sleep(100);
                }
            });
            t.Start();

            Console.ReadLine();
        }
    }
}
