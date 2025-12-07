using Clinica.Models;
using Microsoft.Maui.Controls.Shapes;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Clinica.View;

[QueryProperty(nameof(Consulta), "Consulta")]
public partial class InfoReagendamentoConsultaPage : ContentPage
{
    private Consulta _consulta;
    private Border _medicoSelecionado;
    private string _medicoNome;
    private readonly HttpClient _httpClient = new();

    private const string FirebaseUrl = "https://clinica-e248d-default-rtdb.firebaseio.com/consultas";

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
        MontarMedicos();
    }

    private void CarregarDados()
    {
        if (_consulta == null)
            return;

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

        // Horário
        timePicker.SelectedItem = _consulta.Hora;
    }

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
            var imagem = medico.Value;
            var nome = medico.Key;

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

            var img = new Image
            {
                Source = imagem,
                Aspect = Aspect.AspectFill,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            border.Content = img;

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
                _medicoNome = nome;
                _medicoSelecionado = border;
                break;
            }
        }
    }


    private void OnMedicoTapped(object sender, EventArgs e)
    {
        var border = sender as Border;
        if (border == null) return;

        if (_medicoSelecionado != null)
        {
            _medicoSelecionado.Stroke = Colors.Transparent;
            _medicoSelecionado.BackgroundColor = Color.FromArgb("#D0E8FF");
        }

        border.Stroke = Colors.Blue;
        border.BackgroundColor = Color.FromArgb("#ADD8FF");

        _medicoSelecionado = border;
        _medicoNome =
            ((TapGestureRecognizer)border.GestureRecognizers[0]).CommandParameter.ToString();
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

    private async void OnSalvarReagendamentoClicked(object sender, EventArgs e)
    {
        if (_consulta == null)
        {
            await DisplayAlert("Erro", "Consulta inválida.", "OK");
            return;
        }

        var servicos = ObterServicos();

        var atualizacao = new
        {
            data = datePicker.Date,
            hora = timePicker.SelectedItem?.ToString(),
            medico = _medicoNome,
            servico = servicos,
            observacoes = txtObservacoes.Text,
            status = (int)StatusConsulta.Reagendada
        };

        string url = $"{FirebaseUrl}/{_consulta.Id}.json";

        var json = JsonSerializer.Serialize(atualizacao);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PatchAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            _consulta.Data = datePicker.Date;
            _consulta.Hora = timePicker.SelectedItem?.ToString();
            _consulta.Medico = _medicoNome;
            _consulta.Servico = servicos;
            _consulta.Observacoes = txtObservacoes.Text;
            _consulta.Status = StatusConsulta.Reagendada;

            await DisplayAlert("Sucesso", "Consulta reagendada!", "OK");

            await Shell.Current.GoToAsync(
                nameof(InfoConsultasAgendadaPage),
                true,
                new Dictionary<string, object> { { "Consulta", _consulta } }
            );
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
            await Shell.Current.GoToAsync(nameof(InfoConsultasAgendadaPage), true, new Dictionary<string, object>
        {
            { "Consulta", _consulta }
        });
        });

        return true; // bloqueia o back padrão
    }
}
