using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;   

namespace MemGuard.Core.Interfaces;

public interface IMemleaksDetector
{
    void Track(string key , object instance);
    /// <summary>
    /// Forces GC and Checks if tracked objects are still alive 
    /// Returns a report of "alive " objects.
    /// </summary>
    /// <returns></returns> 
    IReadOnlyList<string> DetectMemleaks();
    void UnTrack(string key);
    void Reset();
}

