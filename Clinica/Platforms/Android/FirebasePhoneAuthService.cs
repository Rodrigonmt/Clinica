using Android.App;
using Clinica.Platforms.Android;
using Firebase.Auth;
using Java.Util.Concurrent;

[assembly: Dependency(typeof(FirebasePhoneAuthService))]
namespace Clinica.Platforms.Android;

public class FirebasePhoneAuthService : Java.Lang.Object, IFirebasePhoneAuthService
{
    private FirebaseAuth _auth;
    private string _verificationId;

    public FirebasePhoneAuthService()
    {
        _auth = FirebaseAuth.Instance;
    }

    public Task<string> EnviarSmsAsync(string telefone)
    {
        var tcs = new TaskCompletionSource<string>();

        PhoneAuthProvider.Instance.VerifyPhoneNumber(
            telefone,
            60,
            TimeUnit.Seconds,
            Platform.CurrentActivity,
            new PhoneAuthCallbacks(
                codeSent: (verificationId, token) =>
                {
                    _verificationId = verificationId;
                    tcs.SetResult(verificationId);
                },
                verificationFailed: ex =>
                {
                    tcs.SetException(new Exception(ex.Message));
                }
            )
        );

        return tcs.Task;
    }

    public async Task<AuthResponse?> ConfirmarCodigoAsync(string verificationId, string codigo)
    {
        var credential = PhoneAuthProvider.GetCredential(verificationId, codigo);
        var result = await _auth.SignInWithCredentialAsync(credential);

        var user = result.User;

        var token = await user.GetIdTokenAsync(false);

        return new AuthResponse
        {
            idToken = token.Token,
            localId = user.Uid,
            email = user.PhoneNumber,
            refreshToken = "" // Firebase SDK gerencia internamente
        };
    }
}
