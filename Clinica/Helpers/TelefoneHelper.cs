using System.Text.RegularExpressions;

namespace Clinica.Helpers
{
    public static class TelefoneHelper
    {
        /// <summary>
        /// Converte telefone da tela (com máscara) para o padrão Firebase (E.164)
        /// Ex: (54) 99706-9108 → +5554997069108
        /// </summary>
        public static string ParaFormatoFirebase(string telefoneComMascara)
        {
            if (string.IsNullOrWhiteSpace(telefoneComMascara))
                return null;

            // Remove tudo que não for número
            var somenteNumeros = Regex.Replace(telefoneComMascara, @"\D", "");

            // Se não tiver código do país, adiciona Brasil (55)
            if (!somenteNumeros.StartsWith("55"))
                somenteNumeros = "55" + somenteNumeros;

            return $"+{somenteNumeros}";
        }

        /// <summary>
        /// Converte telefone do Firebase para formato com máscara (tela)
        /// Ex: +5554997069108 → (54) 99706-9108
        /// </summary>
        public static string ParaFormatoTela(string telefoneFirebase)
        {
            if (string.IsNullOrWhiteSpace(telefoneFirebase))
                return null;

            // Remove + e qualquer caractere não numérico
            var digits = Regex.Replace(telefoneFirebase, @"\D", "");

            // Remove código do país (55)
            if (digits.StartsWith("55"))
                digits = digits.Substring(2);

            // Telefones inválidos ou incompletos
            if (digits.Length < 10)
                return digits;

            // Celular (11 dígitos)
            if (digits.Length == 11)
                return $"({digits[..2]}) {digits.Substring(2, 5)}-{digits.Substring(7)}";

            // Fixo (10 dígitos)
            return $"({digits[..2]}) {digits.Substring(2, 4)}-{digits.Substring(6)}";
        }
    }
}
