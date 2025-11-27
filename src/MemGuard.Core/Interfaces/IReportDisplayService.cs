using MemGuard.Core;

namespace MemGuard.Core.Interfaces;

public interface IReportDisplayService
{
    void DisplayHeader(string title);
    void DisplayInfo(string message);
    void DisplaySuccess(string message);
    void DisplayError(string message, Exception? ex = null);
    void DisplayReport(LeakReport report);
    void DisplayObjectInfo(ObjectInfo objInfo);
    void DisplayRetentionPath(RetentionPath path);
    string FormatBytes(ulong bytes);
}