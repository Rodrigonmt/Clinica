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
        public string Data { get; set; }


        // 🔹 Hora inicial (mantida para compatibilidade)
        [Required]
        [JsonPropertyName("hora")]
        public string Hora { get; set; }

        // 🔥 NOVOS CAMPOS (NÃO quebram nada existente)

        // Hora real de início do atendimento
        [JsonPropertyName("horaInicio")]
        public string HoraInicio { get; set; }

        // Hora real de término do atendimento
        [JsonPropertyName("horaFim")]
        public string HoraFim { get; set; }

        // Duração total em minutos
        [JsonPropertyName("duracao")]
        public int Duracao { get; set; }

        //formatação da data
        [JsonIgnore]
        public string DataFormatada
        {
            get
            {
                if (DateTime.TryParse(Data, out DateTime dt))
                {
                    return dt.ToString("dd/MM/yyyy");
                }
                return Data; // Retorna o original caso a conversão falhe
            }
        }

        // 🔹 Demais campos inalterados
        [JsonPropertyName("usuario")]
        public string Usuario { get; set; }

        [Required]
        [JsonPropertyName("medico")]
        public string Medico { get; set; }

        // 🔥 ID REAL DO PROFISSIONAL (novo campo)
        [JsonPropertyName("medicoId")]
        public string MedicoId { get; set; }

        [JsonPropertyName("criadoEm")]
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("status")]
        public StatusConsulta Status { get; set; } = StatusConsulta.Agendada;

        [JsonPropertyName("observacoes")]
        public string Observacoes { get; set; }

        [JsonPropertyName("servico")]
        public string Servico { get; set; }

        [JsonPropertyName("servicos")]
        public List<string> Servicos { get; set; } = new();


        [JsonPropertyName("valorTotal")]
        public decimal ValorTotal { get; set; }

        public override string ToString() =>
            $"{Data:yyyy-MM-dd} {HoraInicio} - {Medico} ({Status})";


        [JsonPropertyName("formaPagamento")]
        public string FormaPagamento { get; set; }


        [JsonIgnore]
        public string DuracaoFormatada
        {
            get
            {
                int horas = Duracao / 60;
                int minutos = Duracao % 60;

                if (horas > 0 && minutos > 0)
                    return $"{horas} hora{(horas > 1 ? "s" : "")} e {minutos} minutos";

                if (horas > 0)
                    return $"{horas} hora{(horas > 1 ? "s" : "")}";

                return $"{minutos} minutos";
            }
        }


    }

    public enum StatusConsulta
    {
        Agendada,
        Confirmada,
        EmAndamento,
        Concluida,
        CanceladaCliente,
        CanceladaEmpresa,
        NaoCompareceu,
        Reagendada
    }
}