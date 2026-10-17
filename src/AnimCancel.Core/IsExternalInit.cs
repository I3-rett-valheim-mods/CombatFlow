// Shim so C# 9+ records / init accessors compile on netstandard2.0.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
