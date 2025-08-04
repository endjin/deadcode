using Spectre.Console.Testing;

namespace DeadCode.Tests.CLI.TestHelpers;

public static class CommandTestHelpers
{
    /// <summary>
    /// Helper for creating test environments for CLI commands
    /// Note: CommandContext is sealed and has internal constructors,
    /// so we can't mock or create it directly for unit tests.
    /// </summary>
    public static TestConsole CreateTestConsole()
    {
        return new TestConsole();
    }
}