using Clinica.Models;
using Microsoft.Maui.Controls;

namespace Clinica.View;

[QueryProperty(nameof(Consulta), "Consulta")]
public partial class PagamentoPixPage : ContentPage
{
    public Consulta Consulta { get; set; }
    public PagamentoPixPage()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (Consulta == null) return;

        lblServico.Text = $"Serviço: {Consulta.Servico}";
        lblValor.Text = $"Valor: R$ {Consulta.ValorTotal:0.00}";
        lblDataHora.Text = $"{Consulta.Data:dd/MM/yyyy} às {Consulta.HoraInicio}";

        // ?? PIX simples (por enquanto)
        lblChavePix.Text = "pix@clinica.com.br";
    }

    private async void OnCopiarPixClicked(object sender, EventArgs e)
    {
        await Clipboard.SetTextAsync(lblChavePix.Text);
        await DisplayAlert("PIX", "Chave PIX copiada!", "OK");
    }

    private async void OnPagarDepoisClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("/MainPage");
    }
}