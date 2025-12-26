using Clinica.Models;
using Clinica.Services;
namespace Clinica.View;

public partial class LoginPage : ContentPage
{
    private readonly FirebaseAuthService _authService = new();

    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        await btnEntrar.ScaleTo(0.92, 80);
        await btnEntrar.ScaleTo(1, 120);

        string email = EmailEntry.Text;
        string senha = SenhaEntry.Text;

        var auth = await _authService.Login(email, senha);


        if (auth == null)
        {
            await DisplayAlert("Erro", "Email ou senha inválidos.", "OK");
            return;
        }

        // Salvar idToken temporariamente
        await SecureStorage.SetAsync("auth_token", auth.idToken);

        // verifica se email foi confirmado
        bool verificado = await _authService.EmailVerificado(auth.idToken);

        if (!verificado)
        {
            await DisplayAlert("Atenção",
                "Você ainda não confirmou seu email. Verifique sua caixa de entrada e a pasta de Spam.",
                "OK");
            return;
        }

        // 🔐 SALVAR LOGIN SEGURO
        await SecureStorage.SetAsync("email", auth.email);
        await SecureStorage.SetAsync("user_id", auth.localId);
        await SecureStorage.SetAsync("refresh_token", auth.refreshToken);

        // token expira rápido, não é obrigatório guardar
        await SecureStorage.SetAsync("id_token", auth.idToken);

        SessaoUsuario.UsuarioLogado = new Usuario
        {
            UserId = auth.localId,
            Email = auth.email
        };

        if (chkLembrar.IsChecked)
        {
            await SecureStorage.SetAsync("lembrar", "true");
            await SecureStorage.SetAsync("refresh_token", auth.refreshToken);
            await SecureStorage.SetAsync("user_id", auth.localId);
        }
        else
        {
            SecureStorage.Remove("lembrar");
            SecureStorage.Remove("refresh_token");
        }

        await Shell.Current.GoToAsync(nameof(MainPage));

    }


    private async void OnCadastrarClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CadastroPage));
    }

    private async void OnRecuperarClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RecuperarSenhaPage));
    }

    private async void OnPageAppearing(object sender, EventArgs e)
    {
        await lblTitulo.FadeTo(1, 500);
        await lblSubTitulo.FadeTo(1, 500);
        await frmContainer.FadeTo(1, 500);
        await btnEsqueci.FadeTo(1, 500);
    }

    private void OnMostrarSenhaTapped(object sender, EventArgs e)
    {
        SenhaEntry.IsPassword = !SenhaEntry.IsPassword;

        imgMostrarSenha.Source = SenhaEntry.IsPassword
            ? "visibility.png"
            : "visibility_off.png";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // 🔥 Sempre começa marcado
        chkLembrar.IsChecked = true;

        if (EmpresaContext.Empresa != null)
            Title = EmpresaContext.Empresa.NomeEmpresa;
        else
            Title = " "; // evita título padrão feio
    }


}
