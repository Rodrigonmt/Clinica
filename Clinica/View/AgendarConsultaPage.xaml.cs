using Clinica.Models; // Importa o modelo Consulta
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Clinica.View
{
    [QueryProperty(nameof(ConsultaEdicao), "Consulta")]
    
    public partial class AgendarConsultaPage : ContentPage
    {
        private Border _medicoSelecionado;
        private string _medicoNome;
        private readonly HttpClient _httpClient;
        private const string FirebaseUrl = "https://clinica-e248d-default-rtdb.firebaseio.com/consultas.json";
        // OBS: o .json no final é OBRIGATÓRIO no Firebase Realtime Database
        private const string FirebaseProfissionaisUrl = "https://clinica-e248d-default-rtdb.firebaseio.com/profissionais.json";
        private const string FirebaseServicosUrl = "https://clinica-e248d-default-rtdb.firebaseio.com/servicos.json";
        private readonly ObservableCollection<Servico> _servicos = new();
        private const string FirebaseCalendariosUrl = "https://clinica-e248d-default-rtdb.firebaseio.com/calendarios.json";
        private string _medicoId;
        private Consulta _consultaEdicao;
        private bool _modoReagendamento => _consultaEdicao != null;


        public AgendarConsultaPage()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            timePicker.IsEnabled = false;
            timePicker.Focused += OnTimePickerFocused;
            datePicker.DateSelected += OnDataSelecionada;
            datePicker.MinimumDate = DateTime.Today;
            datePicker.DateSelected += (s, e) => AtualizarHorariosDisponiveis();

            // Força idioma português
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pt-BR");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("pt-BR");
            Loaded += async (_, __) => await CarregarProfissionaisAsync();

            datePicker.MinimumDate = DateTime.Today; // opcional
        }

        private decimal CalcularValorServicos()
        {
            return _servicos
                .Where(s => s.Selecionado)
                .Sum(s => s.Preco);
        }

        private ImageSource Base64ToImage(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return "user_placeholder.png"; // imagem padrão

            var base64Data = base64.Contains(",")
                ? base64.Substring(base64.IndexOf(",") + 1)
                : base64;

            byte[] bytes = Convert.FromBase64String(base64Data);

            return ImageSource.FromStream(() => new MemoryStream(bytes));
        }

        private async Task SalvarNovoAgendamentoAsync(string servicos)
        {
            var consulta = new Consulta
            {
                Data = datePicker.Date,
                Hora = timePicker.SelectedItem.ToString(),
                Medico = _medicoNome,
                Servico = servicos,
                CriadoEm = DateTime.UtcNow,
                Usuario = SessaoUsuario.UsuarioLogado?.UserId,
                Status = StatusConsulta.Agendada,
                Observacoes = txtObservacoes.Text,
                ValorTotal = CalcularValorServicos()
            };

            var json = JsonSerializer.Serialize(consulta);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _httpClient.PostAsync(FirebaseUrl, content);

            await DisplayAlert("Sucesso", "Consulta agendada com sucesso!", "OK");
            await Shell.Current.GoToAsync("/MainPage");
        }

        public Consulta ConsultaEdicao
        {
            get => _consultaEdicao;
            set
            {
                _consultaEdicao = value;
                if (_consultaEdicao != null)
                    PreencherDadosReagendamento();
            }
        }

        private async void PreencherDadosReagendamento()
        {
            Title = "Reagendar Consulta";

            datePicker.Date = _consultaEdicao.Data;
            txtObservacoes.Text = _consultaEdicao.Observacoes;

            // Médico
            _medicoNome = _consultaEdicao.Medico;

            // Serviços
            var servicosSelecionados = _consultaEdicao.Servico.Split(" + ");

            foreach (var servico in _servicos)
                servico.Selecionado = servicosSelecionados.Contains(servico.Nome);

            AtualizarValorTotal();

            await Task.Delay(200);
            AtualizarHorariosDisponiveis();

            timePicker.SelectedItem = _consultaEdicao.Hora;

            // Texto do botão
            ((Label)((Border)btnAgendar).Content).Text = "Confirmar Reagendamento";
        }

        private async Task CarregarProfissionaisAsync()
        {
            try
            {
                var json = await _httpClient.GetStringAsync(FirebaseProfissionaisUrl);

                if (string.IsNullOrWhiteSpace(json) || json == "null")
                    return;

                var dict = JsonSerializer.Deserialize<Dictionary<string, Profissional>>(json);

                medicosStack.Children.Clear();

                foreach (var item in dict)
                {
                    var profissional = item.Value;
                    profissional.ProfissionalId = item.Key;

                    medicosStack.Children.Add(CriarCardProfissional(profissional));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", "Erro ao carregar profissionais: " + ex.Message, "OK");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CarregarServicosAsync();
        }

        private async Task CarregarServicosAsync()
        {
            try
            {
                var json = await _httpClient.GetStringAsync(FirebaseServicosUrl);

                if (string.IsNullOrWhiteSpace(json) || json == "null")
                    return;

                var dict = JsonSerializer.Deserialize<Dictionary<string, Servico>>(json);

                _servicos.Clear();

                foreach (var item in dict)
                {
                    item.Value.ServicoId = item.Key;
                    _servicos.Add(item.Value);
                }

                servicosCollection.ItemsSource = _servicos;

                AtualizarValorTotal();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", "Erro ao carregar serviços: " + ex.Message, "OK");
            }
        }

        private Microsoft.Maui.Controls.View CriarCardProfissional(Profissional prof)
        {
            var border = new Border
            {
                StrokeThickness = 4,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 40 },
                BackgroundColor = Color.FromArgb(prof.Cor ?? "#D0E8FF"),
                HeightRequest = 80,
                WidthRequest = 80
            };

            var tap = new TapGestureRecognizer
            {
                CommandParameter = prof
            };
            tap.Tapped += OnMedicoTapped;

            border.GestureRecognizers.Add(tap);

            border.Content = new Image
            {
                Source = Base64ToImage(prof.FotoPerfil),
                Aspect = Aspect.AspectFill
            };

            return new VerticalStackLayout
            {
                WidthRequest = 80,
                Spacing = 5,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    border,
                    new Label
                    {
                        Text = prof.Nome,
                        FontSize = 14,
                        TextColor = Color.FromArgb("#007AFF"),
                        HorizontalOptions = LayoutOptions.Center,
                        LineBreakMode = LineBreakMode.TailTruncation
                    }
                }
            };
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

        // 👉 Evento ao clicar em um médico
        private void OnMedicoTapped(object sender, EventArgs e)
        {
            var borderClicado = sender as Border ??
                                ((sender as TapGestureRecognizer)?.Parent as Border);
            if (borderClicado == null) return;

            if (_medicoSelecionado != null)
            {
                _medicoSelecionado.Stroke = Colors.Transparent;
                _medicoSelecionado.BackgroundColor = Color.FromArgb("#D0E8FF");
            }

            borderClicado.Stroke = Colors.Blue;
            borderClicado.BackgroundColor = Color.FromArgb("#ADD8FF");
            _medicoSelecionado = borderClicado;

            if (borderClicado.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap)
            {
                var prof = tap.CommandParameter as Profissional;
                _medicoId = prof?.ProfissionalId;
                _medicoNome = prof?.Nome;
            }

            // 🔥 Limpa o horário SEMPRE que trocar o médico
            timePicker.SelectedItem = null;
            timePicker.ItemsSource = null;
            timePicker.Title = "Escolha um horário";

            // Ativa o picker
            timePicker.IsEnabled = true;

            AtualizarHorariosDisponiveis();
        }

        // 👉 Evento do botão "Agendar Consulta"
        private async void OnAgendarConsultaClicked(object sender, EventArgs e)
        {
            await ClickEffect((VisualElement)sender);

            if (string.IsNullOrEmpty(_medicoNome))
            {
                await DisplayAlert("Aviso", "Selecione um médico.", "OK");
                return;
            }

            var servicos = ObterServicosSelecionados();
            if (string.IsNullOrWhiteSpace(servicos))
            {
                await DisplayAlert("Aviso", "Selecione pelo menos um serviço.", "OK");
                return;
            }

            if (timePicker.SelectedItem == null)
            {
                await DisplayAlert("Aviso", "Selecione um horário.", "OK");
                return;
            }

            if (_modoReagendamento)
                await SalvarReagendamentoAsync(servicos);
            else
                await SalvarNovoAgendamentoAsync(servicos);
        }

        private async Task SalvarReagendamentoAsync(string servicos)
        {
            try
            {
                var update = new
                {
                    data = datePicker.Date,
                    hora = timePicker.SelectedItem.ToString(),
                    medico = _medicoNome,
                    servico = servicos,
                    observacoes = txtObservacoes.Text,
                    valorTotal = CalcularValorServicos(),
                    status = StatusConsulta.Reagendada
                };

                var json = JsonSerializer.Serialize(update);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://clinica-e248d-default-rtdb.firebaseio.com/consultas/{_consultaEdicao.Id}.json";

                var response = await _httpClient.PatchAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Erro", "Falha ao reagendar.", "OK");
                    return;
                }

                await DisplayAlert("Sucesso", "Consulta reagendada com sucesso!", "OK");
                await Shell.Current.GoToAsync("/HistoricoConsultasAgendadasPage");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", ex.Message, "OK");
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

        private async Task ClickEffect(VisualElement element)
        {
            await element.ScaleTo(0.92, 80);
            await element.FadeTo(0.7, 70);
            await element.FadeTo(1, 70);
            await element.ScaleTo(1, 80);
        }

        private readonly List<string> _horariosBase = new()
        {
            "08:00","08:30","09:00","09:30","10:00","10:30",
            "11:00","11:30","14:00","14:30","15:00","15:30","16:00"
        };

        private async void AtualizarHorariosDisponiveis()
        {
            if (string.IsNullOrEmpty(_medicoNome))
                return;

            try
            {
                // 🔹 Descobre o dia da semana em PT-BR
                var diaSemana = datePicker.Date.DayOfWeek switch
                {
                    DayOfWeek.Monday => "Segunda",
                    DayOfWeek.Tuesday => "Terca",
                    DayOfWeek.Wednesday => "Quarta",
                    DayOfWeek.Thursday => "Quinta",
                    DayOfWeek.Friday => "Sexta",
                    DayOfWeek.Saturday => "Sabado",
                    DayOfWeek.Sunday => "Domingo",
                    _ => ""
                };


                // 🔹 Busca calendários
                var jsonCalendarios = await _httpClient.GetStringAsync(FirebaseCalendariosUrl);
                if (string.IsNullOrWhiteSpace(jsonCalendarios) || jsonCalendarios == "null")
                    return;

                var calendarios = JsonSerializer.Deserialize<
                    Dictionary<string, CalendarioProfissional>>(jsonCalendarios);

                // 🔹 Calendário do profissional selecionado
                var calendario = calendarios.Values
                    .FirstOrDefault(c => c.ProfissionalId == _medicoId);

                if (calendario == null)
                {
                    timePicker.ItemsSource = null;
                    timePicker.Title = "Profissional sem agenda";
                    return;
                }

                // 🔹 Verifica se trabalha neste dia
                if (!calendario.DiasSemana.ContainsKey(diaSemana) ||
                    !calendario.DiasSemana[diaSemana])
                {
                    timePicker.ItemsSource = null;
                    timePicker.Title = "Não atende neste dia";
                    return;
                }

                // 🔹 Slots do dia
                if (!calendario.Horarios.ContainsKey(diaSemana))
                    return;

                var slotsDoDia = calendario.Horarios[diaSemana].Slots;

                // 🔹 Buscar consultas já agendadas
                var jsonConsultas = await _httpClient.GetStringAsync(FirebaseUrl);
                var consultasDict = JsonSerializer.Deserialize<
                    Dictionary<string, Consulta>>(jsonConsultas ?? "");

                var dataSelecionada = datePicker.Date.Date;

                var horariosOcupados = consultasDict?
                    .Where(c =>
                        c.Value.Medico == _medicoNome &&
                        c.Value.Data.Date == dataSelecionada &&
                        c.Value.Status != StatusConsulta.CanceladaCliente &&
                        c.Value.Status != StatusConsulta.CanceladaEmpresa)
                    .Select(c => c.Value.Hora)
                    .ToList() ?? new List<string>();

                // 🔹 Horários finais disponíveis
                var horariosDisponiveis = slotsDoDia
                    .Where(h => !horariosOcupados.Contains(h))
                    .ToList();

                timePicker.ItemsSource = horariosDisponiveis;
                timePicker.SelectedItem = null;
                timePicker.Title = "Escolha um horário";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro",
                    "Erro ao carregar horários: " + ex.Message,
                    "OK");
            }
        }


        private async void OnDataSelecionada(object sender, DateChangedEventArgs e)
        {
            if (e.NewDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                await DisplayAlert("Data inválida",
                    "A clínica não realiza atendimentos aos finais de semana.",
                    "OK");

                datePicker.Date = e.OldDate;
                return;
            }

            // 🔥 Sempre limpar o horário ao trocar a data
            timePicker.SelectedItem = null;
            timePicker.ItemsSource = null;
            timePicker.Title = "Escolha um horário";

            await Task.Delay(100);
            AtualizarHorariosDisponiveis();
        }

        private async void OnTimePickerFocused(object sender, FocusEventArgs e)
        {
            // Se não existe médico selecionado → bloqueia
            if (string.IsNullOrEmpty(_medicoNome))
            {
                timePicker.Unfocus(); // fecha o picker
                await DisplayAlert("Aviso", "Selecione um médico antes de escolher o horário.", "OK");
            }
        }

        private string ObterServicosSelecionados()
        {
            var selecionados = _servicos
                .Where(s => s.Selecionado)
                .Select(s => s.Nome)
                .ToList();

            return string.Join(" + ", selecionados);
        }
    }
}