namespace AoC.UI;

public partial class App : Application
{
    public App()
    {
        try {
            InitializeComponent();
        } catch (Exception ex) {
            // Handle initialization exceptions
            Console.WriteLine($"Initialization failed: {ex.Message}");
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "AoC.UI" };
    }
}