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
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = new Window(new LoadingPage()); // <-- SEMPRE DEFINE UMA PAGE

            _ = InitializeApp(window); // roda async depois

            return window;
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
