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

                var usuarioLogado = SessaoUsuario.UsuarioLogado?.UserId;

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

                Consultas.Clear();

                foreach (var item in consultasDict)
                {
                    var id = item.Key;
                    var consulta = item.Value;

                    // 🔹 Inserimos o ID no objeto recebido
                    consulta.Id = id;

                    // 🔹 Filtro de usuário e status
                    if (consulta.Usuario == usuarioLogado &&
                        statusPermitidos.Contains(consulta.Status))
                    {
                        Consultas.Add(consulta);
                    }
                }

                Consultas = new ObservableCollection<Consulta>(
                    Consultas.OrderBy(c => c.Data).ThenBy(c => c.Hora)
                );


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
        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("/MainPage"); // volta para a principal
            });

            return true;
        }

    }
}
