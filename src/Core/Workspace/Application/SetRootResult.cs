using Shared;
using Shared.Contracts;
using Workspace.Domain;

namespace Workspace.Application;

public enum SetRootResult { Ok, NotAbsolute, OverlapsAppData }
