    using Clinica.Models; // Importa o modelo Consulta
    using Microsoft.Maui.Controls;
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Globalization;

    namespace Clinica.View
    {
        public partial class AgendarConsultaPage : ContentPage
        {
            private Border _medicoSelecionado;
            private string _medicoNome;

            private readonly HttpClient _httpClient;
            private const string FirebaseUrl = "https://clinica-e248d-default-rtdb.firebaseio.com/consultas.json";
            // OBS: o .json no final é OBRIGATÓRIO no Firebase Realtime Database

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

                datePicker.MinimumDate = DateTime.Today; // opcional
            }

            // 👉 Evento ao clicar em um médico
            private void OnMedicoTapped(object sender, EventArgs e)
            {
                var borderClicado = sender as Border ?? ((sender as TapGestureRecognizer)?.Parent as Border);
                if (borderClicado == null) return;

                // Reset do médico anterior
                if (_medicoSelecionado != null)
                {
                    _medicoSelecionado.Stroke = Colors.Transparent;
                    _medicoSelecionado.BackgroundColor = Color.FromArgb("#D0E8FF");
                }

                // Destacar médico selecionado
                borderClicado.Stroke = Colors.Blue;
                borderClicado.BackgroundColor = Color.FromArgb("#ADD8FF");
                _medicoSelecionado = borderClicado;

                // Captura o nome do médico pelo CommandParameter
                if (borderClicado.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tapGesture)
                {
                    _medicoNome = tapGesture.CommandParameter?.ToString();
                }

                // 🔥 Sempre limpar horário escolhido ao trocar de médico
                timePicker.SelectedItem = null;

                // Ativa o horário após medico selecionado
                timePicker.IsEnabled = true;

                // Atualizar horários disponíveis
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

                // 👉 Primeiro capturamos os serviços selecionados
                var servicos = ObterServicosSelecionados();

                // 👉 Depois validamos
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

                // 👉 Criar consulta com serviços incluídos
                var consulta = new Consulta
                {
                    Data = datePicker.Date,
                    Hora = timePicker.SelectedItem.ToString(),
                    Medico = _medicoNome,
                    Servico = servicos,    // <-- aqui vai o serviço!
                    CriadoEm = DateTime.UtcNow,
                    Usuario = SessaoUsuario.UsuarioLogado?.UsuarioLogin,
                    Status = StatusConsulta.Agendada,
                    Observacoes = txtObservacoes.Text
                };

                try
                {
                    // Serializa para JSON
                    var json = JsonSerializer.Serialize(consulta);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Envia para o Firebase (POST → cria ID automático)
                    var response = await _httpClient.PostAsync(FirebaseUrl, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Erro", "Não foi possível salvar a consulta.", "OK");
                        return;
                    }

                    await DisplayAlert("Sucesso", "Consulta agendada com sucesso!", "OK");

                    // Voltar para a página principal
                    await Shell.Current.GoToAsync("/MainPage");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Erro", "Falha ao salvar: " + ex.Message, "OK");
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
                    return; // precisa escolher médico primeiro

                try
                {
                    // Buscar todas as consultas do Firebase
                    var response = await _httpClient.GetStringAsync(FirebaseUrl);

                    if (string.IsNullOrWhiteSpace(response) || response == "null")
                    {
                        // Nenhuma consulta cadastrada → todos horários livres
                        timePicker.ItemsSource = _horariosBase;
                        return;
                    }

                    var consultasDict = JsonSerializer.Deserialize<Dictionary<string, Consulta>>(response);

                    var dataSelecionada = datePicker.Date.Date;

                    // Filtrar consultas desse médico e deste dia
                    var horariosOcupados = consultasDict
                        .Where(c => c.Value.Medico == _medicoNome &&
                                    c.Value.Data.Date == dataSelecionada &&
                                    c.Value.Status != StatusConsulta.CanceladaEmpresa &&
                                    c.Value.Status != StatusConsulta.CanceladaCliente)
                        .Select(c => c.Value.Hora)
                        .ToList();

                    // Remover horários já ocupados
                    var horariosDisponiveis = _horariosBase
                        .Where(h => !horariosOcupados.Contains(h))
                        .ToList();

                    // Atualiza a lista no Picker
                    timePicker.ItemsSource = horariosDisponiveis;

                    // Limpa horário selecionado se ele não existe mais
                    if (timePicker.SelectedItem != null &&
                        !horariosDisponiveis.Contains(timePicker.SelectedItem.ToString()))
                    {
                        timePicker.SelectedItem = null;
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Erro", "Não foi possível carregar horários: " + ex.Message, "OK");
                }
            }

            private async void OnDataSelecionada(object sender, DateChangedEventArgs e)
            {
                if (e.NewDate.DayOfWeek == DayOfWeek.Saturday ||
                    e.NewDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    await DisplayAlert("Data inválida",
                        "A clínica não realiza atendimentos aos finais de semana.",
                        "OK");

                    // Volta a data para a última válida
                    datePicker.Date = e.OldDate;

                    return;
                }

                // Atualizar horários disponíveis normalmente
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
                var lista = new List<string>();

                if (chkCabelo.IsChecked) lista.Add("Cabelo");
                if (chkBarba.IsChecked) lista.Add("Barba");
                if (chkSobrancelha.IsChecked) lista.Add("Sobrancelha");
                if (chkColoracao.IsChecked) lista.Add("Coloração de Cabelo");

                return string.Join(" + ", lista);
            }


        }

    }