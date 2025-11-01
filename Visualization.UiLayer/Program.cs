using Visualization.UiLayer.Applications;
using Visualization.UiLayer.Applications.Demos.Materials;

namespace Visualization.UiLayer
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            using Application application = new PbrMaterialsDemo();
            application.Run();
            application.Focus();
        }
    }
}