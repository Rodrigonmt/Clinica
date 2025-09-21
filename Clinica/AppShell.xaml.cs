namespace Clinica
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(View.AgendarConsultaPage), typeof(View.AgendarConsultaPage));
        }
    }
}
