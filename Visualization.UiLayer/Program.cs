using Visualization.UiLayer.Applications;
using Visualization.UiLayer.Applications.Demos;

namespace Visualization.UiLayer
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            using Application application = new BoxesRandomConfigurationDemo();
            application.Run();
            application.Focus();
        }
    }
}