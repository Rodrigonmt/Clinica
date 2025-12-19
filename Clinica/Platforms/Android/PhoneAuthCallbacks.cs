using Firebase;
using Firebase.Auth;

public class PhoneAuthCallbacks
    : PhoneAuthProvider.OnVerificationStateChangedCallbacks
{
    private readonly Action<string, PhoneAuthProvider.ForceResendingToken> _codeSent;
    private readonly Action<FirebaseException> _verificationFailed;
    private readonly Action<PhoneAuthCredential>? _verificationCompleted;

    public PhoneAuthCallbacks(
        Action<string, PhoneAuthProvider.ForceResendingToken> codeSent,
        Action<FirebaseException> verificationFailed,
        Action<PhoneAuthCredential>? verificationCompleted = null)
    {
        _codeSent = codeSent;
        _verificationFailed = verificationFailed;
        _verificationCompleted = verificationCompleted;
    }

    public override void OnCodeSent(
        string verificationId,
        PhoneAuthProvider.ForceResendingToken token)
    {
        _codeSent?.Invoke(verificationId, token);
    }

    public override void OnVerificationFailed(FirebaseException exception)
    {
        _verificationFailed?.Invoke(exception);
    }

    public override void OnVerificationCompleted(PhoneAuthCredential credential)
    {
        // Auto-verificação (Android pode validar sem SMS)
        _verificationCompleted?.Invoke(credential);
    }
}
