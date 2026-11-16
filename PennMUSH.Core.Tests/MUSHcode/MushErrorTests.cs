namespace PennMUSH.Core.Tests.MUSHcode;

using System;
using System.Collections.Generic;
using Xunit;
using PennMUSH.Core.MUSHcode;

public class MushErrorTests
{
    [Fact]
    public void EveryMessageMatchesItsPennText()
    {
        Assert.Equal("#-1 NO MATCH", MushError.NoMatch.Text());
        Assert.Equal("#-1 TOO FEW ARGUMENTS", MushError.TooFewArguments.Text());
        Assert.Equal("#-1", MushError.Nothing.Text());
        Assert.Equal("#-1 REGEX TIMEOUT", MushError.RegexTimeout.Text());
        Assert.Equal("#-1 INVALID PLAYER", MushError.InvalidPlayer.Text());
        Assert.Equal("#-1 DIVISION BY ZERO", MushError.DivisionByZero.Text());
        Assert.Equal("#-1 FUNCTION (ADD) NOT FOUND", MushError.FunctionNotFound.Text(("function", "ADD")));
        Assert.Equal("#-1 LOG OF NONPOSITIVE", MushError.LogOfNonpositive.Text());
        Assert.Equal("#-1 OUT OF RANGE", MushError.OutOfRange.Text());
        Assert.Equal("#-1 TIMEOUT (RUNAWAY LOOP DETECTED)", MushError.RunawayLoop.Text());
        Assert.Equal("#-1 EXECUTION HALTED", MushError.ExecutionHalted.Text());
        Assert.Equal("#-1 FUNCTION INVOCATION LIMIT EXCEEDED", MushError.FunctionInvocationLimitExceeded.Text());
        Assert.Equal("#-1 FUNCTION RECURSION LIMIT EXCEEDED", MushError.FunctionRecursionLimitExceeded.Text());
        Assert.Equal("#-1 INVALID TARGET", MushError.InvalidTarget.Text());
        Assert.Equal("#-1 NO CODE", MushError.NoCode.Text());
        Assert.Equal("#-1 NO COMMAND", MushError.NoCommand.Text());
        Assert.Equal("#-1 PERMISSION DENIED", MushError.PermissionDenied.Text());
        Assert.Equal("#-1 QUEUE FULL", MushError.QueueFull.Text());
        Assert.Equal("#-1 RECURSION CYCLE DETECTED", MushError.RecursionCycleDetected.Text());
        Assert.Equal("#-1 SQUARE ROOT OF NEGATIVE", MushError.SquareRootOfNegative.Text());
    }

    [Fact]
    public void EveryMemberHasATemplate()
    {
        var sample = new Dictionary<MushError, (string Name, string Value)[]>
        {
            [MushError.FunctionNotFound] = new[] { ("function", "ADD") },
            [MushError.FunctionExpects] = new[] { ("function", "ADD"), ("range", "2"), ("got", "1") },
        };

        foreach (MushError error in Enum.GetValues<MushError>())
        {
            var tokens = sample.TryGetValue(error, out var needed)
                ? needed
                : Array.Empty<(string Name, string Value)>();
            var text = error.Text(tokens);
            Assert.StartsWith("#-1", text);
            Assert.DoesNotContain("{{", text);
        }
    }

    [Fact]
    public void MissingTokenThrows()
    {
        var ex = Assert.Throws<ArgumentException>(() => MushError.FunctionNotFound.Text());
        Assert.Contains("{{function}}", ex.Message);
    }

    [Fact]
    public void UnknownTokenThrows()
    {
        var ex = Assert.Throws<ArgumentException>(() => MushError.DivisionByZero.Text(("function", "ADD")));
        Assert.Contains("no {{function}} token", ex.Message);
    }
}
