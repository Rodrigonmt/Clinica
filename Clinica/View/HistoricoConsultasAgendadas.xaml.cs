using Clinica.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
using Clinica.Services;

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
            if (EmpresaContext.Empresa != null)
                Title = EmpresaContext.Empresa.NomeEmpresa;
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

                // 1. Garante os IDs primeiro (Chave do Firebase para o objeto)
                foreach (var item in consultasDict)
                {
                    item.Value.Id = item.Key;
                }

                // 2. Filtra e Ordena (Descendente para Histórico: mais recente primeiro)
                var historico = consultasDict.Values
                    .Where(c => c.Usuario == usuarioLogado)
                    .OrderBy(c => c.Data)
                    .ThenBy(c => c.HoraInicio)
                    .ToList();

                // 3. Atualiza a coleção observada pela UI
                Consultas.Clear();
                foreach (var consulta in historico)
                {
                    Consultas.Add(consulta);
                }

                // 4. Atualiza aviso de lista vazia
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

                await Shell.Current.GoToAsync(nameof(InfoHistoricoConsultaAgendadaPage), parametros);
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