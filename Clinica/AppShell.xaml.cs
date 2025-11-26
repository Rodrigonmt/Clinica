using Clinica.View;

namespace Clinica
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(View.LoginPage), typeof(View.LoginPage));
            Routing.RegisterRoute(nameof(View.CadastroPage), typeof(View.CadastroPage));
            Routing.RegisterRoute(nameof(View.AgendarConsultaPage), typeof(View.AgendarConsultaPage));
            Routing.RegisterRoute(nameof(View.ConsultasAgendaPage), typeof(View.ConsultasAgendaPage));
            Routing.RegisterRoute(nameof(View.InfoConsultasAgendadaPage), typeof(View.InfoConsultasAgendadaPage));
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        }
    }

}