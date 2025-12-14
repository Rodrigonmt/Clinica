//Tela com o as informações detalhadas de uma consulta agendada no histórico do paciente.
//Possui as consultas recentes e as antigas/Diferente da tela de consultas agendadas, essa tela não permite editar as informações da consulta, apenas visualizar exceto nas consultas com status agendada, reagendada e confirmada


using Clinica.Models;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;



namespace Clinica.View
{
    [QueryProperty(nameof(Consulta), "Consulta")]

    public partial class InfoHistoricoConsultaAgendadaPage : ContentPage
    {
        private Consulta _consulta;
        private readonly HttpClient _httpClient = new HttpClient();
        private const string FirebaseUrl = "https://clinica-e248d-default-rtdb.firebaseio.com";


        public Consulta Consulta
        {
            get => _consulta;
            set
            {
                _consulta = value;
                CarregarDados();
            }
        }
        public InfoHistoricoConsultaAgendadaPage()
        {
            InitializeComponent();
        }

        private void CarregarDados()
        {
            if (Consulta == null)
                return;

            lblData.Text = Consulta.Data.ToString("dd/MM/yyyy");
            lblHora.Text = Consulta.Hora;
            lblMedico.Text = Consulta.Medico;
            lblStatus.Text = Consulta.Status.ToString();
            lblServico.Text = Consulta.Servico; // <-- ADICIONADO
            lblValorTotal.Text = $"R$ {Consulta.ValorTotal:0.00}";


            lblObservacoes.Text = string.IsNullOrWhiteSpace(Consulta.Observacoes)
                ? "Nenhuma observação registrada."
                : Consulta.Observacoes;
            AtualizarEstadoDosBotoes();
        }

        private void AtualizarEstadoDosBotoes()
        {
            // Status permitidos
            bool podeEditar =
                Consulta.Status == StatusConsulta.Agendada ||
                Consulta.Status == StatusConsulta.Confirmada ||
                Consulta.Status == StatusConsulta.Reagendada;

            // Cancelar
            BtnCancelar.IsEnabled = podeEditar;
            BtnCancelar.BackgroundColor = podeEditar ? Color.FromArgb("#D32F2F") : Colors.LightGray;

            // Reagendar
            BtnReagendar.IsEnabled = podeEditar;
            BtnReagendar.BackgroundColor = podeEditar ? Color.FromArgb("#1976D2") : Colors.LightGray;
        }


        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Cancelar consulta",
                                              "Tem certeza que deseja cancelar esta consulta?",
                                              "Sim", "Não");

            if (confirm)
            {
                // Sua lógica para cancelar a consulta aqui
                bool sucesso = await CancelarConsultaAsync();

                if (sucesso)
                {
                    await DisplayAlert("Cancelada", "A consulta foi cancelada.", "OK");
                    await Shell.Current.GoToAsync("/HistoricoConsultasAgendadasPage");
                }
                else
                {
                    await DisplayAlert("Erro", "Não foi possível cancelar a consulta.", "OK");
                }

            }

        }

        private async void OnReagendarClicked(object sender, EventArgs e)
        {
            if (Consulta == null)
            {
                await DisplayAlert("Erro", "Consulta inválida.", "OK");
                return;
            }

            await Shell.Current.GoToAsync(
                nameof(AgendarConsultaPage),
                true,
                new Dictionary<string, object>
                {
            { "Consulta", Consulta }
                });
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("/HistoricoConsultasAgendadasPage");
            });

            return true;
        }

        private async Task<bool> CancelarConsultaAsync()
        {
            try
            {
                if (Consulta == null || string.IsNullOrEmpty(Consulta.Id))
                {
                    await DisplayAlert("Erro", "Consulta inválida.", "OK");
                    return false;
                }

                string url = $"{FirebaseUrl}/consultas/{Consulta.Id}.json";

                var update = new
                {
                    status = (int)StatusConsulta.CanceladaCliente
                };

                var json = JsonSerializer.Serialize(update);

                var content = new StringContent(json, Encoding.UTF8);
                content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                var response = await _httpClient.PatchAsync(url, content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Erro ao cancelar: {ex.Message}", "OK");
                return false;
            }
        }

    }
}