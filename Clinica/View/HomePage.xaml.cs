namespace Clinica.View;

public partial class HomePage : ContentPage
{
	public HomePage()
	{
		InitializeComponent();
	}
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        Avatar.Source = await SecureStorage.GetAsync("foto_url");
        NomeUsuario.Text = await SecureStorage.GetAsync("user_email");
    }

}