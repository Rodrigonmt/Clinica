namespace Clinica.View;
using Clinica.Models;
using Clinica.Services;
using System.Text;
using System.Text.Json;

public partial class PerfilPage : ContentPage
{
    private readonly FirebaseStorageService _storage = new();
    private readonly FirebaseAuthService _auth = new();
    private readonly HttpClient _http = new();
    private string _localId;
    private string _fotoPerfilBase64;

    private const string FirebaseUserUrl =
        "https://clinica-e248d-default-rtdb.firebaseio.com/usuarios";

    public PerfilPage()
    {
        InitializeComponent();
    }

    private async Task CarregarDadosUsuario()
    {
        try
        {
            if (string.IsNullOrEmpty(_localId))
                return;

            var response = await _http.GetAsync($"{FirebaseUserUrl}/{_localId}.json");

            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            if (json == "null") return;

            var dados = JsonSerializer.Deserialize<Usuario>(json);

            NomeEntry.Text = dados?.Nome;
            TelefoneEntry.Text = dados?.Telefone;
            DataNascEntry.Text = dados?.DataNascimento;
            EnderecoEntry.Text = dados?.Endereco;

            if (!string.IsNullOrWhiteSpace(dados?.FotoPerfil) && dados.FotoPerfil != "null")
            {
                _fotoPerfilBase64 = dados.FotoPerfil;

                if (dados.FotoPerfil.StartsWith("data:image"))
                {
                    var base64Data = dados.FotoPerfil[(dados.FotoPerfil.IndexOf(',') + 1)..];
                    var bytes = Convert.FromBase64String(base64Data);

                    FotoPerfil.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                }
            }
            else
            {
                FotoPerfil.Source = "perfil_inicial.jpg";
            }

        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }


    private async void OnAlterarFotoClicked(object sender, EventArgs e)
    {
        try
        {
            var file = await MediaPicker.PickPhotoAsync();
            if (file == null) return;

            using var stream = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            var bytes = ms.ToArray();
            string base64 = Convert.ToBase64String(bytes);

            _fotoPerfilBase64 = $"data:image/jpeg;base64,{base64}";

            FotoPerfil.Source = ImageSource.FromStream(() => new MemoryStream(bytes));

            await SalvarCampo("FotoPerfil", _fotoPerfilBase64);

            await DisplayAlert("Sucesso", "Foto atualizada!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }



    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _localId = await SecureStorage.GetAsync("local_id");
        await CarregarDadosUsuario();

        // Se não tiver foto, usar imagem padrão
        if (FotoPerfil.Source == null)
            FotoPerfil.Source = "perfil_inicial.jpg";

    }



    private async void OnSalvarClicked(object sender, EventArgs e)
    {
        try
        {
            var dados = new Usuario
            {
                UserId = _localId,
                Nome = NomeEntry.Text,
                Telefone = TelefoneEntry.Text,
                DataNascimento = DataNascEntry.Text,
                Endereco = EnderecoEntry.Text,
                FotoPerfil = _fotoPerfilBase64, // Agora correto!
                Email = await SecureStorage.GetAsync("email")
            };

            string json = JsonSerializer.Serialize(dados);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PutAsync($"{FirebaseUserUrl}/{_localId}.json", content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Sucesso", "Dados atualizados!", "OK");
                await Shell.Current.GoToAsync("/MainPage");
            }
            else
            {
                await DisplayAlert("Erro", "Falha ao salvar dados!", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }



    private async Task SalvarCampo(string campo, string valor)
    {
        var json = JsonSerializer.Serialize(valor);
        await _http.PatchAsync(
            $"{FirebaseUserUrl}/{_localId}/{campo}.json",
            new StringContent(json, Encoding.UTF8, "application/json")
        );
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.GoToAsync("/MainPage");
        });

        return true;
    }

    private async Task Logout()
    {
        SessaoUsuario.UsuarioLogado = null;

        SecureStorage.Remove("auth_token");
        SecureStorage.Remove("refresh_token");
        SecureStorage.Remove("user_id");
        SecureStorage.Remove("foto_url");
        SecureStorage.Remove("lembrar");

        await Shell.Current.GoToAsync("//LoginPage");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Sair", "Deseja realmente sair?", "Sim", "Não");

        if (confirm)
            await Logout();
    }

}