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

            // ?? Mostra imagem em cache imediatamente
            MostrarImagemCache();

            // ?? Carrega dados da empresa
            await CarregarEmpresaAsync();

            // ?? Atualiza imagem real em background
            _ = CarregarImagemFundoAsync(); // fire-and-forget
        }

        private void MostrarImagemCache()
        {
            if (ImagemCacheService.ExisteImagemCache())
            {
                imgFundo.Source = ImagemCacheService.ObterImagemCache();
            }
        }

        private async Task CarregarEmpresaAsync()
        {
            try
            {
                var empresa = await _configService.ObterEmpresaAsync();

                if (empresa == null)
                {
                    lblEnderecoEmpresa.Text = "Dados da empresa indisponíveis";
                    return;
                }

                // ?? Contexto global
                EmpresaContext.SetEmpresa(empresa);

                // ?? Dados visuais
                lblEnderecoEmpresa.Text = empresa.Endereco ?? "Endereço não informado";

                lblTelefoneEmpresa.Text = string.IsNullOrWhiteSpace(empresa.Telefone)
                    ? string.Empty
                    : empresa.Telefone;

                lblEmailEmpresa.Text = string.IsNullOrWhiteSpace(empresa.Email)
                    ? string.Empty
                    : empresa.Email;
            }
            catch
            {
                lblEnderecoEmpresa.Text = "Erro ao carregar dados da empresa";
                lblTelefoneEmpresa.Text = string.Empty;
                lblEmailEmpresa.Text = string.Empty;
            }
        }


        private async Task CarregarImagemFundoAsync()
        {
            try
            {
                var empresa = EmpresaContext.Empresa;

                // fallback: se ainda não existe no contexto
                if (empresa == null)
                    empresa = await _configService.ObterEmpresaAsync();

                if (empresa == null || string.IsNullOrWhiteSpace(empresa.ImagemFundoMobile))
                    return;

                string base64 = empresa.ImagemFundoMobile;

                if (base64.Contains(","))
                    base64 = base64.Split(',')[1];

                byte[] imageBytes = Convert.FromBase64String(base64);

                // ?? Atualiza somente se mudou
                if (!ImagemCacheService.ImagemMudou(imageBytes))
                    return;

                await ImagemCacheService.SalvarImagemAsync(imageBytes);

                // ?? Atualiza UI suavemente
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await imgFundo.FadeTo(0, 120);
                    imgFundo.Source = ImageSource.FromStream(() =>
                        new MemoryStream(imageBytes));
                    await imgFundo.FadeTo(1, 180);
                });
            }
            catch
            {
                // silencioso — imagem não pode quebrar a tela
            }
        }

    }
}