namespace Clinica
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnAgendarConsultaTapped(object sender, TappedEventArgs e)
        {
            // Navega para a página AgendarConsultaPage
            await Shell.Current.GoToAsync(nameof(View.AgendarConsultaPage));
        }
    }
}