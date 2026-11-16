namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PennMUSH.Core.MUSHcode;

public static class MathFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Register("ADD", Add);
        registry.Register("SUB", Sub);
        registry.Register("MUL", Mul);
        registry.Register("DIV", Div);
        registry.Register("FDIV", FDiv);
        registry.Register("MOD", Mod);
        registry.Register("REMAINDER", Remainder);
        registry.Register("ABS", Abs);
        registry.Register("MIN", Min);
        registry.Register("MAX", Max);
        registry.Register("BOUND", Bound);
        registry.Register("INC", Inc);
        registry.Register("DEC", Dec);
        registry.Register("SIGN", Sign);
        registry.Register("ROUND", Round);
        registry.Register("FLOOR", Floor);
        registry.Register("CEIL", Ceil);
        registry.Register("TRUNC", Trunc);
        registry.Register("SQRT", Sqrt);
        registry.Register("POWER", Power);
        registry.Register("LOG", Log);
        registry.Register("LN", Ln);
        registry.Register("SIN", Sin);
        registry.Register("COS", Cos);
        registry.Register("TAN", Tan);
        registry.Register("ASIN", Asin);
        registry.Register("ACOS", Acos);
        registry.Register("ATAN", Atan);
        registry.Register("DIST2D", Dist2D);
        registry.Register("DIST3D", Dist3D);
        registry.Register("BAND", BAnd);
        registry.Register("BOR", BOr);
        registry.Register("BXOR", BXor);
        registry.Register("BNOT", BNot);
        registry.Register("SHL", Shl);
        registry.Register("SHR", Shr);
        registry.Register("MEAN", Mean);
        registry.Register("MEDIAN", Median);
        registry.Register("STDDEV", StdDev);
    }

    public static string Add(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double sum = 0;
        foreach (var a in args)
        {
            if (double.TryParse(a.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                sum += val;
        }
        return FormatNumber(sum);
    }

    public static string Sub(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double a);
        double.TryParse(args[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double b);
        return FormatNumber(a - b);
    }

    public static string Mul(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double prod = 1;
        foreach (var a in args)
        {
            if (double.TryParse(a.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                prod *= val;
            else
                return "0";
        }
        return FormatNumber(prod);
    }

    public static string Div(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double a);
        if (!double.TryParse(args[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double b) || b == 0)
            return MushError.DivisionByZero.Text();
        return ((long)(a / b)).ToString();
    }

    public static string FDiv(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double a);
        if (!double.TryParse(args[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double b) || b == 0)
            return MushError.DivisionByZero.Text();
        return FormatNumber(a / b);
    }

    public static string Mod(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        long.TryParse(args[0].Trim(), out long a);
        if (!long.TryParse(args[1].Trim(), out long b) || b == 0)
            return MushError.DivisionByZero.Text();
        return (a % b).ToString();
    }

    public static string Remainder(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double a);
        if (!double.TryParse(args[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double b) || b == 0)
            return MushError.DivisionByZero.Text();
        return FormatNumber(a % b);
    }

    public static string Abs(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double a);
        return FormatNumber(Math.Abs(a));
    }

    public static string Min(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double min = double.MaxValue;
        foreach (var a in args)
        {
            if (double.TryParse(a.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            {
                if (val < min) min = val;
            }
        }
        return min == double.MaxValue ? "0" : FormatNumber(min);
    }

    public static string Max(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double max = double.MinValue;
        foreach (var a in args)
        {
            if (double.TryParse(a.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            {
                if (val > max) max = val;
            }
        }
        return max == double.MinValue ? "0" : FormatNumber(max);
    }

    public static string Bound(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        double.TryParse(args[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double min);
        double.TryParse(args[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double max);
        return FormatNumber(Math.Clamp(val, min, max));
    }

    public static string Inc(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "1";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        double step = 1;
        if (args.Length > 1 && double.TryParse(args[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double s))
            step = s;
        return FormatNumber(val + step);
    }

    public static string Dec(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "-1";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        double step = 1;
        if (args.Length > 1 && double.TryParse(args[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double s))
            step = s;
        return FormatNumber(val - step);
    }

    public static string Sign(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        return Math.Sign(val).ToString();
    }

    public static string Round(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        int digits = 0;
        if (args.Length > 1 && int.TryParse(args[1].Trim(), out int d) && d >= 0) digits = d;
        return FormatNumber(Math.Round(val, digits, MidpointRounding.AwayFromZero));
    }

    public static string Floor(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        return FormatNumber(Math.Floor(val));
    }

    public static string Ceil(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        return FormatNumber(Math.Ceiling(val));
    }

    public static string Trunc(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        return FormatNumber(Math.Truncate(val));
    }

    public static string Sqrt(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        if (val < 0) return MushError.SquareRootOfNegative.Text();
        return FormatNumber(Math.Sqrt(val));
    }

    public static string Power(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double b);
        double.TryParse(args[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double e);
        return FormatNumber(Math.Pow(b, e));
    }

    public static string Log(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        if (val <= 0) return MushError.LogOfNonpositive.Text();
        double b = 10;
        if (args.Length > 1 && double.TryParse(args[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double userBase) && userBase > 0)
            b = userBase;
        return FormatNumber(Math.Log(val, b));
    }

    public static string Ln(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        if (val <= 0) return MushError.LogOfNonpositive.Text();
        return FormatNumber(Math.Log(val));
    }

    public static string Sin(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        return FormatNumber(Math.Sin(val));
    }

    public static string Cos(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        return FormatNumber(Math.Cos(val));
    }

    public static string Tan(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        return FormatNumber(Math.Tan(val));
    }

    public static string Asin(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        if (val < -1 || val > 1) return MushError.OutOfRange.Text();
        return FormatNumber(Math.Asin(val));
    }

    public static string Acos(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        if (val < -1 || val > 1) return MushError.OutOfRange.Text();
        return FormatNumber(Math.Acos(val));
    }

    public static string Atan(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        return FormatNumber(Math.Atan(val));
    }

    public static string Dist2D(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 4) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double x1);
        double.TryParse(args[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double y1);
        double.TryParse(args[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double x2);
        double.TryParse(args[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double y2);
        double dx = x1 - x2;
        double dy = y1 - y2;
        return FormatNumber(Math.Sqrt(dx * dx + dy * dy));
    }

    public static string Dist3D(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 6) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double x1);
        double.TryParse(args[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double y1);
        double.TryParse(args[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double z1);
        double.TryParse(args[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double x2);
        double.TryParse(args[4].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double y2);
        double.TryParse(args[5].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double z2);
        double dx = x1 - x2;
        double dy = y1 - y2;
        double dz = z1 - z2;
        return FormatNumber(Math.Sqrt(dx * dx + dy * dy + dz * dz));
    }

    public static string BAnd(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return "0";
        long.TryParse(args[0].Trim(), out long a);
        long.TryParse(args[1].Trim(), out long b);
        return (a & b).ToString();
    }

    public static string BOr(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return "0";
        long.TryParse(args[0].Trim(), out long a);
        long.TryParse(args[1].Trim(), out long b);
        return (a | b).ToString();
    }

    public static string BXor(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return "0";
        long.TryParse(args[0].Trim(), out long a);
        long.TryParse(args[1].Trim(), out long b);
        return (a ^ b).ToString();
    }

    public static string BNot(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        long.TryParse(args[0].Trim(), out long a);
        return (~a).ToString();
    }

    public static string Shl(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return "0";
        long.TryParse(args[0].Trim(), out long a);
        int.TryParse(args[1].Trim(), out int b);
        if (b < 0 || b > 63) return "0";
        return (a << b).ToString();
    }

    public static string Shr(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return "0";
        long.TryParse(args[0].Trim(), out long a);
        int.TryParse(args[1].Trim(), out int b);
        if (b < 0 || b > 63) return "0";
        return (a >> b).ToString();
    }

    public static string Mean(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        var nums = ParseDoubleList(args);
        if (nums.Count == 0) return "0";
        return FormatNumber(nums.Average());
    }

    public static string Median(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        var nums = ParseDoubleList(args);
        if (nums.Count == 0) return "0";
        nums.Sort();
        int mid = nums.Count / 2;
        if (nums.Count % 2 != 0) return FormatNumber(nums[mid]);
        return FormatNumber((nums[mid - 1] + nums[mid]) / 2.0);
    }

    public static string StdDev(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        var nums = ParseDoubleList(args);
        if (nums.Count <= 1) return "0";
        double avg = nums.Average();
        double sum = nums.Sum(d => Math.Pow(d - avg, 2));
        return FormatNumber(Math.Sqrt(sum / (nums.Count - 1)));
    }

    private static List<double> ParseDoubleList(string[] args)
    {
        var list = new List<double>();
        foreach (var a in args)
        {
            var parts = ListFunctions.SplitList(a, " ");
            foreach (var p in parts)
            {
                if (double.TryParse(p.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                {
                    list.Add(val);
                }
            }
        }
        return list;
    }

    private static string FormatNumber(double val)
    {
        if (val == Math.Floor(val) && !double.IsInfinity(val) && !double.IsNaN(val))
            return ((long)val).ToString();
        return val.ToString("G", CultureInfo.InvariantCulture);
    }
}
