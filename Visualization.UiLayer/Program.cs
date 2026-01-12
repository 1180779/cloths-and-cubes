using Visualization.UiLayer.Applications.Demos;

namespace Visualization.UiLayer
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            using Application application = new Application();
            application.Run();
            application.Focus();
        }
    }
}