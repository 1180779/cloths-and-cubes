using Visualization.Applications.Demos;
using Visualization.UiLayer.Applications;

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