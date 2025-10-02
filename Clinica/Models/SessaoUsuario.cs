using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinica.Models
{
    public static class SessaoUsuario
    {
        // Guarda o usuário logado durante a execução do app
        public static Usuario UsuarioLogado { get; set; }
    }
}
