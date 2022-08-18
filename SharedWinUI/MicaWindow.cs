using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace ExperimentFramework;

public class MicaWindow
{
    WindowsSystemDispatcherQueueHelper wsdqHelper = new(); // See separate sample below for implementation
    MicaController? micaController;
    SystemBackdropConfiguration? configurationSource;

    internal MicaWindow(Window window, bool useCustomTitlebar)
    {
        if (useCustomTitlebar)
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            var wndId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(wndId);
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }

        wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

        if (MicaController.IsSupported())
        {
            // Hooking up the policy object
            configurationSource = new SystemBackdropConfiguration();

            // Initial configuration state.
            configurationSource.IsInputActive = true;
            // configurationSource.Theme = SystemBackdropTheme.Default;

            micaController = new MicaController();

            // Enable the system backdrop.
            // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
            micaController.AddSystemBackdropTarget(window.As<ICompositionSupportsSystemBackdrop>());
            micaController.SetSystemBackdropConfiguration(configurationSource);
        }
    }

    public void Activate(WindowActivatedEventArgs args)
    {
        if (configurationSource != null)
        {
            configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }
    }

    public void Close()
    {
        // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
        // use this closed window.
        if (micaController != null)
        {
            micaController.Dispose();
            micaController = null;
        }
        configurationSource = null;
    }
}

public static class MicaWindowExtensions
{
    public static MicaWindow EnableMica(this Window window, bool useCustomTitlebar = false)
    {
        return new MicaWindow(window, useCustomTitlebar);
    }
}

internal class WindowsSystemDispatcherQueueHelper
{
    [StructLayout(LayoutKind.Sequential)]
    struct DispatcherQueueOptions
    {
        internal int dwSize;
        internal int threadType;
        internal int apartmentType;
    }

    [DllImport("CoreMessaging.dll")]
    private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

    object? m_dispatcherQueueController = null;
    public void EnsureWindowsSystemDispatcherQueueController()
    {
        if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
        {
            // one already exists, so we'll just use it.
            return;
        }

        if (m_dispatcherQueueController == null)
        {
            DispatcherQueueOptions options;
            options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
            options.threadType = 2;    // DQTYPE_THREAD_CURRENT
            options.apartmentType = 2; // DQTAT_COM_STA

            CreateDispatcherQueueController(options, ref m_dispatcherQueueController!);
        }
    }
}
