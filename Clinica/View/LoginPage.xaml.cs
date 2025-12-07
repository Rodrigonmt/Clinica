//using Clinica.Models;
//using System.Text.Json;

//namespace Clinica.View;

//public partial class LoginPage : ContentPage
//{
//    private const string FirebaseUrl = "https://clinica-e248d-default-rtdb.firebaseio.com/usuarios.json";

//    public LoginPage()
//    {
//        InitializeComponent();
//    }

//    private async void BTN_Entrar_Login_Clicked(object sender, EventArgs e)
//    {
//        string usuarioDigitado = TXTUsuario.Text?.Trim();
//        string senhaDigitada = TXTSenha.Text?.Trim();

//        if (string.IsNullOrEmpty(usuarioDigitado) || string.IsNullOrEmpty(senhaDigitada))
//        {
//            await DisplayAlert("Erro", "Preencha usuário e senha!", "OK");
//            return;
//        }

//        try
//        {
//            using (HttpClient client = new HttpClient())
//            {
//                string json = await client.GetStringAsync(FirebaseUrl);

//                if (string.IsNullOrWhiteSpace(json) || json == "null")
//                {
//                    await DisplayAlert("Erro", "Nenhum usuário cadastrado.", "OK");
//                    return;
//                }

//                // 🔹 Firebase retorna um dicionário: chave gerada -> Usuario
//                var usuarios = JsonSerializer.Deserialize<Dictionary<string, Usuario>>(json);

//                var usuarioEncontrado = usuarios
//                    .Values
//                    .FirstOrDefault(u =>
//                        u.UsuarioLogin.Equals(usuarioDigitado, StringComparison.OrdinalIgnoreCase) &&
//                        u.Senha == senhaDigitada);

//                if (usuarioEncontrado != null)
//                {
//                    await DisplayAlert("Sucesso", $"Bem-vindo {usuarioEncontrado.Nome}!", "OK");

//                    // 🔹 Guardar o usuário na sessão
//                    SessaoUsuario.UsuarioLogado = usuarioEncontrado;

//                    // 🔹 Abre MainPage
//                    //await Shell.Current.GoToAsync(nameof(MainPage));
//                    await Shell.Current.GoToAsync(nameof(MainPage));
//                }
//                else
//                {
//                    await DisplayAlert("Erro", "Usuário ou senha incorretos.", "OK");
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            await DisplayAlert("Erro", $"Falha ao conectar: {ex.Message}", "OK");
//        }
//    }

//    private async void BTN_ir_cadastro_Clicked(object sender, EventArgs e)
//    {
//        await Shell.Current.GoToAsync(nameof(CadastroPage));
//    }
//}


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
        string email = EmailEntry.Text;
        string senha = SenhaEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            await DisplayAlert("Erro", "Preencha email e senha.", "OK");
            return;
        }

        var token = await _authService.Login(email, senha);

        if (token == null)
        {
            await DisplayAlert("Erro", "Email ou senha inválidos.", "OK");
            return;
        }

        await SecureStorage.SetAsync("auth_token", token);
        //await Shell.Current.GoToAsync("//MainPage");
        await Shell.Current.GoToAsync(nameof(MainPage));
    }

    private async void OnCadastrarClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CadastroPage));
    }
}
