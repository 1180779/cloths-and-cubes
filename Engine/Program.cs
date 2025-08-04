using System.Runtime.InteropServices.Marshalling;
using Engine.Display;
using Engine.Physics;


namespace Engine
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (Window window = new(800, 600, "Display"))
            {
                window.Run();
                window.Focus();
            }
        }
    }
}
