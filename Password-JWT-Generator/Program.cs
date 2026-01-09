using System.Globalization;
using System.Security.Cryptography;

namespace PassGen;

static class Program
{
    const string Chars =
        "abcdefghijklmnopqrstuvwxyz" +
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
        "0123456789" +
        "!@#$%^&*()-_=+[]{};:,.<>?/~";

    const int DefaultLength = 16, MaxLength = 1024;
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
        int length = DefaultLength;
        int count = 1;

        for (int i = 0; i < a.Count; i++)
        {
            string arg = a[i];
            string Next() => i + 1 < a.Count ? a[++i] : throw new ArgumentException($"Option '{arg}' needs a value.");

            switch (arg)
            {
                case "-l": case "--length": length = ParseInt(Next(), arg); break;
                case "-n": case "--count": count = ParseInt(Next(), arg); break;
                default: throw new ArgumentException($"Unknown option '{arg}'.");
            }
        }

        if (length < 1 || length > MaxLength)
            throw new ArgumentException($"Length must be between 1 and {MaxLength}.");
        CheckCount(count);

        for (int i = 0; i < count; i++)
            Console.WriteLine(GeneratePassword(length));

        return 0;
    }

    static string GeneratePassword(int length)
    {
        var buffer = new char[length];

        // RandomNumberGenerator is cryptographically secure, unlike System.Random.
        for (int i = 0; i < length; i++)
            buffer[i] = Chars[RandomNumberGenerator.GetInt32(Chars.Length)];

        return new string(buffer);
    }

    static void CheckCount(int count)
    {
        if (count < 1 || count > MaxCount)
            throw new ArgumentException($"Count must be between 1 and {MaxCount}.");
    }

    static int ParseInt(string value, string option) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
            ? result
            : throw new ArgumentException($"'{value}' is not a valid number for {option}.");
}