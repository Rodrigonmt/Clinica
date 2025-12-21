using Clinica.Models;

namespace Clinica.Services
{
    public static class EmpresaContext
    {
        public static EmpresaConfig? Empresa { get; private set; }

        public static event Action? EmpresaAtualizada;

        public static void SetEmpresa(EmpresaConfig empresa)
        {
            Empresa = empresa;
            EmpresaAtualizada?.Invoke();
        }
    }

}
