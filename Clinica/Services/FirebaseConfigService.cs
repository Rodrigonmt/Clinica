using System.Net.Http.Json;
using Clinica.Models;

namespace Clinica.Services
{
    public class FirebaseConfigService
    {
        private const string FirebaseUrl =
            "https://clinica-e248d-default-rtdb.firebaseio.com/configuracoes/empresa.json";

        private readonly HttpClient _http = new();

        public async Task<EmpresaConfig?> ObterEmpresaAsync()
        {
            return await _http.GetFromJsonAsync<EmpresaConfig>(FirebaseUrl);
        }
    }
}