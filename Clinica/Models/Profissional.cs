namespace Clinica.Models
{
    public class Profissional
    {
        public string ProfissionalId { get; set; }
        public string Nome { get; set; }
        public string FotoPerfil { get; set; }   // Base64
        public string Cor { get; set; }
        public string Especialidade { get; set; }
        public string Email { get; set; }
        public string Telefone { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}
