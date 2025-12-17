namespace Clinica.Models
{
    public class ProfissionalServico
    {
        public string ProfissionalId { get; set; }

        // Lista de IDs dos serviços que o profissional atende
        public List<string> ServicosIds { get; set; } = new();

        public DateTime AtualizadoEm { get; set; }
    }
}
