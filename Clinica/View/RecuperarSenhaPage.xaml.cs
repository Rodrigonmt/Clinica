using Clinica.Services;

namespace Clinica.View
{
    public partial class RecuperarSenhaPage : ContentPage
    {
        private readonly FirebaseAuthService _authService = new();

        public RecuperarSenhaPage()
        {
            InitializeComponent();
        }

        private async void OnEnviarClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmailEntry.Text))
            {
                await DisplayAlert("Erro", "Informe o e-mail.", "OK");
                return;
            }

            bool enviado = await _authService.EnviarEmailRecuperacao(EmailEntry.Text);

            if (enviado)
            {
                await DisplayAlert("Sucesso", "E-mail enviado! Verifique sua caixa de entrada.", "OK");

                // ?? Redirecionar para Login
                await Shell.Current.GoToAsync("//LoginPage");
            }
            else
            {
                await DisplayAlert("Erro", "Falha ao enviar o e-mail.", "OK");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (EmpresaContext.Empresa != null)
                Title = EmpresaContext.Empresa.NomeEmpresa;
        }

    }
}