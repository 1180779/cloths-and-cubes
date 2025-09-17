using Visualization.UiLayer.Applications;
using Visualization.UiLayer.Applications.Demos;

namespace Visualization.UiLayer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (Application application = new BoxesFallingDemo())
            {
                application.Run();
                application.Focus();
            }
        }
    }
}