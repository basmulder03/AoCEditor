namespace Plugins.Domain;

/// <summary>A command as an argument list with <c>{placeholders}</c>; it is never run through a shell.</summary>
public sealed record CommandSpec(IReadOnlyList<string> Command, InputMode Input = InputMode.Stdin);
