namespace Clinica.View;
using Clinica.Services;
using Clinica.Models;
public partial class PerfilPage : ContentPage
{
    private readonly FirebaseStorageService _storage = new();
    private readonly FirebaseAuthService _auth = new();

    public PerfilPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        string foto = await SecureStorage.GetAsync("foto_url");
        if (!string.IsNullOrEmpty(foto))
            FotoPerfil.Source = foto;
    }

    private async void OnAlterarFotoClicked(object sender, EventArgs e)
    {
        try
        {
            var file = await MediaPicker.PickPhotoAsync();
            if (file == null) return;

            Stream stream = await file.OpenReadAsync();

            string token = await SecureStorage.GetAsync("auth_token");
            string localId = await SecureStorage.GetAsync("local_id");

            var url = await _storage.UploadFotoAsync(stream, localId, token);

            if (url != null)
            {
                FotoPerfil.Source = url;
                await SecureStorage.SetAsync("foto_url", url);
                await DisplayAlert("Sucesso", "Foto atualizada!", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.GoToAsync("//LoginPage");
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