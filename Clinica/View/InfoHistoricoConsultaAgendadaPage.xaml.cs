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
            // ? HORA CORRETA DO FIREBASE
            lblHora.Text = Consulta.HoraInicio;
            lblMedico.Text = Consulta.Medico;
            lblStatus.Text = Consulta.Status.ToString();
            lblServico.Text = Consulta.Servico; // <-- ADICIONADO
            lblValorTotal.Text = $"R$ {Consulta.ValorTotal:0.00}";
            // ? BOA PRÁTICA – USANDO O MODEL
            lblDuracao.Text = Consulta.DuracaoFormatada;


            // ? FORMA DE PAGAMENTO (PADRONIZADA)
            lblFormaPagamento.Text = Consulta.FormaPagamento switch
            {
                "pix" => "PIX",
                "credito" => "Cartão de Crédito",
                "debito" => "Cartão de Débito",
                "carteirasDigitais" => "Carteiras Digitais",
                _ => string.IsNullOrWhiteSpace(Consulta.FormaPagamento)
                        ? "Não informado"
                        : Consulta.FormaPagamento
            };

            lblObservacoes.Text = string.IsNullOrWhiteSpace(Consulta.Observacoes)
                ? "Nenhuma observação registrada."
                : Consulta.Observacoes;
            AtualizarEstadoDosBotoes();

            AplicarCorStatus(Consulta.Status.ToString());


            //lblFormaPagamento.Text = string.IsNullOrWhiteSpace(Consulta.FormaPagamento)
            //    ? "Não informado"
            //    : Consulta.FormaPagamento.ToUpper();

        }


        private void AplicarCorStatus(string status)
        {
            lblStatus.Text = status;

            switch (status.ToLower())
            {
                case "confirmada":
                    lblStatus.TextColor = Colors.Green;
                    break;

                case "pendente":
                    lblStatus.TextColor = Colors.Orange;
                    break;

                case "agendada":
                    lblStatus.TextColor = Colors.Blue;
                    break;

                case "cancelada":
                    lblStatus.TextColor = Colors.Red;
                    break;

                case "reagendada":
                    lblStatus.TextColor = Colors.BlueViolet;
                    break;

                default:
                    lblStatus.TextColor = Colors.Gray;
                    break;
            }
        }

        private void AtualizarEstadoDosBotoes()
        {
            // Status permitidos para ações
            bool podeEditar =
                Consulta.Status == StatusConsulta.Agendada ||
                Consulta.Status == StatusConsulta.Confirmada ||
                Consulta.Status == StatusConsulta.Reagendada;

            // Cancelar
            BtnCancelar.IsEnabled = podeEditar;
            BtnCancelar.BackgroundColor = podeEditar
                ? Color.FromArgb("#D32F2F")
                : Colors.LightGray;

            // Reagendar
            BtnReagendar.IsEnabled = podeEditar;
            BtnReagendar.BackgroundColor = podeEditar
                ? Color.FromArgb("#1976D2")
                : Colors.LightGray;

            // ?? PAGAMENTO PIX (apenas visualização + navegação)
            BtnPagarPix.IsVisible =
                podeEditar &&
                Consulta.FormaPagamento == "pix";
        }


        private async void OnPagarPixClicked(object sender, EventArgs e)
        {
            if (Consulta == null)
            {
                await DisplayAlert("Erro", "Consulta inválida.", "OK");
                return;
            }

            await Shell.Current.GoToAsync(
                nameof(PagamentoPixPage),
                true,
                new Dictionary<string, object>
                {
            { "Consulta", Consulta }
                });
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