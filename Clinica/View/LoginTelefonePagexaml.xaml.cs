using Clinica.Services;

namespace Clinica.View;

public partial class LoginTelefonePage : ContentPage
{
    private readonly IFirebasePhoneAuthService _phoneAuth;
    private string _verificationId;

    public LoginTelefonePage()
    {
        InitializeComponent();
        _phoneAuth = DependencyService.Get<IFirebasePhoneAuthService>();
    }

    private async void OnEnviarSmsClicked(object sender, EventArgs e)
    {
        _verificationId = await _phoneAuth.EnviarSmsAsync(TelefoneEntry.Text);

        CodigoEntry.IsVisible = true;
        ((Button)sender).IsVisible = false;
    }

    private async void OnConfirmarCodigoClicked(object sender, EventArgs e)
    {
        var auth = await _phoneAuth.ConfirmarCodigoAsync(_verificationId, CodigoEntry.Text);

        if (auth == null)
        {
            await DisplayAlert("Erro", "Código inválido", "OK");
            return;
        }

        await SecureStorage.SetAsync("auth_token", auth.idToken);
        await SecureStorage.SetAsync("user_id", auth.localId);

        await Shell.Current.GoToAsync(nameof(MainPage));
    }
}
