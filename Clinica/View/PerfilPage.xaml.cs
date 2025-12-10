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
        BindingContext = this; // Necessário para carregar lista de estados
    }

    public List<string> Estados { get; set; } = new()
    {
        "RS", "SC", "PR", "SP", "RJ", "MG", "ES", "BA",
        "GO", "MT", "MS", "DF", "RO", "RR", "AM", "AC",
        "PA", "AP", "MA", "PI", "CE", "RN", "PB", "PE",
        "AL", "SE", "TO"
    };


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

            // Endereço (novo modelo)
            if (!string.IsNullOrEmpty(dados?.Endereco))
            {
                var partes = dados.Endereco.Split('|');
                if (partes.Length >= 4)
                {
                    EstadoPicker.SelectedItem = partes[0];
                    CidadePicker.SelectedItem = partes[1];
                    RuaEntry.Text = partes[2];
                    NumeroEntry.Text = partes[3];
                }

                if (partes.Length == 6)
                {
                    TipoResidenciaPicker.SelectedItem = partes[4];
                    NomeApartamentoEntry.Text = partes[5];
                    NomeApartamentoEntry.IsVisible = partes[4] == "Apartamento";
                }
            }

            // FOTO
            if (!string.IsNullOrWhiteSpace(dados?.FotoPerfil) && dados.FotoPerfil != "null")
            {
                _fotoPerfilBase64 = dados.FotoPerfil;

                var base64Data = dados.FotoPerfil[(dados.FotoPerfil.IndexOf(',') + 1)..];
                var bytes = Convert.FromBase64String(base64Data);

                FotoPerfil.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
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

        if (FotoPerfil.Source == null)
            FotoPerfil.Source = "perfil_inicial.jpg";
    }



    private async void OnSalvarClicked(object sender, EventArgs e)
    {
        try
        {
            // Salvar endereço no novo formato
            string enderecoFormatado =
                $"{EstadoPicker.SelectedItem}|{CidadePicker.SelectedItem}|{RuaEntry.Text}|{NumeroEntry.Text}|{TipoResidenciaPicker.SelectedItem}|{NomeApartamentoEntry.Text}";

            var dados = new Usuario
            {
                UserId = _localId,
                Nome = NomeEntry.Text,
                Telefone = TelefoneEntry.Text,
                DataNascimento = DataNascEntry.Text,
                Endereco = enderecoFormatado,
                FotoPerfil = _fotoPerfilBase64,
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



    private CancellationTokenSource _telefoneCts;

    private void TelefoneEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_telefoneCts != null)
            _telefoneCts.Cancel();

        bool inserindo = e.NewTextValue?.Length > e.OldTextValue?.Length;

        int oldCursor = TelefoneEntry.CursorPosition;

        _telefoneCts = new CancellationTokenSource();
        var token = _telefoneCts.Token;

        Task.Delay(30).ContinueWith(_ =>
        {
            if (token.IsCancellationRequested) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                string digits = new string(TelefoneEntry.Text.Where(char.IsDigit).ToArray());
                if (digits.Length > 11)
                    digits = digits[..11];

                string formatted;

                if (digits.Length <= 10)
                    formatted = $"({digits[..Math.Min(2, digits.Length)]}{(digits.Length >= 2 ? ") " : "")}" +
                                $"{(digits.Length > 2 ? digits.Substring(2, Math.Min(4, digits.Length - 2)) : "")}" +
                                $"{(digits.Length > 6 ? "-" : "")}" +
                                $"{(digits.Length > 6 ? digits[6..] : "")}";
                else
                    formatted = $"({digits[..2]}) {digits.Substring(2, 5)}-{digits[7..]}";

                if (TelefoneEntry.Text != formatted)
                {
                    TelefoneEntry.Text = formatted;

                    int newCursor = oldCursor;

                    if (inserindo)
                    {
                        // Avança cursor ao inserir mascara automaticamente
                        if (newCursor == 1) newCursor = 2;                // "("
                        else if (newCursor == 3) newCursor = 4;           // ") "
                        else if (newCursor == 9) newCursor = 10;          // "-"
                        else newCursor++;
                    }
                    else
                    {
                        // Ao apagar, não deixa cursor saltar errado
                        if (newCursor > formatted.Length)
                            newCursor = formatted.Length;
                    }

                    TelefoneEntry.CursorPosition = Math.Min(newCursor, formatted.Length);
                }
            });
        }, token);
    }



    private CancellationTokenSource _dataCts;

    private void DataNascEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_dataCts != null)
            _dataCts.Cancel();

        _dataCts = new CancellationTokenSource();
        var token = _dataCts.Token;

        Task.Delay(50).ContinueWith(_ =>
        {
            if (token.IsCancellationRequested) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                string digits = new string(DataNascEntry.Text.Where(char.IsDigit).ToArray());

                if (digits.Length > 8)
                    digits = digits[..8];

                string formatted = digits.Length switch
                {
                    >= 5 => $"{digits[..2]}/{digits.Substring(2, 2)}/{digits[4..]}",
                    >= 3 => $"{digits[..2]}/{digits[2..]}",
                    _ => digits
                };

                if (DataNascEntry.Text != formatted)
                {
                    DataNascEntry.Text = formatted;
                    DataNascEntry.CursorPosition = formatted.Length;
                }
            });

        }, token);
    }




    private void TipoResidenciaPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        NomeApartamentoEntry.IsVisible =
            TipoResidenciaPicker.SelectedItem?.ToString() == "Apartamento";
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
