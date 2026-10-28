namespace Shared.Contracts;

/// <summary>Bounded, observable list of the latest gateway requests.</summary>
public interface IGatewayRequestHistory
{
    IReadOnlyList<GatewayRequestEntry> Recent { get; }

    event EventHandler? Changed;

    void Record(GatewayRequestEntry entry);
}
