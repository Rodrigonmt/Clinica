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

        var auth = await _authService.CriarUsuario(email, senha);


        if (!auth.sucesso)
        {
            await DisplayAlert("Erro ao criar conta", TraduzErroFirebase(auth.mensagem), "OK");
            return;
        }


        await DisplayAlert("Sucesso", "Conta criada com sucesso!", "OK");
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private string TraduzErroFirebase(string codigo)
    {
        return codigo switch
        {
            "EMAIL_EXISTS" => "Este e-mail já está cadastrado.",
            "INVALID_EMAIL" => "O e-mail informado é inválido.",
            "OPERATION_NOT_ALLOWED" => "Cadastro por email/senha está desativado no Firebase.",
            "WEAK_PASSWORD" => "A senha deve ter no mínimo 6 caracteres.",
            "TOO_MANY_ATTEMPTS_TRY_LATER" => "Muitas tentativas. Tente novamente mais tarde.",
            _ => "Não foi possível criar sua conta. Tente novamente."
        };
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

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (EmpresaContext.Empresa != null)
            Title = EmpresaContext.Empresa.NomeEmpresa;
    }


}
