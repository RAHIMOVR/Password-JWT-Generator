using System.Security.Cryptography;

namespace PassGen;

static class Program
{
    const string Chars =
        "abcdefghijklmnopqrstuvwxyz" +
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
        "0123456789" +
        "!@#$%^&*()-_=+[]{};:,.<>?/~";

    static void Main()
    {
        Console.WriteLine(GeneratePassword(16));
    }

    static string GeneratePassword(int length)
    {
        var buffer = new char[length];

        // RandomNumberGenerator is cryptographically secure, unlike System.Random.
        for (int i = 0; i < length; i++)
            buffer[i] = Chars[RandomNumberGenerator.GetInt32(Chars.Length)];

        return new string(buffer);
    }
}