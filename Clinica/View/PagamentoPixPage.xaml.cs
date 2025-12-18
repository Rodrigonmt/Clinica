using Clinica.Models;
using System.Net.Http.Json;

namespace Clinica.View;

[QueryProperty(nameof(Consulta), "Consulta")]
public partial class PagamentoPixPage : ContentPage
{
    private readonly HttpClient _httpClient = new();
    private const string FirebaseUrl = "https://clinica-e248d-default-rtdb.firebaseio.com";

    public Consulta Consulta { get; set; }

    public PagamentoPixPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (Consulta == null || string.IsNullOrEmpty(Consulta.MedicoId))
            return;

        // ?? Resumo da consulta
        lblServico.Text = $"Serviço: {Consulta.Servico}";
        lblValor.Text = $"Valor: R$ {Consulta.ValorTotal:0.00}";
        lblDataHora.Text = $"{Consulta.Data:dd/MM/yyyy} às {Consulta.HoraInicio}";

        await CarregarPixAsync();
    }

    private async Task CarregarPixAsync()
    {
        try
        {
            string url = $"{FirebaseUrl}/pagamento/{Consulta.MedicoId}/pix.json";

            var pix = await _httpClient.GetFromJsonAsync<PixConfig>(url);

            if (pix == null || !pix.Ativo)
            {
                await DisplayAlert("PIX", "PIX indisponível para este profissional.", "OK");
                return;
            }

            lblDestinatario.Text = pix.Destinatario;
            lblInstituicaoFinanceira.Text = pix.InstituicaoFinanceira;

            // ?? CONTROLE POR TIPO
            if (pix.Tipo == "SIMPLES")
            {
                layoutPixSimples.IsVisible = true;
                layoutPixAutomatizado.IsVisible = false;

                lblChavePix.Text = pix.ChaveMascara;
            }
            else if (pix.Tipo == "AUTOMATIZADO")
            {
                layoutPixSimples.IsVisible = false;
                layoutPixAutomatizado.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Erro ao carregar PIX: {ex.Message}", "OK");
        }
    }


    private async void OnCopiarPixClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(lblChavePix.Text))
        {
            await Clipboard.SetTextAsync(lblChavePix.Text);
            await DisplayAlert("PIX", "Chave PIX copiada!", "OK");
        }
    }

    private async void OnPagarAgoraClicked(object sender, EventArgs e)
    {
        await DisplayAlert(
            "Pagamento PIX",
            "Pagamento automático será integrado em breve.",
            "OK"
        );
    }


    private async void OnPagarDepoisClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("/MainPage");
    }
}