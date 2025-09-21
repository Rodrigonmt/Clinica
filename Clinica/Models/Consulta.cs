using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Clinica.Models
{
    public class Consulta
    {
        // Opcional: será preenchido com a chave gerada pelo Firebase (ex: "-Mxyz123")
        [JsonPropertyName("id")]
        public string Id { get; set; }

        // Data da consulta (use DatePicker.Date no UI)
        [Required]
        [JsonPropertyName("data")]
        public DateTime Data { get; set; }

        // Hora em formato "HH:mm" (ex: "14:30"). Mantemos string por simplicidade.
        [Required]
        [JsonPropertyName("hora")]
        public string Hora { get; set; }

        // Nome do médico
        [Required]
        [JsonPropertyName("medico")]
        public string Medico { get; set; }

        // Timestamp de criação (padrão UTC)
        [JsonPropertyName("criadoEm")]
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        // Conveniência: formato legível
        public override string ToString() =>
            $"{Data:yyyy-MM-dd} {Hora} - {Medico}";
    }
}
