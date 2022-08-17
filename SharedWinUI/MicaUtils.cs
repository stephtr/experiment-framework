﻿using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using WinRT;

namespace ExperimentFramework;

public class MicaUtils
{
    WindowsSystemDispatcherQueueHelper wsdqHelper = new(); // See separate sample below for implementation
    MicaController? micaController;
    SystemBackdropConfiguration? configurationSource;

    public bool EnableMica(Window window)
    {
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
            return true; // succeeded
        }

        return false; // Mica is not supported on this system
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
