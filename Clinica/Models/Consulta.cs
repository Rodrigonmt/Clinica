using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Clinica.Models
{
    public class Consulta
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [Required]
        [JsonPropertyName("data")]
        public DateTime Data { get; set; }

        [Required]
        [JsonPropertyName("hora")]
        public string Hora { get; set; }

        [JsonPropertyName("usuario")]
        public string Usuario { get; set; }

        [Required]
        [JsonPropertyName("medico")]
        public string Medico { get; set; }

        [JsonPropertyName("criadoEm")]
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        // 🔹 Novo campo de status
        [JsonPropertyName("status")]
        public StatusConsulta Status { get; set; } = StatusConsulta.Agendada;

        //Observações
        [JsonPropertyName("observacoes")]
        public string Observacoes { get; set; }

        public override string ToString() =>
            $"{Data:yyyy-MM-dd} {Hora} - {Medico} ({Status})";
    }

    public enum StatusConsulta
    {
        Agendada,
        Confirmada,
        EmAndamento,
        Concluida,
        CanceladaPaciente,
        CanceladaClinica,
        NaoCompareceu,
        Reagendada
    }
}
