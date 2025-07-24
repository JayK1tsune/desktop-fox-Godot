using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Godot;
using System.Security.Cryptography.X509Certificates;

public partial class WindowClick : Node
{

    public static WindowClick Instance;

    // SetWindowLong() modifies a specific flag value associated with a window.
    // We pass the window handle, the index of the property, and the flags the property will have
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    // This is the index of the property we want to modify
    private const int GwlExStyle = -20;

    // The flags we want to set
    private const int WsExLayered = 0x80000;         // Makes the window "layered"
    private const int WsExTransparent = 0x20;       // Makes the window "clickable through"
    // check https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles 

    // This is the variable containing the window handle
    private IntPtr _hWnd;

    public override void _Ready()
    {
        Instance = this;
        // We store the window handle
        _hWnd = (IntPtr)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, GetWindow().GetWindowId());

        // We can set the properties already from here
        SetWindowLong(_hWnd, GwlExStyle, WsExLayered);

        SetClickThrough(true);
    }

    // This function sets the property of being clickable or not, we will call this function from the mouse detection 
    public void SetClickThrough(bool clickthrough)
    {
        if (clickthrough)
        {
            // We set the window as layered and click-through
            SetWindowLong(_hWnd, GwlExStyle, WsExLayered | WsExTransparent);
        }
        else
        {
            // We only set the window as layered, so it will be clickable
            SetWindowLong(_hWnd, GwlExStyle, WsExLayered);
        }
    }
    

    private const uint SPI_GETWORKAREA = 0x0030;

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    private IntPtr _previousWindowHandle = IntPtr.Zero;

    private IntPtr GetFoxWindowHandle()
    {
        return Process.GetCurrentProcess().MainWindowHandle;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out RECT pvParam, uint fWinIni);

    // ────── User Input ──────

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    /* What is a layered window? 
     * In the Windows API, a layered window is a special type of window that offers several
     * advantages over standard windows:
     * 
     * Transparency: Layered windows can be partially transparent, allowing the content of underlying windows
     * to show through. This can be achieved using either color keying, where a specific color in the window
     * is transparent, or alpha blending, where the window's opacity is specified for each pixel.
     *
     * Complex Shapes: Layered windows can have complex shapes that are not limited by rectangular regions.
     * This is achieved by defining a custom region, allowing for more visually appealing or functional window designs.
     *
     * Animation: Layered windows can be animated smoothly without the visual artifacts
     * that can occur with standard windows due to region updates. This is because the system automatically manages
     * the composition of layered windows with underlying elements.
     */
}