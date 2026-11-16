namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;
using PennMUSH.Core.MUSHcode;

public static class AsyncFunctions
{
    private static int _nextTaskId = 1;
    private static ActionQueue? _queue;

    public static void Initialize(ActionQueue queue)
    {
        _queue = queue;
    }

    public static void Register(FunctionRegistry registry, ActionQueue? queue = null)
    {
        if (queue != null) _queue = queue;

        registry.Register("ASYNC", (ctx, args) => AsyncFunc(registry, ctx, args));
        registry.Register("PARALLEL_MAP", (ctx, args) => ParallelMapFunc(registry, ctx, args));
        registry.Register("BGTASK", (ctx, args) => BgTaskFunc(registry, ctx, args));
    }

    public static string AsyncFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Evaluator == null) return MushError.NoCode.Text();

        string codeToRun = args[0];
        string? callback = args.Length > 1 ? args[1].Trim() : null;
        int taskId = Interlocked.Increment(ref _nextTaskId);

        // Capture isolated context for thread execution
        var clonedCtx = new EvaluationContext(ctx.Executor, ctx.Enactor, ctx.Caller, ctx.Location, (string[])ctx.Arguments.Clone());

        var queue = reg.Queue ?? _queue;
        Task.Run(() =>
        {
            try
            {
                string result = reg.Evaluator.Evaluate(codeToRun, clonedCtx);

                if (!string.IsNullOrWhiteSpace(callback) && queue != null)
                {
                    // Enqueue callback command with result in %0
                    string cmd = callback.Contains("%0") ? callback.Replace("%0", result) : $"{callback} {result}";
                    queue.Enqueue(clonedCtx.Executor, clonedCtx.Enactor, cmd, args: new[] { result });
                }
            }
            catch
            {
                // Background exception suppressed to protect game tick
            }
        });

        return $"TASK#{taskId}";
    }

    public static string ParallelMapFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3 || reg.Evaluator == null) return string.Empty;

        string listStr = args[0];
        string delimiter = string.IsNullOrEmpty(args[1]) ? " " : args[1];
        string ufunName = args[2];

        if (string.IsNullOrEmpty(listStr)) return string.Empty;

        string[] items = listStr.Split(new[] { delimiter }, StringSplitOptions.None);
        string[] results = new string[items.Length];

        // Process items concurrently across multi-core processors
        Parallel.For(0, items.Length, i =>
        {
            string item = items[i];
            var childCtx = ctx.CreateChildContext(ctx.Executor, ctx.Enactor, new[] { item });
            results[i] = reg.Evaluator.Evaluate($"[u({ufunName}, {item})]", childCtx);
        });

        return string.Join(delimiter, results);
    }

    public static string BgTaskFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || _queue == null) return MushError.NoCommand.Text();
        string command = args[0];

        var entry = _queue.Enqueue(ctx.Executor, ctx.Enactor, command);
        return entry != null ? $"PID#{entry.Pid}" : MushError.QueueFull.Text();
    }
}
