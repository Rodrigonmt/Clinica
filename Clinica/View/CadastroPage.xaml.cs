using Clinica.Services;

namespace Clinica.View;

public partial class CadastroPage : ContentPage
{
    private readonly FirebaseAuthService _authService = new();

    public CadastroPage()
    {
        InitializeComponent();
    }

    private async void OnCriarClicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text;
        string senha = SenhaEntry.Text;
        string repetir = RepetirEntry.Text;

        if (senha != repetir)
        {
            await DisplayAlert("Erro", "As senhas não coincidem.", "OK");
            return;
        }

        var result = await _authService.CriarUsuario(email, senha);

        if (result != "OK")
        {
            await DisplayAlert("Erro", $"Falha ao criar conta: {result}", "OK");
            return;
        }

        await DisplayAlert("Sucesso", "Conta criada com sucesso!", "OK");
        await Shell.Current.GoToAsync("//LoginPage");
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.GoToAsync("//LoginPage");
        });

        return true;
    }

    private void OnMostrarSenhaTapped(object sender, TappedEventArgs e)
    {
        // Alterna visibilidade da senha
        SenhaEntry.IsPassword = !SenhaEntry.IsPassword;

        // Atualiza ícone
        btnMostrarSenha.Text = SenhaEntry.IsPassword ? "visibility" : "visibility_off";
    }

    private void OnMostrarRepetirTapped(object sender, TappedEventArgs e)
    {
        RepetirEntry.IsPassword = !RepetirEntry.IsPassword;
        btnMostrarRepetir.Text = RepetirEntry.IsPassword ? "visibility" : "visibility_off";
    }

}
