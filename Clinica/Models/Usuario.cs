using System;
using System.Text.Json.Serialization;

namespace Clinica.Models
{
    public class Usuario
    {
        [JsonPropertyName("nome")]
        public string Nome { get; set; }

        [JsonPropertyName("usuario")]
        public string UsuarioLogin { get; set; }

        [JsonPropertyName("senha")]
        public string Senha { get; set; }

        [JsonPropertyName("cpf")]
        public string CPF { get; set; }

        [JsonPropertyName("telefone")]
        public string Telefone { get; set; }

        [JsonPropertyName("idade")]
        public int Idade { get; set; }

        [JsonPropertyName("cidade")]
        public string Cidade { get; set; }

        [JsonPropertyName("criadoEm")]
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
