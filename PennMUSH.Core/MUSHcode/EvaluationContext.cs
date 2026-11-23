using System;
using System.Collections.Generic;
using System.Threading;
using PennMUSH.Core.Types;

namespace PennMUSH.Core.MUSHcode
{
    public record EvaluationContext
    {
        public Dbref Executor { get; init; }
        public Dbref Enactor { get; init; }
        public Dbref Caller { get; init; }
        public Dbref Location { get; init; }
        public string[] Arguments { get; set; }
        public Dictionary<string, string> Registers { get; private set; }
        public Dictionary<string, string>? RegexCaptures { get; set; }
        public HashSet<string> CallTrail { get; private set; }
        public List<(string Text, int Number)> IterStack { get; private set; } = new();
        public List<string> SwitchStack { get; private set; } = new();
        public bool IterBreakRequested { get; set; }
        public bool NoSideFX { get; set; }

        public int RecursionDepth { get; set; } = 0;
        public int MaxRecursionDepth { get; set; } = 50;
        public int InvocationCount { get; set; } = 0;
        public int MaxInvocations { get; set; } = 250000;

        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
        public DateTimeOffset? ExecutionDeadline { get; set; }

        public EvaluationContext(Dbref executor, Dbref enactor, Dbref caller = default, Dbref location = default, string[]? args = null)
        {
            Executor = executor;
            Enactor = enactor;
            Caller = caller == default ? executor : caller;
            Location = location == default ? Dbref.Nothing : location;
            Arguments = args ?? Array.Empty<string>();
            Registers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            CallTrail = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public string GetArgument(int index) => (index >= 0 && index < Arguments.Length) ? (Arguments[index] ?? string.Empty) : string.Empty;

        public void SetRegister(string name, string value) => Registers[name] = value;

        public string GetRegister(string name) => Registers.TryGetValue(name, out var val) ? val : string.Empty;

        public string GetRegexCapture(string name)
            => RegexCaptures != null && RegexCaptures.TryGetValue(name, out var val) ? val : string.Empty;

        public static Dictionary<string, string> BuildRegexCaptures(System.Text.RegularExpressions.Match match)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < match.Groups.Count; i++)
                dict[i.ToString()] = match.Groups[i].Value;
            foreach (System.Text.RegularExpressions.Group g in match.Groups)
            {
                if (!string.IsNullOrEmpty(g.Name))
                    dict[g.Name] = g.Value;
            }
            return dict;
        }

        public Dictionary<string, string> CloneRegisters()
            => new(Registers, StringComparer.OrdinalIgnoreCase);

        public void RestoreRegisters(Dictionary<string, string> saved)
            => Registers = new Dictionary<string, string>(saved, StringComparer.OrdinalIgnoreCase);

        public bool PushCall(Dbref target, string attrName)
        {
            string key = $"{target}/{attrName}";
            return CallTrail.Add(key);
        }

        public void PopCall(Dbref target, string attrName)
        {
            string key = $"{target}/{attrName}";
            CallTrail.Remove(key);
        }

        public bool CheckExecutionBudget(out string? error)
        {
            if (CancellationToken.IsCancellationRequested)
            {
                error = MushError.RunawayLoop.Text();
                return false;
            }

            if (ExecutionDeadline.HasValue && DateTimeOffset.UtcNow > ExecutionDeadline.Value)
            {
                error = MushError.RunawayLoop.Text();
                return false;
            }

            if (RecursionDepth > MaxRecursionDepth)
            {
                error = MushError.FunctionRecursionLimitExceeded.Text();
                return false;
            }

            if (++InvocationCount > MaxInvocations)
            {
                error = MushError.FunctionInvocationLimitExceeded.Text();
                return false;
            }

            error = null;
            return true;
        }

        public EvaluationContext CreateChildContext(Dbref newExecutor, Dbref newCaller, string[]? newArgs = null)
        {
            return new EvaluationContext(newExecutor, this.Enactor, newCaller, this.Location, newArgs)
            {
                RecursionDepth = this.RecursionDepth + 1,
                MaxRecursionDepth = this.MaxRecursionDepth,
                InvocationCount = this.InvocationCount,
                MaxInvocations = this.MaxInvocations,
                CancellationToken = this.CancellationToken,
                ExecutionDeadline = this.ExecutionDeadline,
                Registers = this.Registers,
                RegexCaptures = RegexCaptures != null ? new Dictionary<string, string>(RegexCaptures, StringComparer.Ordinal) : null,
                CallTrail = new HashSet<string>(CallTrail, StringComparer.OrdinalIgnoreCase),
                IterStack = new List<(string Text, int Number)>(IterStack),
                SwitchStack = new List<string>(SwitchStack)
            };
        }
    }
}
