using Clinica.View;
using Clinica.Services;
using Clinica.Models;

namespace Clinica
{
    public partial class MainPage : ContentPage
    {
        // ?? DECLARAÇÃO DO SERVICE
        private readonly FirebaseConfigService _configService;
        public MainPage()
        {
            InitializeComponent();
            // ?? INICIALIZAÇÃO
            _configService = new FirebaseConfigService();
        }

        private async void OnAgendarConsultaTapped(object sender, TappedEventArgs e)
        {
            // Navega para a página AgendarConsultaPage
            await Shell.Current.GoToAsync(nameof(View.AgendarConsultaPage));
        }

        private async void OnConsultasAgendadasTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(View.ConsultasAgendaPage));
        }

        private async void OnHistoricoConsultasAgendadasPage(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(View.HistoricoConsultasAgendadasPage));
        }

        private async void OnConfigTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(PerfilPage));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CarregarEnderecoEmpresa();
        }

        private async Task CarregarEnderecoEmpresa()
        {
            try
            {
                var empresa = await _configService.ObterEmpresaAsync();

                if (empresa == null)
                {
                    lblEnderecoEmpresa.Text = "Dados da empresa indisponíveis";
                    return;
                }

                // ============================
                // ?? DADOS JÁ EXISTENTES (NÃO ALTERADOS)
                // ============================
                lblEnderecoEmpresa.Text = empresa.Endereco ?? "Endereço não informado";

                lblTelefoneEmpresa.Text = string.IsNullOrWhiteSpace(empresa.Telefone)
                    ? string.Empty
                    : $" {empresa.Telefone}";

                lblEmailEmpresa.Text = string.IsNullOrWhiteSpace(empresa.Email)
                    ? string.Empty
                    : $" {empresa.Email}";

                // ============================
                // ??? NOVO: IMAGEM DE FUNDO (BASE64)
                // ============================
                if (!string.IsNullOrWhiteSpace(empresa.ImagemFundoMobile))
                {
                    string base64 = empresa.ImagemFundoMobile;

                    // Remove prefixo: data:image/jpeg;base64,
                    if (base64.Contains(","))
                        base64 = base64.Split(',')[1];

                    byte[] imageBytes = Convert.FromBase64String(base64);

                    imgFundo.Source = ImageSource.FromStream(() =>
                        new MemoryStream(imageBytes));
                }
            }
            catch
            {
                // ?? Mantém comportamento atual em caso de erro
                lblEnderecoEmpresa.Text = "Erro ao carregar dados da empresa";
                lblTelefoneEmpresa.Text = string.Empty;
                lblEmailEmpresa.Text = string.Empty;
            }
        }

    }
}