namespace Clinica.View;
using Clinica.Models;
using Clinica.Services;
using System.Text;
using System.Text.Json;
using Clinica.Services;

public partial class PerfilPage : ContentPage
{
    private readonly FirebaseStorageService _storage = new();
    private readonly FirebaseAuthService _auth = new();
    private readonly HttpClient _http = new();
    private string _userId;
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
            if (string.IsNullOrEmpty(_userId))
                return;

            var response = await _http.GetAsync($"{FirebaseUserUrl}/{_userId}.json");
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
                    if (!string.IsNullOrEmpty(partes[0]))
                    {
                        EstadoPicker.SelectedItem = partes[0];

                        // primeiro carrega cidades
                        await CarregarCidadesPorEstado(partes[0]);

                        // depois seleciona a cidade salva
                        CidadePicker.SelectedItem = partes[1];
                    }
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

        _userId = await SecureStorage.GetAsync("user_id");
        await CarregarDadosUsuario();

        if (FotoPerfil.Source == null)
            FotoPerfil.Source = "perfil_inicial.jpg";

        if (EmpresaContext.Empresa != null)
            Title = EmpresaContext.Empresa.NomeEmpresa;
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
                UserId = _userId,
                Nome = NomeEntry.Text,
                Telefone = TelefoneEntry.Text,
                DataNascimento = DataNascEntry.Text,
                Endereco = enderecoFormatado,
                FotoPerfil = _fotoPerfilBase64,
                Email = await SecureStorage.GetAsync("email")
            };

            string json = JsonSerializer.Serialize(dados);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PutAsync($"{FirebaseUserUrl}/{_userId}.json", content);

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
            $"{FirebaseUserUrl}/{_userId}/{campo}.json",
            new StringContent(json, Encoding.UTF8, "application/json")
        );
    }



    private CancellationTokenSource _telefoneCts;

    private void TelefoneEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Cancela qualquer formatação pendente
        if (_telefoneCts != null)
            _telefoneCts.Cancel();

        // Verifica se está digitando ou apagando
        bool inserindo = e.NewTextValue?.Length > e.OldTextValue?.Length;

        // Guarda posição atual do cursor
        int oldCursor = TelefoneEntry.CursorPosition;

        // Novo token de cancelamento
        _telefoneCts = new CancellationTokenSource();
        var token = _telefoneCts.Token;

        // Aguarda 30ms antes de formatar
        Task.Delay(30).ContinueWith(_ =>
        {
            if (token.IsCancellationRequested) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Mantém somente dígitos
                string digits = new string(TelefoneEntry.Text.Where(char.IsDigit).ToArray());

                // Limita para 11 dígitos (celular)
                if (digits.Length > 11)
                    digits = digits[..11];

                // Monta a máscara de celular
                string formatted = digits.Length switch
                {
                    <= 2 => $"({digits}",                                                                             // (99
                    <= 7 => $"({digits[..2]}) {digits.Substring(2)}",                                                  // (99) 9999
                    <= 11 => $"({digits[..2]}) {digits.Substring(2, 5)}-{digits.Substring(7)}",                       // (99) 99999-9999
                    _ => TelefoneEntry.Text
                };

                // Evita loop
                if (TelefoneEntry.Text != formatted)
                {
                    TelefoneEntry.Text = formatted;

                    // CORREÇÃO DO PROBLEMA DO DDD
                    // Durante a digitação dos dois primeiros dígitos, sempre coloca o cursor no final
                    if (digits.Length <= 2)
                    {
                        TelefoneEntry.CursorPosition = formatted.Length;
                        return;
                    }

                    int newCursor;

                    if (inserindo)
                    {
                        // Inserindo ? cursor sempre vai para o final
                        newCursor = formatted.Length;
                    }
                    else
                    {
                        // Apagando ? mantém cursor consistente
                        newCursor = Math.Min(oldCursor, formatted.Length);
                    }

                    TelefoneEntry.CursorPosition = newCursor;
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


    private async Task CarregarCidadesPorEstado(string uf)
    {
        try
        {
            string url = $"https://servicodados.ibge.gov.br/api/v1/localidades/estados/{uf}/municipios";

            using var http = new HttpClient();
            var response = await http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Erro", "Falha ao carregar cidades.", "OK");
                return;
            }

            var json = await response.Content.ReadAsStringAsync();

            var cidades = JsonSerializer.Deserialize<List<IBGECidade>>(json);

            CidadePicker.ItemsSource = cidades.Select(c => c.nome).ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    public class IBGECidade
    {
        public string nome { get; set; }
    }


    private async void EstadoPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (EstadoPicker.SelectedItem == null)
            return;

        string uf = EstadoPicker.SelectedItem.ToString();

        CidadePicker.ItemsSource = null;
        CidadePicker.SelectedItem = null;

        await CarregarCidadesPorEstado(uf);
    }


    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.GoToAsync("/MainPage");
        });

        return true;
    }


}
