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
using Clinica.Services;
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
        private const string FirebasePagamentosUrl = "https://clinica-e248d-default-rtdb.firebaseio.com/pagamento";
        private ObservableCollection<MetodoPagamentoItem> _pagamentos = new();
        private MetodoPagamentoItem _pagamentoSelecionado;
        private const string FirebaseProfissionaisServicosUrl = "https://clinica-e248d-default-rtdb.firebaseio.com/profissionaisServicos.json";



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

            pagamentosCollection.SelectionChanged += (s, e) =>
            {
                _pagamentoSelecionado = e.CurrentSelection.FirstOrDefault() as MetodoPagamentoItem;
            };


        }

        private decimal CalcularValorServicos()
        {
            return _servicos
                .Where(s => s.Selecionado)
                .Sum(s => s.Preco);
        }
        public static class DataHelperBR
        {
            public static string ParaDataAgenda(DateTime date)
            {
                return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            public static string AgoraIsoUtc()
            {
                return DateTime.UtcNow.ToString("o");
            }
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
            int duracaoTotal = CalcularDuracaoTotal();

            var horaInicio = TimeSpan.Parse(timePicker.SelectedItem.ToString());
            var horaFim = horaInicio.Add(TimeSpan.FromMinutes(duracaoTotal));

            // ✅ VARIÁVEL QUE ESTAVA FALTANDO
            var horaInicioStr = horaInicio.ToString(@"hh\:mm");

            var consulta = new Consulta
            {
                Data = DataHelperBR.ParaDataAgenda(datePicker.Date),

                // 🔹 Campo legado (compatibilidade)
                Hora = horaInicioStr,

                // 🔹 Campos novos
                HoraInicio = horaInicioStr,
                HoraFim = horaFim.ToString(@"hh\:mm"),
                Duracao = duracaoTotal,

                Medico = _medicoNome,
                MedicoId = _medicoId,
                Servico = servicos,
                Servicos = ObterServicosIdsSelecionados(),
                CriadoEm = DateTime.UtcNow,
                Usuario = SessaoUsuario.UsuarioLogado?.UserId,
                Status = StatusConsulta.Agendada,
                Observacoes = txtObservacoes.Text,
                ValorTotal = CalcularValorServicos(),
                FormaPagamento = _pagamentoSelecionado.Key
            };

            var json = JsonSerializer.Serialize(consulta);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(FirebaseUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Erro", "Erro ao salvar agendamento.", "OK");
                return;
            }

            await DisplayAlert("Sucesso", "Consulta agendada com sucesso!", "OK");

            if (consulta.FormaPagamento == "pix")
            {
                await Shell.Current.GoToAsync(
                    nameof(PagamentoPixPage),
                    true,
                    new Dictionary<string, object>
                    {
                { "Consulta", consulta }
                    });
            }
            else
            {
                await Shell.Current.GoToAsync("/MainPage");
            }
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

            datePicker.Date = DateTime.ParseExact(
                _consultaEdicao.Data,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture
            );

            txtObservacoes.Text = _consultaEdicao.Observacoes;

            // Médico
            _medicoNome = _consultaEdicao.Medico;

            // Serviços
            var servicosSelecionados = _consultaEdicao.Servico.Split(" + ");

            _medicoId = _consultaEdicao.MedicoId;
            await LiberarServicosDoProfissionalAsync(_medicoId);

            // depois disso
            foreach (var servico in _servicos)
            {
                servico.Selecionado =
                    servicosSelecionados.Contains(servico.Nome) && servico.Habilitado;
            }


            AtualizarValorTotal();
            AtualizarTempoTotal();

            await CarregarPagamentosDoProfissionalAsync();

            _pagamentoSelecionado = _pagamentos
                .FirstOrDefault(p => p.Key == _consultaEdicao.FormaPagamento);

            if (_pagamentoSelecionado != null)
            {
                pagamentosCollection.SelectedItem = _pagamentoSelecionado;
            }


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

            if (EmpresaContext.Empresa != null)
                Title = EmpresaContext.Empresa.NomeEmpresa;

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
                    item.Value.Habilitado = false; // 🔒 começa bloqueado
                    item.Value.Selecionado = false;

                    _servicos.Add(item.Value);
                }

                servicosCollection.ItemsSource = _servicos;

                AtualizarValorTotal();
                AtualizarTempoTotal();
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

        private int CalcularDuracaoTotal()
        {
            return _servicos
                .Where(s => s.Selecionado)
                .Sum(s => s.Duracao);
        }

        private async Task CarregarPagamentosDoProfissionalAsync()
        {
            pagamentosCollection.ItemsSource = null;
            _pagamentos.Clear();
            _pagamentoSelecionado = null;

            if (string.IsNullOrEmpty(_medicoId))
                return;

            var url = $"{FirebasePagamentosUrl}/{_medicoId}.json";
            var json = await _httpClient.GetStringAsync(url);

            if (string.IsNullOrWhiteSpace(json) || json == "null")
                return;

            var dados = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            foreach (var item in dados)
            {
                if (!item.Value.TryGetProperty("ativo", out var ativo) || !ativo.GetBoolean())
                    continue;

                _pagamentos.Add(new MetodoPagamentoItem
                {
                    Key = item.Key,
                    Nome = FormatarNomePagamento(item.Key)
                });
            }

            pagamentosCollection.ItemsSource = _pagamentos;
        }


        private string FormatarNomePagamento(string key) => key switch
        {
            "pix" => "PIX",
            "cartaoCredito" => "Cartão de Crédito",
            "cartaoDebito" => "Cartão de Débito",
            "dinheiro" => "Dinheiro",
            "boleto" => "Boleto",
            "nfc" => "Pagamento por Aproximação",
            "carteirasDigitais" => "Carteiras Digitais",
            "planosPacotes" => "Planos / Pacotes",
            _ => key
        };



        private int CalcularQuantidadeDeSlots(int duracaoTotal, int intervaloSlot)
        {
            return (int)Math.Ceiling((double)duracaoTotal / intervaloSlot);
        }


        private void AtualizarValorTotal()
        {
            decimal total = CalcularValorServicos();
            lblValorTotal.Text = $"R$ {total:0.00}";
        }

        private void AtualizarTempoTotal()
        {
            int duracaoTotal = CalcularDuracaoTotal();
            lblTempoTotal.Text = FormatarDuracao(duracaoTotal);
        }


        private void OnServicoChanged(object sender, CheckedChangedEventArgs e)
        {

            var checkbox = sender as CheckBox;
            if (checkbox?.BindingContext is Servico servico)
            {
                if (!servico.Habilitado)
                {
                    servico.Selecionado = false;
                    return;
                }
            }

            // 🔹 Sempre limpar horário e pagamento ao alterar serviços (muda a duração total)
            timePicker.SelectedItem = null;
            timePicker.ItemsSource = null;
            timePicker.Title = "Escolha um horário";

            _pagamentoSelecionado = null;
            pagamentosCollection.SelectedItem = null;

            // 🔹 Atualiza valor e tempo
            AtualizarValorTotal();
            AtualizarTempoTotal();

            // 🔹 Só recalcula horários se médico já estiver selecionado
            if (!string.IsNullOrEmpty(_medicoNome))
            {
                AtualizarHorariosDisponiveis();
            }
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

            // 🔥 APLICAÇÃO DA REGRA: Limpa tudo para forçar nova seleção
            ResetarSelecoesIntermediarias(resetarServicos: true);

            // Reativa o fluxo
            timePicker.IsEnabled = true;
            _ = LiberarServicosDoProfissionalAsync(_medicoId);
            _ = CarregarPagamentosDoProfissionalAsync();

            // 🔥 Reset geral
            foreach (var s in _servicos)
            {
                s.Selecionado = false;
                s.Habilitado = false;
            }

            _ = LiberarServicosDoProfissionalAsync(_medicoId);


            // 🔥 Limpa pagamento ao trocar profissional
            _pagamentoSelecionado = null;
            _pagamentos.Clear();
            pagamentosCollection.ItemsSource = null;

            // 🔹 Recarrega formas de pagamento
            _ = CarregarPagamentosDoProfissionalAsync();

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

            if (_pagamentoSelecionado == null)
            {
                await DisplayAlert("Aviso", "Selecione uma forma de pagamento.", "OK");
                return;
            }


            if (_modoReagendamento)
                await SalvarReagendamentoAsync(servicos);
            else
                await SalvarNovoAgendamentoAsync(servicos);
        }

        private async Task LiberarServicosDoProfissionalAsync(string profissionalId)
        {
            var json = await _httpClient.GetStringAsync(FirebaseProfissionaisServicosUrl);

            if (string.IsNullOrWhiteSpace(json) || json == "null")
                return;

            var dict = JsonSerializer.Deserialize<
                Dictionary<string, ProfissionalServico>>(json);

            var vinculo = dict?
                .Values
                .FirstOrDefault(v => v.ProfissionalId == profissionalId);

            if (vinculo?.ServicosIds == null)
                return;

            foreach (var servico in _servicos)
            {
                servico.Habilitado = vinculo.ServicosIds.Contains(servico.ServicoId);
                servico.Selecionado = false;
            }

            AtualizarValorTotal();
            AtualizarTempoTotal();
        }



        private async Task SalvarReagendamentoAsync(string servicos)
        {
            try
            {
                // 🔹 Hora selecionada
                var horaInicio = TimeSpan.Parse(timePicker.SelectedItem.ToString());

                // 🔹 Duração total dos serviços
                var duracaoTotal = CalcularDuracaoTotal();

                // 🔹 Hora final calculada
                var horaFim = horaInicio.Add(TimeSpan.FromMinutes(duracaoTotal));

                var update = new
                {
                    // 📅 Data do atendimento
                    data = DataHelperBR.ParaDataAgenda(datePicker.Date),

                    // 🔹 Campo legado (compatibilidade)
                    hora = horaInicio.ToString(@"hh\:mm"),

                    // 🔹 Campos novos (padrão correto)
                    horaInicio = horaInicio.ToString(@"hh\:mm"),
                    horaFim = horaFim.ToString(@"hh\:mm"),
                    duracao = duracaoTotal,

                    medico = _medicoNome,
                    medicoId = _medicoId,

                    servico = servicos,
                    servicos = ObterServicosIdsSelecionados(),

                    observacoes = txtObservacoes.Text,
                    valorTotal = CalcularValorServicos(),

                    status = StatusConsulta.Reagendada,
                    formaPagamento = _pagamentoSelecionado.Key
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
                await Shell.Current.GoToAsync("/ConsultasAgendaPage");
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

        private string FormatarDuracao(int minutos)
        {
            if (minutos <= 0)
                return "0 min";

            int horas = minutos / 60;
            int restoMin = minutos % 60;

            if (horas > 0 && restoMin > 0)
                return $"{horas} hora{(horas > 1 ? "s" : "")} e {restoMin} min";

            if (horas > 0)
                return $"{horas} hora{(horas > 1 ? "s" : "")}";

            return $"{restoMin} min";
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

                // 🔥 BLOQUEIA TODOS OS SLOTS DO INTERVALO
                var horariosOcupados = new List<TimeSpan>();

                if (consultasDict != null)
                {
                    foreach (var consulta in consultasDict.Values.Where(c =>
                    {
                        if (string.IsNullOrEmpty(c.Data))
                            return false;

                        var dataConsulta = DateTime.ParseExact(
                            c.Data,
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture
                        );

                        return
                            c.Medico == _medicoNome &&
                            dataConsulta.Date == dataSelecionada &&
                            c.Status != StatusConsulta.CanceladaCliente &&
                            c.Status != StatusConsulta.CanceladaEmpresa;
                    }))

                    {
                        var inicio = !string.IsNullOrEmpty(consulta.HoraInicio)
                            ? TimeSpan.Parse(consulta.HoraInicio)
                            : TimeSpan.Parse(consulta.Hora);

                        var fim = !string.IsNullOrEmpty(consulta.HoraFim)
                            ? TimeSpan.Parse(consulta.HoraFim)
                            : inicio.Add(TimeSpan.FromMinutes(30)); // fallback seguro

                        for (var t = inicio; t < fim; t = t.Add(
                                 TimeSpan.FromMinutes(calendario.Horarios[diaSemana].IntervaloDivisao)))
                        {
                            horariosOcupados.Add(t);
                        }
                    }
                }

                // 🔹 DURAÇÃO TOTAL DOS SERVIÇOS
                int duracaoTotal = _servicos
                    .Where(s => s.Selecionado)
                    .Sum(s => s.Duracao);

                if (duracaoTotal == 0)
                {
                    timePicker.ItemsSource = null;
                    timePicker.Title = "Selecione os serviços";
                    return;
                }

                // 🔹 Intervalo do slot
                int intervaloSlot = calendario.Horarios[diaSemana].IntervaloDivisao;

                // 🔹 Quantidade de slots necessários
                int slotsNecessarios = (int)Math.Ceiling(
                    (double)duracaoTotal / intervaloSlot);

                // 🔹 Slots livres reais
                var slotsLivres = slotsDoDia
                    .Select(h => TimeSpan.Parse(h))
                    .Where(h => !horariosOcupados.Contains(h)) // ✅ CORRETO
                    .OrderBy(h => h)
                    .ToList();


                // 🔹 Horários válidos (com continuidade)
                var horariosValidos = new List<string>();

                for (int i = 0; i <= slotsLivres.Count - slotsNecessarios; i++)
                {
                    bool sequenciaValida = true;

                    for (int j = 0; j < slotsNecessarios - 1; j++)
                    {
                        if (slotsLivres[i + j + 1] - slotsLivres[i + j]
                            != TimeSpan.FromMinutes(intervaloSlot))
                        {
                            sequenciaValida = false;
                            break;
                        }
                    }

                    if (sequenciaValida)
                        horariosValidos.Add(slotsLivres[i].ToString(@"hh\:mm"));
                }

                // 🔹 Atualiza picker
                timePicker.ItemsSource = horariosValidos;
                timePicker.SelectedItem = null;
                timePicker.Title = horariosValidos.Any()
                    ? "Escolha um horário"
                    : "Sem horário contínuo disponível";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro",
                    "Erro ao carregar horários: " + ex.Message,
                    "OK");
            }
        }

        private void ResetarSelecoesIntermediarias(bool resetarServicos = true)
        {
            // 1. Resetar Serviços
            if (resetarServicos)
            {
                foreach (var s in _servicos)
                {
                    s.Selecionado = false;
                    // Se for troca de médico, os serviços também devem ser desabilitados
                    s.Habilitado = false;
                }
            }

            // 2. Resetar Horário
            timePicker.ItemsSource = null;
            timePicker.SelectedItem = null;
            timePicker.Title = "Escolha um horário";

            // 3. Resetar Pagamento
            _pagamentoSelecionado = null;
            pagamentosCollection.SelectedItem = null;
            _pagamentos.Clear();
            pagamentosCollection.ItemsSource = null;

            // 4. Atualizar UI de Totais
            AtualizarValorTotal();
            AtualizarTempoTotal();
        }


        private async void OnDataSelecionada(object sender, DateChangedEventArgs e)
        {
            // 🔥 Sempre limpar o horário ao trocar a data
            timePicker.SelectedItem = null;
            timePicker.ItemsSource = null;
            timePicker.Title = "Escolha um horário";

            // 🔥 Limpa seleções intermediárias
            ResetarSelecoesIntermediarias(resetarServicos: true);

            await Task.Delay(100);
            AtualizarHorariosDisponiveis();
        }

        private List<string> ObterServicosIdsSelecionados()
        {
            return _servicos
                .Where(s => s.Selecionado)
                .Select(s => s.ServicoId)
                .ToList();
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