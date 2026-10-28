namespace AoCEditor.Core.Session.Application;

public delegate Task<CandidateVerdict> TokenValidator(string candidate, CancellationToken cancellationToken);
