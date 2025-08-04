using System.Runtime.InteropServices;

namespace DeadCode.Tests.TestHelpers;

internal static class NativeMethods
{
    [DllImport("kernel32.dll")]
    internal static extern bool DllImportMethod();
}