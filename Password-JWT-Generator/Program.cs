using System.Globalization;
using System.Security.Cryptography;

namespace PassGen;

[Flags]
enum CharSets { None = 0, Lower = 1, Upper = 2, Digits = 4, Symbols = 8, All = Lower | Upper | Digits | Symbols }

sealed class Settings
{
    public int? Length;
    public int? Bits;
    public int Count = 1;
    public CharSets Sets = CharSets.All;
    public string? Custom;
    public bool NoAmbiguous;
    public bool RequireEach = true;
    public bool Quiet;
}

static class Program
{
    const string Lower = "abcdefghijklmnopqrstuvwxyz";
    const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const string Digits = "0123456789";
    // No quotes, backtick or backslash, so passwords are safe to paste into shells and config files.
    const string Symbols = "!@#$%^&*()-_=+[]{};:,.<>?/~";
    const string Ambiguous = "0O1lI|";

    const int DefaultLength = 16, MaxLength = 1024;
    const int MinBits = 8, MaxBits = 256;
    const int MaxCount = 100;

    static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    static int Run(string[] args)
    {
        var a = new List<string>(args);
        var s = new Settings();

        for (int i = 0; i < a.Count; i++)
        {
            string arg = a[i];
            string Next() => i + 1 < a.Count ? a[++i] : throw new ArgumentException($"Option '{arg}' needs a value.");

            switch (arg)
            {
                case "-l": case "--length": s.Length = ParseInt(Next(), arg); break;
                case "-b": case "--bits": s.Bits = ParseInt(Next(), arg); break;
                case "-n": case "--count": s.Count = ParseInt(Next(), arg); break;
                case "-c": case "--charset": s.Sets = ParseCharset(Next()); break;
                case "--custom": s.Custom = Next(); break;
                case "-x": case "--no-ambiguous": s.NoAmbiguous = true; break;
                case "--no-require": s.RequireEach = false; break;
                case "-q": case "--quiet": s.Quiet = true; break;
                default: throw new ArgumentException($"Unknown option '{arg}'.");
            }
        }

        return RunPassword(s);
    }

    static int RunPassword(Settings s)
    {
        CheckCount(s.Count);
        if (s.Length is not null && s.Bits is not null)
            throw new ArgumentException("Use either --length or --bits, not both.");

        var groups = GetGroups(s);
        string pool = new string(groups.SelectMany(g => g).Distinct().ToArray());
        int length = s.Bits is int bits ? LengthForBits(bits, pool.Length) : s.Length ?? DefaultLength;

        if (length < 1 || length > MaxLength)
            throw new ArgumentException($"Length must be between 1 and {MaxLength}.");

        bool require = s.RequireEach && groups.Count > 1;
        if (require && groups.Count > length)
            throw new ArgumentException($"Length {length} is too short to include a character from each of the {groups.Count} sets.");

        for (int i = 0; i < s.Count; i++)
            Console.WriteLine(GeneratePassword(pool, groups, length, require));

        if (!s.Quiet)
        {
            double entropy = length * Math.Log2(pool.Length);
            Info($"length {length} | pool {pool.Length} chars | ~{entropy:F1} bits of entropy ({Rate(entropy)})");
        }
        return 0;
    }

    static List<string> GetGroups(Settings s)
    {
        var groups = new List<string>();

        if (!string.IsNullOrEmpty(s.Custom))
        {
            groups.Add(new string(s.Custom.Distinct().ToArray()));
        }
        else
        {
            if (s.Sets.HasFlag(CharSets.Lower)) groups.Add(Lower);
            if (s.Sets.HasFlag(CharSets.Upper)) groups.Add(Upper);
            if (s.Sets.HasFlag(CharSets.Digits)) groups.Add(Digits);
            if (s.Sets.HasFlag(CharSets.Symbols)) groups.Add(Symbols);
        }

        if (s.NoAmbiguous)
            groups = groups.Select(g => new string(g.Where(c => !Ambiguous.Contains(c)).ToArray())).ToList();

        groups.RemoveAll(g => g.Length == 0);

        if (groups.Count == 0)
            throw new ArgumentException("No characters available. Pick at least one character set.");

        return groups;
    }

    static string GeneratePassword(string pool, List<string> groups, int length, bool require)
    {
        var buffer = new char[length];

        // Rejection sampling: regenerate until every set is present.
        // This keeps the result uniformly random among all valid passwords.
        while (true)
        {
            for (int i = 0; i < length; i++)
                buffer[i] = pool[RandomNumberGenerator.GetInt32(pool.Length)];

            if (!require || groups.All(g => buffer.Any(c => g.Contains(c))))
                return new string(buffer);
        }
    }

    // Entropy = length × log2(pool size), so length = ceil(bits / log2(pool size)).
    static int LengthForBits(int bits, int poolSize)
    {
        CheckBits(bits);
        if (poolSize < 2)
            throw new ArgumentException("At least 2 distinct characters are needed to reach a target entropy.");

        // The epsilon stops floating-point noise from rounding exact results up (e.g. 256 / 4.0 = 64).
        return (int)Math.Ceiling(bits / Math.Log2(poolSize) - 1e-9);
    }

    static string Rate(double bits) => bits switch
    {
        < 40 => "weak",
        < 60 => "fair",
        < 80 => "strong",
        < 128 => "very strong",
        _ => "excellent",
    };

    static void CheckCount(int count)
    {
        if (count < 1 || count > MaxCount)
            throw new ArgumentException($"Count must be between 1 and {MaxCount}.");
    }

    static void CheckBits(int bits)
    {
        if (bits < MinBits || bits > MaxBits)
            throw new ArgumentException($"Bits must be between {MinBits} and {MaxBits}.");
    }

    static int ParseInt(string value, string option) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
            ? result
            : throw new ArgumentException($"'{value}' is not a valid number for {option}.");

    static CharSets ParseCharset(string value)
    {
        if (value.Equals("all", StringComparison.OrdinalIgnoreCase)) return CharSets.All;

        var sets = CharSets.None;
        foreach (char c in value.ToLowerInvariant())
        {
            sets |= c switch
            {
                'l' => CharSets.Lower,
                'u' => CharSets.Upper,
                'd' => CharSets.Digits,
                's' => CharSets.Symbols,
                _ => throw new ArgumentException($"Unknown charset letter '{c}'. Use any of: l u d s."),
            };
        }
        return sets;
    }

    // Info goes to stderr so `passgen | clip` or `$(passgen)` captures only the password.
    static void Info(string message) => Console.Error.WriteLine(message);
}