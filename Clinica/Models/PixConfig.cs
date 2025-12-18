namespace Clinica.Models
{
    public class PixConfig
    {
        public bool Ativo { get; set; }

        public string ChaveMascara { get; set; }

        public string Destinatario { get; set; }

        public string InstituicaoFinanceira { get; set; }

        public string Tipo { get; set; } // SIMPLES | AUTOMATIZADO
    }
}

