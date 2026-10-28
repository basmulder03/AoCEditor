using AoCEditor.Core.Workspace.Domain;
using Shared;
using Shared.Contracts;

namespace AoCEditor.Core.Workspace.Application;

public enum SetRootResult { Ok, NotAbsolute, OverlapsAppData }
