using System.Security.Cryptography;

namespace ArkKeeper.Core.Utils;

public static class PasswordGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    public static string Generate(int length) =>
        string.Create(length, 0, (span, _) =>
        {
            var bytes = RandomNumberGenerator.GetBytes(length);
            for (var i = 0; i < length; i++)
            {
                span[i] = Alphabet[bytes[i] % Alphabet.Length];
            }
        });
}
