using System.Text.Json;

public class FirebaseStorageService
{
    private const string Bucket = "clinica-e248d.appspot.com";

    public async Task<string?> UploadFotoAsync(Stream fotoStream, string localId, string idToken)
    {
        string url = $"https://firebasestorage.googleapis.com/v0/b/{Bucket}/o/fotosPerfil%2F{localId}.jpg?uploadType=media&name=fotosPerfil/{localId}.jpg";

        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {idToken}");

        var content = new StreamContent(fotoStream);

        var response = await http.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
            return null;

        string json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        string downloadUrl = "https://firebasestorage.googleapis.com/v0/b/" +
                             Bucket +
                             "/o/fotosPerfil%2F" + localId + ".jpg?alt=media";

        return downloadUrl;
    }
}