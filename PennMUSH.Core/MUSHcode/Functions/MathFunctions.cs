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
        registry.Register("FLOORDIV", FloorDiv);
        registry.Register("FDIV", FDiv);
        registry.Register("MOD", Modulo);
        registry.Register("MODULO", Modulo);
        registry.Register("MODULUS", Modulo);
        registry.Register("REMAINDER", Remainder);
        registry.Register("FMOD", FMod);
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
        registry.Register("VAL", Trunc);
        registry.Register("SQRT", Sqrt);
        registry.Register("POWER", Power);
        registry.Register("LOG", Log);
        registry.Register("LN", Ln);
        registry.Register("PI", Pi);
        registry.Register("E", EFunc);
        registry.Register("EXP", EFunc);
        registry.Register("SIN", Sin);
        registry.Register("COS", Cos);
        registry.Register("TAN", Tan);
        registry.Register("ASIN", Asin);
        registry.Register("ACOS", Acos);
        registry.Register("ATAN", Atan);
        registry.Register("ATAN2", Atan2);
        registry.Register("CTU", Ctu);
        registry.Register("FRACTION", Fraction);
        registry.Register("DIST2D", Dist2D);
        registry.Register("DIST3D", Dist3D);
        registry.Register("BAND", BAnd);
        registry.Register("BOR", BOr);
        registry.Register("BXOR", BXor);
        registry.Register("BNAND", BNand);
        registry.Register("BNOT", BNot);
        registry.Register("SHL", Shl);
        registry.Register("SHR", Shr);
        registry.Register("MEAN", Mean);
        registry.Register("AVG", Mean);
        registry.Register("MEDIAN", Median);
        registry.Register("STDDEV", StdDev);
        registry.Register("LMATH", LMath);
        registry.Register("BASECONV", BaseConv);
        registry.Register("RAND", Rand);
        registry.Register("DIE", Die);
        registry.Register("VADD", VAdd);
        registry.Register("VSUB", VSub);
        registry.Register("VMUL", VMul);
        registry.Register("VDOT", VDot);
        registry.Register("VCROSS", VCross);
        registry.Register("VMAG", VMag);
        registry.Register("VDIM", VDim);
        registry.Register("VUNIT", VUnit);
        registry.Register("VMAX", VMax);
        registry.Register("VMIN", VMin);
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
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double acc);
        for (int i = 1; i < args.Length; i++)
        {
            double.TryParse(args[i].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double next);
            acc -= next;
        }
        return FormatNumber(acc);
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
        if (!long.TryParse(args[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long acc))
            return MushError.ArgumentsMustBeIntegers.Text();
        for (int i = 1; i < args.Length; i++)
        {
            if (!long.TryParse(args[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long d))
                return MushError.ArgumentsMustBeIntegers.Text();
            if (d == 0) return MushError.DivisionByZero.Text();
            acc /= d;
        }
        return acc.ToString(CultureInfo.InvariantCulture);
    }

    public static string FloorDiv(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (!long.TryParse(args[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long acc))
            return MushError.ArgumentsMustBeIntegers.Text();
        for (int i = 1; i < args.Length; i++)
        {
            if (!long.TryParse(args[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long d))
                return MushError.ArgumentsMustBeIntegers.Text();
            if (d == 0) return MushError.DivisionByZero.Text();
            long q = acc / d;
            long rem = acc % d;
            if (rem != 0 && ((rem < 0) != (d < 0)))
                q--;
            acc = q;
        }
        return acc.ToString(CultureInfo.InvariantCulture);
    }

    public static string FDiv(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double acc);
        for (int i = 1; i < args.Length; i++)
        {
            if (!double.TryParse(args[i].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double d) || d == 0)
                return MushError.DivisionByZero.Text();
            acc /= d;
        }
        return FormatNumber(acc);
    }

    public static string Modulo(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        long.TryParse(args[0].Trim(), out long acc);
        for (int i = 1; i < args.Length; i++)
        {
            if (!long.TryParse(args[i].Trim(), out long d) || d == 0)
                return MushError.DivisionByZero.Text();
            long r = acc % d;
            if (r != 0 && ((r < 0) != (d < 0)))
                r += d;
            acc = r;
        }
        return acc.ToString(CultureInfo.InvariantCulture);
    }

    public static string Remainder(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (!long.TryParse(args[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long acc))
            return MushError.ArgumentsMustBeIntegers.Text();
        for (int i = 1; i < args.Length; i++)
        {
            if (!long.TryParse(args[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long d))
                return MushError.ArgumentsMustBeIntegers.Text();
            if (d == 0) return MushError.DivisionByZero.Text();
            acc %= d;
        }
        return acc.ToString(CultureInfo.InvariantCulture);
    }

    public static string FMod(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (!double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double a) ||
            !double.TryParse(args[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double b))
            return MushError.ArgumentsMustBeNumbers.Text();
        if (b == 0) return MushError.DivisionByZero.Text();
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
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (!double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val) ||
            !double.TryParse(args[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double min))
            return MushError.ArgumentsMustBeNumbers.Text();
        if (args.Length < 3 || string.IsNullOrWhiteSpace(args[2]))
            return FormatNumber(Math.Max(val, min));
        if (!double.TryParse(args[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double max))
            return MushError.ArgumentsMustBeNumbers.Text();
        return FormatNumber(Math.Clamp(val, min, max));
    }

    private static string IncDecCore(string[] args, int delta)
    {
        if (args.Length == 0) return MushError.TooFewArguments.Text();
        string raw = args[0].Trim();
        if (string.IsNullOrEmpty(raw)) return delta > 0 ? "1" : "-1";

        int idx = raw.Length - 1;
        while (idx >= 0 && char.IsDigit(raw[idx])) idx--;
        if (idx >= 0 && raw[idx] == '-' && idx + 1 < raw.Length) idx--;

        int numStart = idx + 1;
        if (numStart >= raw.Length || !long.TryParse(raw.AsSpan(numStart), NumberStyles.Integer, CultureInfo.InvariantCulture, out long val))
            return "#-1 ARGUMENT MUST END IN AN INTEGER";

        string prefix = raw.Substring(0, numStart);
        return prefix + (val + delta).ToString(CultureInfo.InvariantCulture);
    }

    public static string Inc(EvaluationContext ctx, string[] args) => IncDecCore(args, 1);

    public static string Dec(EvaluationContext ctx, string[] args) => IncDecCore(args, -1);

    public static string Sign(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        return Math.Sign(val).ToString();
    }

    public static string Round(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (!double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            return MushError.ArgumentsMustBeNumbers.Text();
        if (!int.TryParse(args[1].Trim(), out int digits) || digits < 0)
            return MushError.ArgumentMustBeInteger.Text();
        double rounded = Math.Round(val, digits, MidpointRounding.AwayFromZero);
        bool pad = args.Length > 2 && LogicFunctions.IsTruthy(args[2]);
        return pad
            ? rounded.ToString($"F{digits}", CultureInfo.InvariantCulture)
            : FormatNumber(rounded);
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
        string s = args[0].TrimStart();
        if (string.IsNullOrEmpty(s)) return "0";
        int end = (s[0] == '-' || s[0] == '+') ? 1 : 0;
        while (end < s.Length && char.IsDigit(s[end])) end++;
        if (end == 0 || (end == 1 && (s[0] == '-' || s[0] == '+')))
            return "0";
        return long.TryParse(s.AsSpan(0, end), NumberStyles.Integer, CultureInfo.InvariantCulture, out long val)
            ? val.ToString(CultureInfo.InvariantCulture)
            : "0";
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
        if (args.Length == 0) return MushError.TooFewArguments.Text();
        if (!double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            return MushError.ArgumentsMustBeNumbers.Text();
        if (val <= 0) return MushError.LogOfNonpositive.Text();
        if (args.Length > 1 && args[1].Trim().Equals("e", StringComparison.OrdinalIgnoreCase))
            return FormatNumber(Math.Log(val));

        double b = 10;
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
        {
            if (!double.TryParse(args[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out b))
                return MushError.ArgumentsMustBeNumbers.Text();
            if (b <= 1) return "#-1 BASE OUT OF RANGE";
        }
        return FormatNumber(Math.Log(val, b));
    }

    public static string Ln(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        if (val <= 0) return MushError.LogOfNonpositive.Text();
        return FormatNumber(Math.Log(val));
    }

    public static string Pi(EvaluationContext ctx, string[] args) => FormatNumber(Math.PI);

    public static string EFunc(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return FormatNumber(Math.E);
        if (!double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            return MushError.ArgumentMustBeNumber.Text();
        return FormatNumber(Math.Exp(val));
    }

    private static bool TryConvertAngleToRadians(double val, string? unitArg, out double radians)
    {
        if (string.IsNullOrWhiteSpace(unitArg))
        {
            radians = val;
            return true;
        }
        switch (char.ToLowerInvariant(unitArg.Trim()[0]))
        {
            case 'r':
                radians = val;
                return true;
            case 'd':
                radians = val * (Math.PI / 180.0);
                return true;
            case 'g':
                radians = val * (Math.PI / 200.0);
                return true;
            default:
                radians = 0;
                return false;
        }
    }

    private static bool TryConvertRadiansToAngle(double radians, string? unitArg, out double result)
    {
        if (string.IsNullOrWhiteSpace(unitArg))
        {
            result = radians;
            return true;
        }
        switch (char.ToLowerInvariant(unitArg.Trim()[0]))
        {
            case 'r':
                result = radians;
                return true;
            case 'd':
                result = radians * (180.0 / Math.PI);
                return true;
            case 'g':
                result = radians * (200.0 / Math.PI);
                return true;
            default:
                result = 0;
                return false;
        }
    }

    public static string Sin(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        if (args.Length > 1 && !TryConvertAngleToRadians(val, args[1], out val))
            return MushError.InvalidAngleType.Text();
        return FormatNumber(Math.Sin(val));
    }

    public static string Cos(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        if (args.Length > 1 && !TryConvertAngleToRadians(val, args[1], out val))
            return MushError.InvalidAngleType.Text();
        return FormatNumber(Math.Cos(val));
    }

    public static string Tan(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        if (args.Length > 1 && !TryConvertAngleToRadians(val, args[1], out val))
            return MushError.InvalidAngleType.Text();
        return FormatNumber(Math.Tan(val));
    }

    public static string Asin(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        if (val < -1 || val > 1) return MushError.OutOfRange.Text();
        double rad = Math.Asin(val);
        if (args.Length > 1 && !TryConvertRadiansToAngle(rad, args[1], out rad))
            return MushError.InvalidAngleType.Text();
        return FormatNumber(rad);
    }

    public static string Acos(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        if (val < -1 || val > 1) return MushError.OutOfRange.Text();
        double rad = Math.Acos(val);
        if (args.Length > 1 && !TryConvertRadiansToAngle(rad, args[1], out rad))
            return MushError.InvalidAngleType.Text();
        return FormatNumber(rad);
    }

    public static string Atan(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val);
        double rad = Math.Atan(val);
        if (args.Length > 1 && !TryConvertRadiansToAngle(rad, args[1], out rad))
            return MushError.InvalidAngleType.Text();
        return FormatNumber(rad);
    }

    public static string Atan2(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (!double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double first) ||
            !double.TryParse(args[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double second))
            return MushError.ArgumentsMustBeNumbers.Text();
        double rad = Math.Atan2(first, second);
        if (args.Length > 2 && !TryConvertRadiansToAngle(rad, args[2], out rad))
            return MushError.InvalidAngleType.Text();
        return FormatNumber(rad);
    }

    public static string Ctu(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return MushError.TooFewArguments.Text();
        if (!double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double angle))
            return MushError.ArgumentMustBeNumber.Text();
        if (string.IsNullOrWhiteSpace(args[1]) || string.IsNullOrWhiteSpace(args[2]) ||
            !TryConvertAngleToRadians(angle, args[1], out double rad) ||
            !TryConvertRadiansToAngle(rad, args[2], out double converted))
            return MushError.InvalidAngleType.Text();
        return FormatNumber(converted);
    }

    public static string Fraction(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        if (!double.TryParse(args[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            return MushError.ArgumentMustBeNumber.Text();
        bool mixed = args.Length > 1 && LogicFunctions.IsTruthy(args[1]);

        if (Math.Abs(val - Math.Round(val)) < 1e-9)
            return ((long)Math.Round(val)).ToString(CultureInfo.InvariantCulture);

        int sign = val < 0 ? -1 : 1;
        val = Math.Abs(val);

        long h1 = 1, h2 = 0, k1 = 0, k2 = 1;
        double b = val;
        for (int iter = 0; iter < 25; iter++)
        {
            long a = (long)Math.Floor(b);
            long nextH = a * h1 + h2;
            long nextK = a * k1 + k2;
            if (nextK > 100000)
                break;
            h2 = h1;
            h1 = nextH;
            k2 = k1;
            k1 = nextK;
            if (Math.Abs(val - (double)h1 / k1) < 1e-7 || b - a < 1e-9)
                break;
            b = 1.0 / (b - a);
        }

        if (k1 <= 1)
            return (sign * h1).ToString(CultureInfo.InvariantCulture);

        if (mixed && h1 > k1)
        {
            long whole = (h1 / k1) * sign;
            long rem = h1 % k1;
            return rem == 0
                ? whole.ToString(CultureInfo.InvariantCulture)
                : $"{whole} {rem}/{k1}";
        }

        return $"{sign * h1}/{k1}";
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
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (!ulong.TryParse(args[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong acc))
            return "#-1 ARGUMENTS MUST BE UNSIGNED INTEGERS";
        for (int i = 1; i < args.Length; i++)
        {
            if (!ulong.TryParse(args[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong b))
                return "#-1 ARGUMENTS MUST BE UNSIGNED INTEGERS";
            acc &= b;
        }
        return acc.ToString(CultureInfo.InvariantCulture);
    }

    public static string BOr(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (!ulong.TryParse(args[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong acc))
            return "#-1 ARGUMENTS MUST BE UNSIGNED INTEGERS";
        for (int i = 1; i < args.Length; i++)
        {
            if (!ulong.TryParse(args[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong b))
                return "#-1 ARGUMENTS MUST BE UNSIGNED INTEGERS";
            acc |= b;
        }
        return acc.ToString(CultureInfo.InvariantCulture);
    }

    public static string BXor(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (!ulong.TryParse(args[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong acc))
            return "#-1 ARGUMENTS MUST BE UNSIGNED INTEGERS";
        for (int i = 1; i < args.Length; i++)
        {
            if (!ulong.TryParse(args[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong b))
                return "#-1 ARGUMENTS MUST BE UNSIGNED INTEGERS";
            acc ^= b;
        }
        return acc.ToString(CultureInfo.InvariantCulture);
    }

    public static string BNand(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (!ulong.TryParse(args[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong a) ||
            !ulong.TryParse(args[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong b))
            return "#-1 ARGUMENTS MUST BE UNSIGNED INTEGERS";
        return (a & ~b).ToString(CultureInfo.InvariantCulture);
    }

    public static string BNot(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return MushError.TooFewArguments.Text();
        if (!ulong.TryParse(args[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong a))
            return "#-1 ARGUMENT MUST BE UNSIGNED INTEGER";
        return (~a).ToString(CultureInfo.InvariantCulture);
    }

    public static string Shl(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (!ulong.TryParse(args[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong a) ||
            !int.TryParse(args[1].Trim(), out int b))
            return "#-1 ARGUMENTS MUST BE UNSIGNED INTEGERS";
        if (b < 0 || b > 63) return "0";
        return (a << b).ToString(CultureInfo.InvariantCulture);
    }

    public static string Shr(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (!ulong.TryParse(args[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong a) ||
            !int.TryParse(args[1].Trim(), out int b))
            return "#-1 ARGUMENTS MUST BE UNSIGNED INTEGERS";
        if (b < 0 || b > 63) return "0";
        return (a >> b).ToString(CultureInfo.InvariantCulture);
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

    public static string LMath(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (args.Length > 2 && args[2].Length > 1)
            return MushError.SeparatorMustBeOneCharacter.Text();

        string sep = args.Length > 2 && args[2].Length > 0 ? args[2] : " ";
        string[] items = string.IsNullOrWhiteSpace(args[1])
            ? Array.Empty<string>()
            : ListFunctions.SplitList(args[1], sep).ToArray();

        return args[0].Trim().ToLowerInvariant() switch
        {
            "add" or "+" => Add(ctx, items),
            "sub" or "-" => Sub(ctx, items),
            "mul" or "*" => Mul(ctx, items),
            "div" or "/" => Div(ctx, items),
            "floordiv" => FloorDiv(ctx, items),
            "fdiv" => FDiv(ctx, items),
            "modulo" or "mod" or "modulus" or "%" => Modulo(ctx, items),
            "fmod" => FMod(ctx, items),
            "remainder" => Remainder(ctx, items),
            "max" => Max(ctx, items),
            "min" => Min(ctx, items),
            "mean" or "avg" => Mean(ctx, items),
            "median" => Median(ctx, items),
            "stddev" => StdDev(ctx, items),
            "band" or "&" => BAnd(ctx, items),
            "bor" or "|" => BOr(ctx, items),
            "bxor" or "^" => BXor(ctx, items),
            "bnand" => BNand(ctx, items),
            "and" or "&&" => LogicFunctions.And(ctx, items),
            "or" or "||" => LogicFunctions.Or(ctx, items),
            "xor" => LogicFunctions.Xor(ctx, items),
            "nand" => LogicFunctions.Nand(ctx, items),
            "nor" => LogicFunctions.Nor(ctx, items),
            "eq" or "=" or "==" => LogicFunctions.Eq(ctx, items),
            "neq" or "!=" or "<>" => LogicFunctions.Neq(ctx, items),
            "gt" or ">" => LogicFunctions.Gt(ctx, items),
            "gte" or ">=" => LogicFunctions.Gte(ctx, items),
            "lt" or "<" => LogicFunctions.Lt(ctx, items),
            "lte" or "<=" => LogicFunctions.Lte(ctx, items),
            "dist2d" => Dist2D(ctx, items),
            "dist3d" => Dist3D(ctx, items),
            _ => MushError.UnknownOperation.Text()
        };
    }

    public static string BaseConv(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return MushError.TooFewArguments.Text();
        if (!int.TryParse(args[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int fromBase) ||
            !int.TryParse(args[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int toBase))
            return MushError.ArgumentsMustBeIntegers.Text();

        if (fromBase < 2 || fromBase > 64) return MushError.FromBaseOutOfRange.Text();
        if (toBase < 2 || toBase > 64) return MushError.ToBaseOutOfRange.Text();

        string input = args[0].Trim();
        bool isNegative = false;
        int idx = 0;
        if (fromBase < 63 && toBase < 63 && input.Length > 0 && input[0] == '-')
        {
            isNegative = true;
            idx = 1;
        }

        ulong value = 0;
        for (; idx < input.Length; idx++)
        {
            int digit = DecodeBaseDigit(input[idx], fromBase);
            if (digit < 0 || digit >= fromBase)
                return MushError.MalformedNumber.Text();
            value = value * (ulong)fromBase + (ulong)digit;
        }

        const string alphabet36 = "0123456789abcdefghijklmnopqrstuvwxyz";
        const string alphabet64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        string toAlphabet = toBase <= 36 ? alphabet36 : alphabet64;

        if (value == 0)
            return (isNegative ? "-" : string.Empty) + toAlphabet[0];

        var chars = new Stack<char>();
        while (value > 0)
        {
            chars.Push(toAlphabet[(int)(value % (ulong)toBase)]);
            value /= (ulong)toBase;
        }

        return (isNegative ? "-" : string.Empty) + new string(chars.ToArray());
    }

    private static int DecodeBaseDigit(char c, int fromBase)
    {
        if (fromBase <= 36)
        {
            if (c is >= '0' and <= '9') return c - '0';
            if (c is >= 'a' and <= 'z') return c - 'a' + 10;
            if (c is >= 'A' and <= 'Z') return c - 'A' + 10;
            return -1;
        }

        if (c is >= 'A' and <= 'Z') return c - 'A';
        if (c is >= 'a' and <= 'z') return c - 'a' + 26;
        if (c is >= '0' and <= '9') return c - '0' + 52;
        if (c is '-' or '+') return 62;
        if (c is '_' or '/') return 63;
        return -1;
    }

    public static string Rand(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0)
            return Random.Shared.NextDouble().ToString("0.######", CultureInfo.InvariantCulture);

        if (args.Length == 1)
        {
            if (!long.TryParse(args[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long bound) || bound == 0)
                return MushError.ArgumentMustBePositiveInteger.Text();
            if (bound < 0)
                return (-Random.Shared.NextInt64(0, -bound)).ToString(CultureInfo.InvariantCulture);
            return Random.Shared.NextInt64(0, bound).ToString(CultureInfo.InvariantCulture);
        }

        if (!long.TryParse(args[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long low) ||
            !long.TryParse(args[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long high))
            return MushError.ArgumentsMustBeIntegers.Text();

        if (low > high)
            (low, high) = (high, low);

        if (high == long.MaxValue)
            return Random.Shared.NextInt64(low, high).ToString(CultureInfo.InvariantCulture);
        return Random.Shared.NextInt64(low, high + 1).ToString(CultureInfo.InvariantCulture);
    }

    public static string Die(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (!int.TryParse(args[0].Trim(), out int n) ||
            !int.TryParse(args[1].Trim(), out int sides) ||
            n <= 0 || sides <= 0)
            return MushError.ArgumentsMustBePositiveIntegers.Text();
        if (n > 700)
            return MushError.NumberOutOfRange.Text();

        bool showAll = args.Length > 2 && LogicFunctions.IsTruthy(args[2]);
        if (showAll)
        {
            var rolls = new int[n];
            for (int i = 0; i < n; i++)
                rolls[i] = Random.Shared.Next(1, sides + 1);
            return string.Join(" ", rolls);
        }

        long total = 0;
        for (int i = 0; i < n; i++)
            total += Random.Shared.Next(1, sides + 1);
        return total.ToString(CultureInfo.InvariantCulture);
    }

    private static bool TryGetSeparators(string[] args, int sepIndex, out string sep, out string osep, out string? error)
    {
        sep = " ";
        osep = " ";
        error = null;

        if (args.Length > sepIndex)
        {
            if (args[sepIndex].Length > 1)
            {
                error = MushError.SeparatorMustBeOneCharacter.Text();
                return false;
            }
            if (args[sepIndex].Length == 1)
                sep = args[sepIndex];
        }

        osep = sep;
        if (args.Length > sepIndex + 1)
        {
            if (args[sepIndex + 1].Length > 1)
            {
                error = MushError.SeparatorMustBeOneCharacter.Text();
                return false;
            }
            if (args[sepIndex + 1].Length == 1)
                osep = args[sepIndex + 1];
        }

        return true;
    }

    private static bool TryParseVector(string raw, string sep, out double[] vec, out string? error)
    {
        var tokens = ListFunctions.SplitList(raw, sep);
        if (tokens.Count == 0)
        {
            vec = Array.Empty<double>();
            error = MushError.VectorMustNotBeEmpty.Text();
            return false;
        }

        vec = new double[tokens.Count];
        for (int i = 0; i < tokens.Count; i++)
        {
            if (!double.TryParse(tokens[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out vec[i]))
            {
                error = MushError.ArgumentsMustBeNumbers.Text();
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool TryParseTwoVectors(string[] args, out double[] v1, out double[] v2, out string sep, out string osep, out string? error)
    {
        v1 = Array.Empty<double>();
        v2 = Array.Empty<double>();
        if (!TryGetSeparators(args, 2, out sep, out osep, out error))
            return false;

        if (args.Length < 2)
        {
            error = MushError.VectorsMustBeSameDimensions.Text();
            return false;
        }

        if (!TryParseVector(args[0], sep, out v1, out var e1))
        {
            error = e1 == MushError.VectorMustNotBeEmpty.Text() ? MushError.VectorsMustBeSameDimensions.Text() : e1;
            return false;
        }

        if (!TryParseVector(args[1], sep, out v2, out var e2))
        {
            error = e2 == MushError.VectorMustNotBeEmpty.Text() ? MushError.VectorsMustBeSameDimensions.Text() : e2;
            return false;
        }

        return true;
    }

    private static string FormatVector(double[] values, string osep)
        => string.Join(osep, values.Select(FormatNumber));

    private static string ApplyElementwiseVectorOp(string[] args, Func<double, double, double> op)
    {
        if (!TryParseTwoVectors(args, out var v1, out var v2, out _, out var osep, out var error))
            return error!;
        if (v1.Length != v2.Length)
            return MushError.VectorsMustBeSameDimensions.Text();

        var result = new double[v1.Length];
        for (int i = 0; i < v1.Length; i++)
            result[i] = op(v1[i], v2[i]);
        return FormatVector(result, osep);
    }

    public static string VAdd(EvaluationContext ctx, string[] args) => ApplyElementwiseVectorOp(args, (a, b) => a + b);
    public static string VSub(EvaluationContext ctx, string[] args) => ApplyElementwiseVectorOp(args, (a, b) => a - b);
    public static string VMax(EvaluationContext ctx, string[] args) => ApplyElementwiseVectorOp(args, Math.Max);
    public static string VMin(EvaluationContext ctx, string[] args) => ApplyElementwiseVectorOp(args, Math.Min);

    public static string VMul(EvaluationContext ctx, string[] args)
    {
        if (!TryParseTwoVectors(args, out var v1, out var v2, out _, out var osep, out var error))
            return error!;

        if (v1.Length == 1)
            return FormatVector(v2.Select(x => v1[0] * x).ToArray(), osep);
        if (v2.Length == 1)
            return FormatVector(v1.Select(x => x * v2[0]).ToArray(), osep);
        if (v1.Length != v2.Length)
            return MushError.VectorsMustBeSameDimensions.Text();

        var result = new double[v1.Length];
        for (int i = 0; i < v1.Length; i++)
            result[i] = v1[i] * v2[i];
        return FormatVector(result, osep);
    }

    public static string VDot(EvaluationContext ctx, string[] args)
    {
        if (!TryParseTwoVectors(args, out var v1, out var v2, out _, out _, out var error))
            return error!;
        if (v1.Length != v2.Length)
            return MushError.VectorsMustBeSameDimensions.Text();

        double sum = 0;
        for (int i = 0; i < v1.Length; i++)
            sum += v1[i] * v2[i];
        return FormatNumber(sum);
    }

    public static string VCross(EvaluationContext ctx, string[] args)
    {
        if (!TryParseTwoVectors(args, out var v1, out var v2, out _, out var osep, out var error))
            return error!;
        if (v1.Length != v2.Length)
            return MushError.VectorsMustBeSameDimensions.Text();
        if (v1.Length != 3)
            return MushError.VectorsMustBeThreeDimensional.Text();

        double[] cross =
        [
            v1[1] * v2[2] - v1[2] * v2[1],
            v1[2] * v2[0] - v1[0] * v2[2],
            v1[0] * v2[1] - v1[1] * v2[0]
        ];
        return FormatVector(cross, osep);
    }

    public static string VMag(EvaluationContext ctx, string[] args)
    {
        if (!TryGetSeparators(args, 1, out string sep, out _, out var error))
            return error!;
        if (args.Length == 0 || !TryParseVector(args[0], sep, out var v, out error))
            return error ?? MushError.VectorMustNotBeEmpty.Text();

        return FormatNumber(Math.Sqrt(v.Sum(x => x * x)));
    }

    public static string VDim(EvaluationContext ctx, string[] args)
    {
        if (!TryGetSeparators(args, 1, out string sep, out _, out var error))
            return error!;
        if (args.Length == 0) return "0";
        return ListFunctions.SplitList(args[0], sep).Count.ToString(CultureInfo.InvariantCulture);
    }

    public static string VUnit(EvaluationContext ctx, string[] args)
    {
        if (!TryGetSeparators(args, 1, out string sep, out string osep, out var error))
            return error!;
        if (args.Length == 0 || !TryParseVector(args[0], sep, out var v, out error))
            return error ?? MushError.VectorMustNotBeEmpty.Text();

        double mag = Math.Sqrt(v.Sum(x => x * x));
        if (mag == 0)
            return FormatVector(v, osep);
        return FormatVector(v.Select(x => x / mag).ToArray(), osep);
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
        if (val == Math.Floor(val) && !double.IsInfinity(val) && !double.IsNaN(val) && val >= -9e18 && val <= 9e18)
            return ((long)val).ToString(CultureInfo.InvariantCulture);
        return val.ToString("G", CultureInfo.InvariantCulture);
    }
}
