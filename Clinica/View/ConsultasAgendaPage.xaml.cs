using Clinica.Models;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Clinica.View
{
    public partial class ConsultasAgendaPage : ContentPage
    {
        private const string FirebaseUrl = "https://clinica-e248d-default-rtdb.firebaseio.com/consultas.json";

        public ObservableCollection<Consulta> Consultas { get; set; } = new();

        public ConsultasAgendaPage()
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
                    //lblSemConsultas.IsVisible = true;
                    lblSemConsultas.IsVisible = true;
                    return;
                }

                // 🔹 Firebase retorna um dicionário (id gerado -> objeto consulta)
                var consultasDict = JsonSerializer.Deserialize<Dictionary<string, Consulta>>(json);

                var usuarioLogado = SessaoUsuario.UsuarioLogado?.UsuarioLogin;

                if (string.IsNullOrEmpty(usuarioLogado))
                {
                    await DisplayAlert("Erro", "Usuário não está logado.", "OK");
                    return;
                }

                var statusPermitidos = new[]
                {
                    StatusConsulta.Agendada,
                    StatusConsulta.Confirmada,
                    StatusConsulta.Reagendada
                };

                var minhasConsultas = consultasDict
                .Values
                .Where(c =>
                    c.Usuario == usuarioLogado &&
                    statusPermitidos.Contains(c.Status)
                )
                .OrderBy(c => c.Data)
                .ThenBy(c => c.Hora)
                .ToList();

                Consultas.Clear();
                foreach (var consulta in minhasConsultas)
                    Consultas.Add(consulta);

                lblSemConsultas.IsVisible = !Consultas.Any();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Falha ao carregar consultas: {ex.Message}", "OK");
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


    }
}
