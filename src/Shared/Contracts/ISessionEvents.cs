namespace Shared.Contracts;

/// <summary>Lets the gateway tell the UI that the user has to sign in again.</summary>
public interface ISessionEvents
{
    event EventHandler? SignInRequired;

    void NotifySignInRequired();
}
