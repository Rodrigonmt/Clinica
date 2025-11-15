using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Clinica.Models; // Importa o modelo Consulta

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

            if (timePicker.SelectedItem == null)
            {
                await DisplayAlert("Aviso", "Selecione um horário.", "OK");
                return;
            }

            // Cria o objeto consulta
            var consulta = new Consulta
            {
                Data = datePicker.Date,
                Hora = timePicker.SelectedItem.ToString(),
                Medico = _medicoNome,
                CriadoEm = DateTime.UtcNow,
                Usuario = SessaoUsuario.UsuarioLogado?.UsuarioLogin,
                Status = StatusConsulta.Agendada // 🔹 Status inicial
            };

            try
            {
                // Serializa para JSON
                var json = JsonSerializer.Serialize(consulta);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Envia para o Firebase
                var response = await _httpClient.PostAsync(FirebaseUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Sucesso", "Consulta agendada com sucesso!", "OK");

                    // Voltar para MainPage
                    await Shell.Current.GoToAsync("/MainPage");
                    // ou, se quiser garantir que sempre vai pra MainPage:
                    // await Shell.Current.GoToAsync($"//{nameof(MainPage)}");

                }
                else
                {
                    await DisplayAlert("Erro", $"Falha ao agendar: {response.StatusCode}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Ocorreu um erro: {ex.Message}", "OK");
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

    }

}