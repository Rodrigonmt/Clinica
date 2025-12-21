using Clinica.Services;

namespace Clinica
{
    public static class EmpresaBootstrapper
    {
        public static async Task CarregarEmpresaAsync()
        {
            if (EmpresaContext.Empresa != null)
                return;

            var service = new FirebaseConfigService();
            var empresa = await service.ObterEmpresaAsync();

            if (empresa != null)
                EmpresaContext.SetEmpresa(empresa);
        }
    }
}
