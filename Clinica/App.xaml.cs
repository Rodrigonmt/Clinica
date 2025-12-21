using Clinica.Services;
using Clinica.Models;
using Clinica.View;

namespace Clinica
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            // Sempre inicia no AppShell
            MainPage = new AppShell();
        }

        protected override async void OnStart()
        {
            await EmpresaBootstrapper.CarregarEmpresaAsync();
            await TryAutoLogin();
        }

        private async Task TryAutoLogin()
        {
            try
            {
                // Lê "lembrar senha"
                var lembrar = await SecureStorage.GetAsync("lembrar");

                //        // Se não lembrar → vai para login
                if (lembrar != "true")
                {
                    await Shell.Current.GoToAsync(nameof(LoginPage));
                    return;
                }

                var refreshToken = await SecureStorage.GetAsync("refresh_token");
                if (string.IsNullOrEmpty(refreshToken))
                    return; // nada a fazer

                var authService = new FirebaseAuthService();
                var refreshResp = await authService.RefreshTokenAsync(refreshToken);

                if (refreshResp == null)
                {
                    // refresh falhou → limpar storage e pedir login
                    SecureStorage.Remove("refresh_token");
                    SecureStorage.Remove("auth_token");
                    SecureStorage.Remove("user_id");
                    return;
                }

                // Salvar novo idToken e refreshToken
                await SecureStorage.SetAsync("auth_token", refreshResp.id_token);
                await SecureStorage.SetAsync("refresh_token", refreshResp.refresh_token);
                await SecureStorage.SetAsync("user_id", refreshResp.user_id ?? "");

                // Popular sessao em memória
                SessaoUsuario.UsuarioLogado = new Usuario
                {
                    UserId = refreshResp.user_id,
                    Email = null // email não vem aqui; se precisar, carregue do Realtime/Firestore
                };

                // Navegar diretamente para MainPage ou atualizar UI
                await Shell.Current.GoToAsync(nameof(MainPage));
            }
            catch
            {
                // falha silenciosa — não travar app
            }
        }



        private async Task InitializeApp(Window window)
        {
            try
            {
                // Lê "lembrar senha"
                var lembrar = await SecureStorage.GetAsync("lembrar");

                // Se não lembrar → vai para login
                if (lembrar != "true")
                {
                    window.Page = new AppShell();
                    return;
                }

                // Tenta pegar refresh token
                var refresh = await SecureStorage.GetAsync("refresh_token");

                if (string.IsNullOrEmpty(refresh))
                {
                    window.Page = new AppShell();
                    return;
                }

                // Tenta logar com o refresh token
                var auth = await FirebaseRefreshLogin(refresh);

                if (auth == null)
                {
                    window.Page = new AppShell();
                    return;
                }

                SessaoUsuario.UsuarioLogado = new Usuario
                {
                    UserId = auth.localId,
                    Email = auth.email
                };

                // Logou automático → vai para MainPage
                window.Page = new MainPage();
            }
            catch
            {
                // fallback
                window.Page = new AppShell();
            }
        }

        private async Task<AuthResponse?> FirebaseRefreshLogin(string refresh)
        {
            var service = new FirebaseAuthService();
            return await service.RefreshLogin(refresh);
        }
    }
}
