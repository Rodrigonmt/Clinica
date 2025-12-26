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
        string email = EmailEntry.Text?.Trim();
        string senha = SenhaEntry.Text;
        string repetir = RepetirEntry.Text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
        {
            await DisplayAlert("Erro", "Preencha todos os campos.", "OK");
            return;
        }

        if (senha != repetir)
        {
            await DisplayAlert("Erro", "As senhas não coincidem.", "OK");
            return;
        }

        // Exibir um loading opcional aqui
        var resultado = await _authService.CriarUsuario(email, senha);

        if (!resultado.sucesso)
        {
            await DisplayAlert("Erro ao criar conta", TraduzErroFirebase(resultado.mensagem), "OK");
            return;
        }

        // Se chegou aqui, o usuário foi criado E a function de e-mail foi chamada
        await DisplayAlert(
            "Verifique seu E-mail",
            $"Enviamos um link de confirmação para {email}. Por favor, valide sua conta antes de fazer o login.",
            "Entendi"
        );

        // Redireciona para o Login
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
        SenhaEntry.IsPassword = !SenhaEntry.IsPassword;

        btnMostrarSenha.Source = SenhaEntry.IsPassword
            ? "visibility.png"
            : "visibility_off.png";
    }


    private void OnMostrarRepetirTapped(object sender, TappedEventArgs e)
    {
        RepetirEntry.IsPassword = !RepetirEntry.IsPassword;

        btnMostrarRepetir.Source = RepetirEntry.IsPassword
            ? "visibility.png"
            : "visibility_off.png";
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (EmpresaContext.Empresa != null)
            Title = EmpresaContext.Empresa.NomeEmpresa;
    }


}
