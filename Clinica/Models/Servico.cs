namespace Clinica.Models
{
    public class Servico
    {
        public string ServicoId { get; set; }
        public string Nome { get; set; }
        public decimal Preco { get; set; }
        public int Duracao { get; set; }
        public DateTime CriadoEm { get; set; }

        // Controle de seleção na tela
        public bool Selecionado { get; set; }
    }
}
