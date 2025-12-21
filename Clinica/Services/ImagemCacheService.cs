using System.Security.Cryptography;
using System.Text;

namespace Clinica.Services;

public static class ImagemCacheService
{
    private static string ImgPath =>
        Path.Combine(FileSystem.AppDataDirectory, "empresa_bg.jpg");

    private static string HashPath =>
        Path.Combine(FileSystem.AppDataDirectory, "empresa_bg.hash");

    public static bool ExisteImagemCache()
        => File.Exists(ImgPath);

    public static ImageSource ObterImagemCache()
        => ImageSource.FromFile(ImgPath);

    public static async Task SalvarImagemAsync(byte[] bytes)
    {
        await File.WriteAllBytesAsync(ImgPath, bytes);

        var hash = GerarHash(bytes);
        await File.WriteAllTextAsync(HashPath, hash);
    }

    public static bool ImagemMudou(byte[] novaImagem)
    {
        if (!File.Exists(HashPath))
            return true;

        var hashAtual = File.ReadAllText(HashPath);
        var novoHash = GerarHash(novaImagem);

        return hashAtual != novoHash;
    }

    private static string GerarHash(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
