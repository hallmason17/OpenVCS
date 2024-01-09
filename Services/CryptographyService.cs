using System.Security.Cryptography;

public class CryptographyService : ICryptographyService
{
    public CryptographyService() { }

    public string GetSha1(string text)
    {
        if (String.IsNullOrEmpty(text))
        {
            return String.Empty;
        }

        using (var sha = SHA1.Create())
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(text);
            byte[] hash = sha.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", String.Empty);
        }
    }
}
