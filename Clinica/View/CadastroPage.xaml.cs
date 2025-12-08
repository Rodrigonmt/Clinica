//using Clinica.Models;
//using System.Text;
//using System.Text.Json;

//namespace Clinica.View;

//public partial class CadastroPage : ContentPage
//{
//    private const string FirebaseUrl = "https://clinica-e248d-default-rtdb.firebaseio.com/usuarios.json";
//    // 👆 troquei "consultas.json" por "usuarios.json" (pra separar cadastros de usuários das consultas)

//    public CadastroPage()
//    {
//        InitializeComponent();
//    }

//    private async void BtnCadastrar_Clicked(object sender, EventArgs e)
//    {
//        try
//        {
//            Usuario novoUsuario = new Usuario
//            {
//                Nome = TxtNome.Text,
//                UsuarioLogin = TxtUsuario.Text,
//                Senha = TxtSenha.Text,
//                CPF = TxtCPF.Text,
//                Telefone = TxtTelefone.Text,
//                Cidade = TxtCidade.Text
//            };

//            if (int.TryParse(TxtIdade.Text, out int idade))
//                novoUsuario.Idade = idade;
//            else
//                novoUsuario.Idade = 0;

//            novoUsuario.CriadoEm = DateTime.UtcNow;

//            // 🔹 Serializar para JSON
//            string json = JsonSerializer.Serialize(novoUsuario);

//            using (HttpClient client = new HttpClient())
//            {
//                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

//                HttpResponseMessage response = await client.PostAsync(FirebaseUrl, content);

//                if (response.IsSuccessStatusCode)
//                {
//                    await DisplayAlert("Sucesso", "Usuário cadastrado no Firebase!", "OK");
//                    await Shell.Current.GoToAsync(".."); // voltar para Login
//                }
//                else
//                {
//                    await DisplayAlert("Erro", $"Falha ao cadastrar: {response.StatusCode}", "OK");
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            await DisplayAlert("Erro", $"Falha: {ex.Message}", "OK");
//        }
//    }
//}


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
}
