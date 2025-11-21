namespace tracker.Domain.User;

public static class AuthenticationHelpers
{
    public static string HashPassword(string password)
    {
        var salt = new byte[16];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        var argon2 = new Konscious.Security.Cryptography.Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 8,
            Iterations = 4,
            MemorySize = 128 * 1024
        };

        var hash = argon2.GetBytes(32);

        var hashBytes = new byte[salt.Length + hash.Length];
        Buffer.BlockCopy(salt, 0, hashBytes, 0, salt.Length);
        Buffer.BlockCopy(hash, 0, hashBytes, salt.Length, hash.Length);

        return Convert.ToBase64String(hashBytes);
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        var hashBytes = Convert.FromBase64String(storedHash);

        var salt = new byte[16];
        Buffer.BlockCopy(hashBytes, 0, salt, 0, salt.Length);

        var argon2 = new Konscious.Security.Cryptography.Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 8,
            Iterations = 4,
            MemorySize = 128 * 1024
        };

        var hash = argon2.GetBytes(32);

        for (int i = 0; i < hash.Length; i++)
        {
            if (hashBytes[i + salt.Length] != hash[i])
                return false;
        }

        return true;
    }
}