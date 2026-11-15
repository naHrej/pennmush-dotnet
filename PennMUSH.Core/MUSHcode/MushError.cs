namespace PennMUSH.Core.MUSHcode;

using System;
using System.Collections.Generic;

public enum MushError
{
    NoMatch,
    TooFewArguments,
    Nothing,
    RegexTimeout,
    InvalidPlayer,
    DivisionByZero,
    FunctionNotFound,
    LogOfNonpositive,
    OutOfRange,
    RunawayLoop,
    ExecutionHalted,
    FunctionInvocationLimitExceeded,
    FunctionRecursionLimitExceeded,
    InvalidTarget,
    NoCode,
    NoCommand,
    PermissionDenied,
    QueueFull,
    RecursionCycleDetected,
    SquareRootOfNegative,
    FunctionDisabled,
    NoSuchAttribute,
    InvalidSecondArgument,
    FunctionExpects,
}

public static class MushErrorText
{
    private static readonly Dictionary<MushError, string> Templates = new()
    {
        [MushError.NoMatch] = "#-1 NO MATCH",
        [MushError.TooFewArguments] = "#-1 TOO FEW ARGUMENTS",
        [MushError.Nothing] = "#-1",
        [MushError.RegexTimeout] = "#-1 REGEX TIMEOUT",
        [MushError.InvalidPlayer] = "#-1 INVALID PLAYER",
        [MushError.DivisionByZero] = "#-1 DIVISION BY ZERO",
        [MushError.FunctionNotFound] = "#-1 FUNCTION ({{function}}) NOT FOUND",
        [MushError.LogOfNonpositive] = "#-1 LOG OF NONPOSITIVE",
        [MushError.OutOfRange] = "#-1 OUT OF RANGE",
        [MushError.RunawayLoop] = "#-1 TIMEOUT (RUNAWAY LOOP DETECTED)",
        [MushError.ExecutionHalted] = "#-1 EXECUTION HALTED",
        [MushError.FunctionInvocationLimitExceeded] = "#-1 FUNCTION INVOCATION LIMIT EXCEEDED",
        [MushError.FunctionRecursionLimitExceeded] = "#-1 FUNCTION RECURSION LIMIT EXCEEDED",
        [MushError.InvalidTarget] = "#-1 INVALID TARGET",
        [MushError.NoCode] = "#-1 NO CODE",
        [MushError.NoCommand] = "#-1 NO COMMAND",
        [MushError.PermissionDenied] = "#-1 PERMISSION DENIED",
        [MushError.QueueFull] = "#-1 QUEUE FULL",
        [MushError.RecursionCycleDetected] = "#-1 RECURSION CYCLE DETECTED",
        [MushError.SquareRootOfNegative] = "#-1 SQUARE ROOT OF NEGATIVE",
        [MushError.FunctionDisabled] = "#-1 FUNCTION DISABLED",
        [MushError.NoSuchAttribute] = "#-1 NO SUCH ATTRIBUTE",
        [MushError.InvalidSecondArgument] = "#-1 INVALID SECOND ARGUMENT",
        [MushError.FunctionExpects] = "#-1 FUNCTION ({{function}}) EXPECTS {{range}} ARGUMENTS BUT GOT {{got}}",
    };

    /// <summary>
    /// Player-visible text for this error. Each {{token}} needs a name and value.
    /// </summary>
    public static string Text(this MushError error, string name, string value)
        => error.Text((name, value));

    public static string Text(this MushError error, params (string Name, string Value)[] tokens)
    {
        if (!Templates.TryGetValue(error, out var text))
            throw new ArgumentOutOfRangeException(nameof(error), error, "No template for this error.");

        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (name, value) in tokens)
        {
            if (string.IsNullOrEmpty(name) || !used.Add(name))
                throw new ArgumentException($"Token '{name}' is missing or repeated.", nameof(tokens));

            var marker = "{{" + name + "}}";
            if (!text.Contains(marker, StringComparison.Ordinal))
                throw new ArgumentException($"MushError.{error} has no {marker} token.", nameof(tokens));

            text = text.Replace(marker, value, StringComparison.Ordinal);
        }

        var open = text.IndexOf("{{", StringComparison.Ordinal);
        if (open >= 0)
        {
            var close = text.IndexOf("}}", open, StringComparison.Ordinal);
            var missing = close > open ? text.Substring(open, close - open + 2) : "{{";
            throw new ArgumentException($"MushError.{error} still contains {missing}.", nameof(tokens));
        }

        return text;
    }
}
