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

            using var http = new HttpClient();
            var response = await http.PostAsync(url, conteudo);

            if (!response.IsSuccessStatusCode)
                return null;

            var respostaString = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AuthResponse>(respostaString);
        }

        public async Task<(bool sucesso, string mensagem, AuthResponse? dados)> CriarUsuario(string email, string senha)
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

            string resposta = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    var erro = JsonDocument.Parse(resposta);
                    string mensagem = erro.RootElement
                        .GetProperty("error")
                        .GetProperty("message")
                        .GetString();

                    return (false, mensagem, null);
                }
                catch
                {
                    return (false, "ERRO_DESCONHECIDO", null);
                }
            }

            var user = JsonSerializer.Deserialize<AuthResponse>(resposta);

            await EnviarEmailVerificacao(user.idToken);

            return (true, "OK", user);
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

        public async Task<bool> EnviarEmailVerificacao(string idToken)
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={ApiKey}";

            var dados = new
            {
                requestType = "VERIFY_EMAIL",
                idToken = idToken
            };

            var json = JsonSerializer.Serialize(dados);
            var conteudo = new StringContent(json, Encoding.UTF8, "application/json");

            var http = new HttpClient();
            var response = await http.PostAsync(url, conteudo);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ConfirmarEmail(string codigo)
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={ApiKey}";

            var dados = new
            {
                oobCode = codigo
            };

            var json = JsonSerializer.Serialize(dados);
            var conteudo = new StringContent(json, Encoding.UTF8, "application/json");

            var http = new HttpClient();
            var response = await http.PostAsync(url, conteudo);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> EmailVerificado(string idToken)
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={ApiKey}";

            var dados = new { idToken = idToken };

            var json = JsonSerializer.Serialize(dados);
            var conteudo = new StringContent(json, Encoding.UTF8, "application/json");

            var http = new HttpClient();
            var response = await http.PostAsync(url, conteudo);

            if (!response.IsSuccessStatusCode)
                return false;

            var jsonResult = await response.Content.ReadAsStringAsync();
            var obj = JsonDocument.Parse(jsonResult);

            return obj.RootElement
                      .GetProperty("users")[0]
                      .GetProperty("emailVerified")
                      .GetBoolean();
        }


        public class RefreshResponse
        {
            public string id_token { get; set; }        // novo idToken (nome com underscore vindo do endpoint)
            public string refresh_token { get; set; }   // novo refreshToken
            public string expires_in { get; set; }
            public string user_id { get; set; }
        }

        public async Task<RefreshResponse?> RefreshTokenAsync(string refreshToken)
        {
            // Endpoint para trocar refresh token
            string url = $"https://securetoken.googleapis.com/v1/token?key={ApiKey}";

            var dados = new Dictionary<string, string>
    {
        { "grant_type", "refresh_token" },
        { "refresh_token", refreshToken }
    };

            using var http = new HttpClient();
            var content = new FormUrlEncodedContent(dados);
            var response = await http.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
                return null;

            var respostaString = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RefreshResponse>(respostaString);
        }

        


    }

    public class AuthResponse
    {
        public string idToken { get; set; }
        public string email { get; set; }
        public string localId { get; set; }
        public string refreshToken { get; set; }
        public string expiresIn { get; set; }
    }

}
