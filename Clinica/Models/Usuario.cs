using System;
using System.Text.Json.Serialization;

namespace Clinica.Models
{
    public class Usuario
    {
        public string UserId { get; set; }   // localId
        public string Email { get; set; }    // email do Firebase
        public string UsuarioLogin => Email; // usado para associação das consultas
        public string Nome { get; set; }
        public string Telefone { get; set; }
        public string DataNascimento { get; set; } // opcional
        public string Endereco { get; set; }       // opcional
        public string FotoPerfil { get; set; }     // URL foto
    }
}
