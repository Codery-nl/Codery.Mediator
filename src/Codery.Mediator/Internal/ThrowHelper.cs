using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Codery.Mediator.Internal;

/// <summary>
/// Internal helper for null argument validation across target frameworks.
/// </summary>
[DebuggerStepThrough]
#if NET6_0_OR_GREATER
[StackTraceHidden]
#endif
internal static class ThrowHelper
{
    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.
    /// </summary>
    public static void ThrowIfNull(
        [NotNull] object? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(argument, paramName);
#else
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
#endif
    }
}
