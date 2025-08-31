using Visualization.Applications;
using Visualization.Applications.Demos;

namespace Visualization
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