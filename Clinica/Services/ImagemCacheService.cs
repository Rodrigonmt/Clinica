using System.Security.Cryptography;
using System.Text;

namespace Clinica.Services;

public static class ImagemCacheService
{
    private static string BasePath =>
        FileSystem.AppDataDirectory;

    private static string HashPath =>
        Path.Combine(BasePath, "empresa_bg.hash");

    private const string Prefixo = "empresa_bg_";
    private const string Extensao = ".jpg";

    public static string ObterCaminhoImagem(string hash)
        => Path.Combine(BasePath, $"{Prefixo}{hash}{Extensao}");

    public static string ObterHashAtual()
        => File.Exists(HashPath) ? File.ReadAllText(HashPath) : null;

    public static ImageSource ObterImagemCache()
    {
        var hash = ObterHashAtual();
        if (hash == null)
            return null;

        var path = ObterCaminhoImagem(hash);
        return File.Exists(path) ? ImageSource.FromFile(path) : null;
    }

    public static bool ImagemMudou(byte[] novaImagem)
    {
        var novoHash = GerarHash(novaImagem);
        var hashAtual = ObterHashAtual();

        return hashAtual != novoHash;
    }

    public static async Task SalvarImagemAsync(byte[] bytes)
    {
        var hash = GerarHash(bytes);
        var imgPath = ObterCaminhoImagem(hash);

        // Salva nova imagem
        await File.WriteAllBytesAsync(imgPath, bytes);

        // Atualiza hash atual
        await File.WriteAllTextAsync(HashPath, hash);

        // 🔥 Limpeza automática
        LimparImagensAntigas(hash);
    }

    // =========================
    // 🔥 LIMPEZA AUTOMÁTICA
    // =========================
    private static void LimparImagensAntigas(string hashAtual)
    {
        try
        {
            var arquivos = Directory.GetFiles(BasePath, $"{Prefixo}*{Extensao}");

            foreach (var arquivo in arquivos)
            {
                if (!arquivo.EndsWith($"{Prefixo}{hashAtual}{Extensao}"))
                {
                    try
                    {
                        File.Delete(arquivo);
                    }
                    catch
                    {
                        // silencioso — nunca quebrar o app
                    }
                }
            }
        }
        catch
        {
            // silencioso
        }
    }

    private static string GerarHash(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(bytes))
            .Replace("/", "_")
            .Replace("+", "-");
    }
}
