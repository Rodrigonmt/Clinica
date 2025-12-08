using System.Text;
using System.Text.Json;

namespace Clinica.Services
{
    public class FirebaseAuthService
    {
        private const string ApiKey = "AIzaSyBkknZVoXHfj7LjH9XT3wbHSGCj9Qvn1LE";

        public async Task<AuthResponse?> Login(string email, string senha)
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={ApiKey}";

            var dados = new
            {
                email = email,
                password = senha,
                returnSecureToken = true
            };

            var json = JsonSerializer.Serialize(dados);
            var conteudo = new StringContent(json, Encoding.UTF8, "application/json");

            var http = new HttpClient();
            var response = await http.PostAsync(url, conteudo);

            if (!response.IsSuccessStatusCode)
                return null;

            var respostaString = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AuthResponse>(respostaString);
        }

        public async Task<string?> CriarUsuario(string email, string senha)
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={ApiKey}";

            var dados = new
            {
                email = email,
                password = senha,
                returnSecureToken = true
            };

            var json = JsonSerializer.Serialize(dados);
            var conteudo = new StringContent(json, Encoding.UTF8, "application/json");

            var http = new HttpClient();
            var response = await http.PostAsync(url, conteudo);

            var resposta = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return resposta;

            return "OK";
        }

        public async Task<bool> EnviarEmailRecuperacao(string email)
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={ApiKey}";

            var dados = new
            {
                requestType = "PASSWORD_RESET",
                email = email
            };

            var json = JsonSerializer.Serialize(dados);
            var conteudo = new StringContent(json, Encoding.UTF8, "application/json");

            var http = new HttpClient();
            var response = await http.PostAsync(url, conteudo);

            return response.IsSuccessStatusCode;
        }

        public async Task<AuthResponse?> RefreshLogin(string refreshToken)
        {
            string url = $"https://securetoken.googleapis.com/v1/token?key={ApiKey}";

            var dados = new
            {
                grant_type = "refresh_token",
                refresh_token = refreshToken
            };

            var json = JsonSerializer.Serialize(dados);
            var conteudo = new StringContent(json, Encoding.UTF8, "application/json");

            var http = new HttpClient();
            var response = await http.PostAsync(url, conteudo);

            if (!response.IsSuccessStatusCode)
                return null;

            var resposta = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AuthResponse>(resposta);
        }
    }

    public class AuthResponse
    {
        public string idToken { get; set; }
        public string email { get; set; }
        public string localId { get; set; }
        public string refreshToken { get; set; }
    }
}
