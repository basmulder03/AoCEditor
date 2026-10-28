using Shared.Contracts;

namespace Session.Application;

public sealed class SessionEvents : ISessionEvents
{
    public event EventHandler? SignInRequired;

    public void NotifySignInRequired() => SignInRequired?.Invoke(this, EventArgs.Empty);
}
