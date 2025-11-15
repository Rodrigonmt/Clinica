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

        private async void OnConsultasAgendadasTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(View.ConsultasAgendaPage));
        }

    }
}