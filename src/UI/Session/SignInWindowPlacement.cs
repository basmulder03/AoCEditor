using Avalonia;

namespace AoCEditor.UI.Session;

public static class SignInWindowPlacement
{
    /// <summary>Centers a window of the desired size on the owner, on the screen the owner mostly occupies, fully inside its working area.</summary>
    public static PixelRect Place(PixelRect owner, IReadOnlyList<PixelRect> workingAreas, PixelSize desired)
    {
        var area = workingAreas
            .OrderByDescending(a => { var i = a.Intersect(owner); return (long)i.Width * i.Height; })
            .FirstOrDefault();
        if (area.Width == 0)
        {
            return new PixelRect(owner.Position, desired);
        }

        var size = new PixelSize(Math.Min(desired.Width, area.Width), Math.Min(desired.Height, area.Height));
        var x = Math.Clamp(owner.X + (owner.Width - size.Width) / 2, area.X, area.Right - size.Width);
        var y = Math.Clamp(owner.Y + (owner.Height - size.Height) / 2, area.Y, area.Bottom - size.Height);
        return new PixelRect(x, y, size.Width, size.Height);
    }
}
