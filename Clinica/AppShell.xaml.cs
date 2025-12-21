using Clinica.Services;
using Clinica.View;

namespace Clinica
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            RegistrarRotas();

            // escuta a atualização da empresa
            EmpresaContext.EmpresaAtualizada += AtualizarTitulo;
        }

        private void RegistrarRotas()
        {
            Routing.RegisterRoute(nameof(View.LoginPage), typeof(View.LoginPage));
            Routing.RegisterRoute(nameof(View.CadastroPage), typeof(View.CadastroPage));
            Routing.RegisterRoute(nameof(View.AgendarConsultaPage), typeof(View.AgendarConsultaPage));
            Routing.RegisterRoute(nameof(View.ConsultasAgendaPage), typeof(View.ConsultasAgendaPage));
            Routing.RegisterRoute(nameof(View.InfoConsultasAgendadaPage), typeof(View.InfoConsultasAgendadaPage));
            Routing.RegisterRoute(nameof(View.HistoricoConsultasAgendadasPage), typeof(View.HistoricoConsultasAgendadasPage));
            Routing.RegisterRoute(nameof(View.InfoHistoricoConsultaAgendadaPage), typeof(View.InfoHistoricoConsultaAgendadaPage));
            Routing.RegisterRoute(nameof(View.PagamentoPixPage), typeof(View.PagamentoPixPage));
            Routing.RegisterRoute(nameof(View.LoadingPage), typeof(View.LoadingPage));
            Routing.RegisterRoute(nameof(View.RecuperarSenhaPage), typeof(View.RecuperarSenhaPage));
            Routing.RegisterRoute(nameof(View.PerfilPage), typeof(View.PerfilPage));
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        }

        private void AtualizarTitulo()
        {
            if (EmpresaContext.Empresa == null)
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var page = Shell.Current?.CurrentPage;

                if (page != null)
                {
                    page.Title = EmpresaContext.Empresa.NomeEmpresa;
                }
            });
        }

    }
}
