using System.Collections.ObjectModel;
using System.Globalization;
using Runner.Application;
using Shared;

namespace Runner.UI;

public sealed record ConsoleLine(string Text, bool IsError);
