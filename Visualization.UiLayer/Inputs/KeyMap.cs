using OpenTK.Windowing.GraphicsLibraryFramework;
using Visualization.Display.Inputs;

namespace Visualization.UiLayer.Inputs;

internal static class KeyMap
{
    internal static readonly Dictionary<InputKey, Keys> OpenTkKeysMap = new()
    {
        { InputKey.Escape, Keys.Escape },
        { InputKey.D1, Keys.D1 },
        { InputKey.D2, Keys.D2 },
        { InputKey.D3, Keys.D3 },
        { InputKey.D4, Keys.D4 },
        { InputKey.D5, Keys.D5 },
        { InputKey.D6, Keys.D6 },
        { InputKey.D7, Keys.D7 },
        { InputKey.D8, Keys.D8 },
        { InputKey.D9, Keys.D9 },
        { InputKey.D0, Keys.D0 },
        { InputKey.Minus, Keys.Minus },
        { InputKey.Equal, Keys.Equal },

        { InputKey.Tab, Keys.Tab },
        { InputKey.Q, Keys.Q },
        { InputKey.W, Keys.W },
        { InputKey.E, Keys.E },
        { InputKey.R, Keys.R },
        { InputKey.T, Keys.T },
        { InputKey.Y, Keys.Y },
        { InputKey.U, Keys.U },
        { InputKey.I, Keys.I },
        { InputKey.O, Keys.O },
        { InputKey.P, Keys.P },
        { InputKey.LeftBracket, Keys.LeftBracket },
        { InputKey.RightBracket, Keys.RightBracket },

        { InputKey.CapsLock, Keys.CapsLock },
        { InputKey.A, Keys.A },
        { InputKey.S, Keys.S },
        { InputKey.D, Keys.D },
        { InputKey.F, Keys.F },
        { InputKey.G, Keys.G },
        { InputKey.H, Keys.H },
        { InputKey.J, Keys.J },
        { InputKey.K, Keys.K },
        { InputKey.L, Keys.L },
        { InputKey.Semicolon, Keys.Semicolon },
        { InputKey.Apostrophe, Keys.Apostrophe },

        { InputKey.LeftShift, Keys.LeftShift },
        { InputKey.Z, Keys.Z },
        { InputKey.X, Keys.X },
        { InputKey.C, Keys.C },
        { InputKey.V, Keys.V },
        { InputKey.B, Keys.B },
        { InputKey.N, Keys.N },
        { InputKey.M, Keys.M },
        { InputKey.Comma, Keys.Comma },
        { InputKey.Period, Keys.Period },
        { InputKey.Slash, Keys.Slash },

        { InputKey.LeftCtrl, Keys.LeftControl },
        { InputKey.LeftSuper, Keys.LeftSuper },
        { InputKey.LeftAlt, Keys.LeftAlt },
        { InputKey.Space, Keys.Space },
        { InputKey.RightAlt, Keys.RightAlt },
        { InputKey.RightSuper, Keys.RightSuper },
        { InputKey.Left, Keys.Left },
        { InputKey.Right, Keys.Right },
        { InputKey.Up, Keys.Up },
        { InputKey.Down, Keys.Down }
    };
}