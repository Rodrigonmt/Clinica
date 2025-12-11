using Clinica.Models;
using Microsoft.Maui.Controls.Shapes;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Globalization;

namespace Clinica.View
{
    [QueryProperty(nameof(Consulta), "Consulta")]
    public partial class InfoReagendamentoConsultaPage : ContentPage
    {
        private Consulta _consulta;
        private Border _medicoSelecionado;
        private string _medicoNome;

        private readonly HttpClient _httpClient = new();
        private const string FirebaseUrl = "https://clinica-e248d-default-rtdb.firebaseio.com/consultas";

        // ? MESMA LISTA DE BASE DA TELA AGENDAR
        private readonly List<string> _horariosBase = new()
        {
            "08:00","08:30","09:00","09:30","10:00","10:30",
            "11:00","11:30","14:00","14:30","15:00","15:30","16:00"
        };

        public Consulta Consulta
        {
            get => _consulta;
            set
            {
                _consulta = value;
                CarregarDados();
            }
        }

        public InfoReagendamentoConsultaPage()
        {
            InitializeComponent();

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pt-BR");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("pt-BR");

            datePicker.MinimumDate = DateTime.Today;

            // Eventos iguais ao AgendarConsultaPage
            datePicker.DateSelected += OnDataSelecionada;
            timePicker.IsEnabled = false;
            timePicker.Focused += OnTimePickerFocused;

            MontarMedicos();
        }


        private decimal CalcularValorServicos()
        {
            decimal total = 0;

            if (chkCabelo.IsChecked) total += 40;
            if (chkBarba.IsChecked) total += 40;
            if (chkSobrancelha.IsChecked) total += 30;
            if (chkColoracao.IsChecked) total += 80;

            return total;
        }

        private void AtualizarValorTotal()
        {
            decimal total = CalcularValorServicos();
            lblValorTotal.Text = $"R$ {total:0.00}";
        }

        private void OnServicoChanged(object sender, CheckedChangedEventArgs e)
        {
            AtualizarValorTotal();
        }


        // -------------------------------------------------------------
        // CARREGAR DADOS INICIAIS
        // -------------------------------------------------------------
        private async void CarregarDados()
        {
            if (_consulta == null) return;

            // Data
            datePicker.Date = _consulta.Data;

            // Observações
            txtObservacoes.Text = _consulta.Observacoes;

            // Serviços
            chkCabelo.IsChecked = _consulta.Servico.Contains("Cabelo");
            chkBarba.IsChecked = _consulta.Servico.Contains("Barba");
            chkSobrancelha.IsChecked = _consulta.Servico.Contains("Sobrancelha");
            chkColoracao.IsChecked = _consulta.Servico.Contains("Coloração");

            // Médico
            SelecionarMedico(_consulta.Medico);

            // ?? Deve preencher horários de acordo com a data e médico
            await Task.Delay(300);
            await AtualizarHorariosDisponiveis();

            // Horário
            timePicker.SelectedItem = _consulta.Hora;
        }

        // -------------------------------------------------------------
        // MONTAR LISTA DE MÉDICOS IGUAL AO AGENDAR
        // -------------------------------------------------------------
        private void MontarMedicos()
        {
            var medicos = new Dictionary<string, string>
    {
        { "Rodrigo", "rodrigo.jpg" },
        { "Dienifer", "dienifer.jpg" },
        { "Fernanda", "fernanda.jpg" },
        { "Amanda", "amanda.jpg" }
    };

            foreach (var medico in medicos)
            {
                string nome = medico.Key;
                string imagem = medico.Value;

                var border = new Border
                {
                    StrokeThickness = 4,
                    Stroke = Colors.Transparent,
                    StrokeShape = new RoundRectangle { CornerRadius = 40 },
                    BackgroundColor = Color.FromArgb("#D0E8FF"),
                    HeightRequest = 80,
                    WidthRequest = 80
                };

                var tap = new TapGestureRecognizer();
                tap.CommandParameter = nome;
                tap.Tapped += OnMedicoTapped;

                border.GestureRecognizers.Add(tap);

                border.Content = new Image
                {
                    Source = imagem,
                    Aspect = Aspect.AspectFill
                };

                var stack = new VerticalStackLayout
                {
                    WidthRequest = 80,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 5
                };

                stack.Add(border);

                stack.Add(new Label
                {
                    Text = nome,
                    FontSize = 14,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Color.FromArgb("#007AFF")
                });

                medicosStack.Add(stack);
            }
        }


        // -------------------------------------------------------------
        // SELECIONAR MÉDICO
        // -------------------------------------------------------------
        private void SelecionarMedico(string nome)
        {
            foreach (var item in medicosStack.Children)
            {
                if (item is VerticalStackLayout stack &&
                    stack.Children[0] is Border border &&
                    ((TapGestureRecognizer)border.GestureRecognizers[0]).CommandParameter.ToString() == nome)
                {
                    border.Stroke = Colors.Blue;
                    border.BackgroundColor = Color.FromArgb("#ADD8FF");

                    _medicoSelecionado = border;
                    _medicoNome = nome;

                    timePicker.IsEnabled = true;

                    break;
                }
            }
        }

        private async void OnMedicoTapped(object sender, EventArgs e)
        {
            var border = sender as Border;
            if (border == null) return;

            var tap = border.GestureRecognizers[0] as TapGestureRecognizer;
            if (tap == null) return;

            string nome = tap.CommandParameter?.ToString();
            if (string.IsNullOrEmpty(nome)) return;

            // Remove seleção anterior
            if (_medicoSelecionado != null)
            {
                _medicoSelecionado.Stroke = Colors.Transparent;
                _medicoSelecionado.BackgroundColor = Color.FromArgb("#D0E8FF");
            }

            // Seleção atual
            border.Stroke = Colors.Blue;
            border.BackgroundColor = Color.FromArgb("#ADD8FF");

            _medicoSelecionado = border;
            _medicoNome = nome;

            // Limpar horários
            timePicker.SelectedItem = null;
            timePicker.ItemsSource = null;

            //await DisplayAlert("Atenção", "Selecione novamente o horário para este profissional.", "OK");

            await AtualizarHorariosDisponiveis();
        }

        // -------------------------------------------------------------
        // BLOQUEAR FIM DE SEMANA
        // -------------------------------------------------------------
        private async void OnDataSelecionada(object sender, DateChangedEventArgs e)
        {
            if (e.NewDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                await DisplayAlert("Data inválida", "A clínica não atende aos finais de semana.", "OK");
                datePicker.Date = e.OldDate;
                return;
            }

            // ? Limpa o horário selecionado ao trocar a data
            timePicker.SelectedItem = null;
            timePicker.ItemsSource = null;

            //await DisplayAlert("Atenção", "Data alterada. Selecione novamente o horário disponível.", "OK");

            await AtualizarHorariosDisponiveis();
        }

        // -------------------------------------------------------------
        // BLOQUEIO SE NÃO ESCOLHER MÉDICO
        // -------------------------------------------------------------
        private async void OnTimePickerFocused(object sender, FocusEventArgs e)
        {
            if (string.IsNullOrEmpty(_medicoNome))
            {
                timePicker.Unfocus();
                await DisplayAlert("Aviso", "Selecione um profissional antes do horário.", "OK");
                return;
            }

            if (timePicker.ItemsSource == null)
            {
                timePicker.Unfocus();
                await DisplayAlert("Aviso", "Escolha a data para carregar os horários.", "OK");
            }
        }


        // -------------------------------------------------------------
        // ATUALIZAR HORÁRIOS DISPONÍVEIS
        // -------------------------------------------------------------
        private async Task AtualizarHorariosDisponiveis()
        {
            if (string.IsNullOrEmpty(_medicoNome))
                return;

            try
            {
                var response = await _httpClient.GetStringAsync($"{FirebaseUrl}.json");

                if (string.IsNullOrWhiteSpace(response) || response == "null")
                {
                    timePicker.ItemsSource = _horariosBase;
                    timePicker.SelectedItem = null;
                    timePicker.Title = "Escolha um horário";
                    return;
                }

                var consultasDict = JsonSerializer.Deserialize<Dictionary<string, Consulta>>(response);
                var data = datePicker.Date.Date;

                var ocupados = consultasDict
                    .Where(c => c.Value.Medico == _medicoNome &&
                                c.Value.Data.Date == data &&
                                c.Value.Id != _consulta.Id &&
                                c.Value.Status != StatusConsulta.CanceladaEmpresa &&
                                c.Value.Status != StatusConsulta.CanceladaCliente)
                    .Select(c => c.Value.Hora)
                    .ToList();

                var disponiveis = _horariosBase.Where(h => !ocupados.Contains(h)).ToList();

                timePicker.ItemsSource = disponiveis;

                // ?? Importante: não selecionar automaticamente
                timePicker.SelectedItem = null;
                timePicker.Title = "Escolha um horário";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", "Falha ao carregar horários: " + ex.Message, "OK");
            }
        }

        // -------------------------------------------------------------
        // BOTÃO SALVAR REAGENDAMENTO
        // -------------------------------------------------------------
        private async void OnReagendarClicked(object sender, EventArgs e)
        {
            if (_consulta == null)
            {
                await DisplayAlert("Erro", "Consulta inválida.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(_medicoNome))
            {
                await DisplayAlert("Aviso", "Selecione um profissional.", "OK");
                return;
            }

            if (timePicker.SelectedItem == null)
            {
                await DisplayAlert("Aviso", "Selecione um horário.", "OK");
                return;
            }

            var servicos = ObterServicos();

            var atualizacao = new
            {
                data = datePicker.Date,
                hora = timePicker.SelectedItem.ToString(),
                medico = _medicoNome,
                servico = servicos,
                observacoes = txtObservacoes.Text,
                status = (int)StatusConsulta.Reagendada,
                valorTotal = CalcularValorServicos()   // ? ADICIONADO
            };


            string url = $"{FirebaseUrl}/{_consulta.Id}.json";

            var json = JsonSerializer.Serialize(atualizacao);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PatchAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Sucesso", "Consulta reagendada!", "OK");

                _consulta.Data = datePicker.Date;
                _consulta.Hora = timePicker.SelectedItem.ToString();
                _consulta.Medico = _medicoNome;
                _consulta.Servico = servicos;
                _consulta.Observacoes = txtObservacoes.Text;

                await Shell.Current.GoToAsync(nameof(InfoConsultasAgendadaPage), true,
                    new Dictionary<string, object> { { "Consulta", _consulta } });
            }
            else
            {
                await DisplayAlert("Erro", "Não foi possível reagendar.", "OK");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync(nameof(InfoConsultasAgendadaPage), true,
                    new Dictionary<string, object> { { "Consulta", _consulta } });
            });
            return true;
        }

        private string ObterServicos()
        {
            List<string> s = new();

            if (chkCabelo.IsChecked) s.Add("Cabelo");
            if (chkBarba.IsChecked) s.Add("Barba");
            if (chkSobrancelha.IsChecked) s.Add("Sobrancelha");
            if (chkColoracao.IsChecked) s.Add("Coloração de Cabelo");

            return string.Join(" + ", s);
        }
    }
}
