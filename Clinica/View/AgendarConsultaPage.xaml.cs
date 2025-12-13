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
                CommandParameter = prof.Nome
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

            if (borderClicado.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tapGesture)
            {
                _medicoNome = tapGesture.CommandParameter?.ToString();
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
                Servico = servicos,
                CriadoEm = DateTime.UtcNow,
                Usuario = SessaoUsuario.UsuarioLogado?.UserId,
                Status = StatusConsulta.Agendada,
                Observacoes = txtObservacoes.Text,
                ValorTotal = CalcularValorServicos()  // ✔ ADICIONADO
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
                return;

            try
            {
                var response = await _httpClient.GetStringAsync(FirebaseUrl);

                if (string.IsNullOrWhiteSpace(response) || response == "null")
                {
                    timePicker.ItemsSource = _horariosBase;
                    timePicker.SelectedItem = null;
                    timePicker.Title = "Escolha um horário";
                    return;
                }

                var consultasDict = JsonSerializer.Deserialize<Dictionary<string, Consulta>>(response);
                var dataSelecionada = datePicker.Date.Date;

                var horariosOcupados = consultasDict
                    .Where(c => c.Value.Medico == _medicoNome &&
                                c.Value.Data.Date == dataSelecionada &&
                                c.Value.Status != StatusConsulta.CanceladaEmpresa &&
                                c.Value.Status != StatusConsulta.CanceladaCliente)
                    .Select(c => c.Value.Hora)
                    .ToList();

                var horariosDisponiveis = _horariosBase
                    .Where(h => !horariosOcupados.Contains(h))
                    .ToList();

                timePicker.ItemsSource = horariosDisponiveis;

                // ❌ NÃO SELECIONAR AUTOMATICAMENTE MESMO QUE EXISTA
                timePicker.SelectedItem = null;
                timePicker.Title = "Escolha um horário";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", "Não foi possível carregar horários: " + ex.Message, "OK");
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