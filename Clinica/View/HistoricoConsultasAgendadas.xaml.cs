using Clinica.Models;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Clinica.View
{
    public partial class HistoricoConsultasAgendadasPage : ContentPage
    {
        private const string FirebaseUrl = "https://clinica-e248d-default-rtdb.firebaseio.com/consultas.json";

        public ObservableCollection<Consulta> Consultas { get; set; } = new();

        public HistoricoConsultasAgendadasPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CarregarConsultas();
        }

        private async Task CarregarConsultas()
        {
            try
            {
                using var client = new HttpClient();
                string json = await client.GetStringAsync(FirebaseUrl);

                if (string.IsNullOrWhiteSpace(json) || json == "null")
                {
                    lblSemConsultas.IsVisible = true;
                    return;
                }

                var consultasDict = JsonSerializer.Deserialize<Dictionary<string, Consulta>>(json);

                var usuarioLogado = SessaoUsuario.UsuarioLogado?.UserId;


                if (string.IsNullOrEmpty(usuarioLogado))
                {
                    await DisplayAlert("Erro", "Usuário não está logado.", "OK");
                    return;
                }

                // 🔹 Carrega TODAS as consultas do usuário logado
                var historico = consultasDict
                    .Values
                    .Where(c => c.Usuario == usuarioLogado)
                    .OrderByDescending(c => c.Data)
                    .ThenByDescending(c => c.Hora)
                    .ToList();

                Consultas.Clear();
                foreach (var consulta in historico)
                    Consultas.Add(consulta);

                lblSemConsultas.IsVisible = !Consultas.Any();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Falha ao carregar histórico: {ex.Message}", "OK");
            }
        }

        private async void OnConsultaTapped(object sender, TappedEventArgs e)
        {
            if (sender is Frame frame && frame.BindingContext is Consulta consulta)
            {
                var parametros = new Dictionary<string, object>
                {
                    { "Consulta", consulta }
                };

                await Shell.Current.GoToAsync(nameof(InfoConsultasAgendadaPage), parametros);
            }
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
}
