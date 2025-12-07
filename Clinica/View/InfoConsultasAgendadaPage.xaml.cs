using Clinica.Models;

namespace Clinica.View
{
    [QueryProperty(nameof(Consulta), "Consulta")]
    public partial class InfoConsultasAgendadaPage : ContentPage
    {
        private Consulta _consulta;

        public Consulta Consulta
        {
            get => _consulta;
            set
            {
                _consulta = value;
                CarregarDados();
            }
        }

        public InfoConsultasAgendadaPage()
        {
            InitializeComponent();
        }

        private void CarregarDados()
        {
            if (Consulta == null)
                return;

            lblData.Text = Consulta.Data.ToString("dd/MM/yyyy");
            lblHora.Text = Consulta.Hora;
            lblMedico.Text = Consulta.Medico;
            lblStatus.Text = Consulta.Status.ToString();
            lblServico.Text = Consulta.Servico; // <-- ADICIONADO

            lblObservacoes.Text = string.IsNullOrWhiteSpace(Consulta.Observacoes)
                ? "Nenhuma observação registrada."
                : Consulta.Observacoes;
        }



        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Cancelar consulta",
                                              "Tem certeza que deseja cancelar esta consulta?",
                                              "Sim", "Não");

            if (confirm)
            {
                // Sua lógica para cancelar a consulta aqui
                await DisplayAlert("Cancelada", "A consulta foi cancelada.", "OK");
                await Shell.Current.GoToAsync("/ConsultasAgendaPage");
            }
            
        }

        private async void OnReagendarClicked(object sender, EventArgs e)
        {
            // Aqui você navega para sua tela de reagendamento
            await Shell.Current.GoToAsync(nameof(AgendarConsultaPage));
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("/ConsultasAgendaPage");
            });

            return true;
        }

    }
}
