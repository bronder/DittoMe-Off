using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace DittoMeOff.Services;

/// <summary>
/// Handles pasting clipboard content to a target window using Win32 APIs.
/// Extracted from MainWindow.xaml.cs to follow single-responsibility principle.
/// </summary>
public class PasteService
{
    private const int SW_RESTORE = 9;
    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;

    private const uint INPUT_KEYBOARD = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    /// <summary>
    /// Asynchronously pastes clipboard content to the target window using SendInput.
    /// Uses Task.Delay instead of Thread.Sleep to avoid blocking the UI thread.
    /// </summary>
    public async Task PasteToWindowAsync(IntPtr targetWindow)
    {
        if (targetWindow == IntPtr.Zero)
        {
            System.Diagnostics.Debug.WriteLine("PasteToWindowAsync: targetWindow is Zero, skipping paste.");
            return;
        }

        try
        {
            // Delay to ensure clipboard is updated and our window is fully hidden
            await Task.Delay(150);

            // Validate the window still exists before attempting paste
            if (!NativeMethods.IsWindow(targetWindow))
            {
                System.Diagnostics.Debug.WriteLine($"PasteToWindowAsync: target window {targetWindow} no longer exists.");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"PasteToWindowAsync: attempting paste to window {targetWindow}");

            // Attach our thread input to the target window's thread so SetForegroundWindow works reliably
            uint foregroundThreadId = NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), out _);
            uint targetThreadId = NativeMethods.GetWindowThreadProcessId(targetWindow, out _);
            uint currentThreadId = NativeMethods.GetCurrentThreadId();

            bool attached = false;
            if (foregroundThreadId != targetThreadId)
            {
                // Detach from current foreground thread if needed
                if (foregroundThreadId != currentThreadId)
                {
                    NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, false);
                }
                // Attach to target window's thread
                attached = NativeMethods.AttachThreadInput(currentThreadId, targetThreadId, true);
                System.Diagnostics.Debug.WriteLine($"PasteToWindowAsync: AttachThreadInput result = {attached}");
            }

            try
            {
                // Restore the target window if it was minimized
                NativeMethods.ShowWindow(targetWindow, SW_RESTORE);

                // Bring to foreground
                bool setForeground = NativeMethods.SetForegroundWindow(targetWindow);
                System.Diagnostics.Debug.WriteLine($"PasteToWindowAsync: SetForegroundWindow result = {setForeground}");

                // Brief pause to let the target window process the focus change
                await Task.Delay(100);

                // Verify we successfully set the foreground window
                var currentForeground = NativeMethods.GetForegroundWindow();
                System.Diagnostics.Debug.WriteLine($"PasteToWindowAsync: current foreground = {currentForeground}, target = {targetWindow}");

                if (currentForeground == targetWindow)
                {
                    // Send Ctrl+V using keybd_event (more reliable for some apps than SendInput)
                    keybd_event((byte)VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    keybd_event((byte)VK_V, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    keybd_event((byte)VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    keybd_event((byte)VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    System.Diagnostics.Debug.WriteLine("PasteToWindowAsync: Ctrl+V sent successfully.");
                }
                else
                {
                    // Fallback: try SendInput approach
                    System.Diagnostics.Debug.WriteLine("PasteToWindowAsync: SetForegroundWindow didn't work, trying SendInput fallback.");
                    SendKeyCombo(VK_CONTROL, VK_V);
                }
            }
            finally
            {
                // Always detach thread input
                if (attached)
                {
                    NativeMethods.AttachThreadInput(currentThreadId, targetThreadId, false);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PasteToWindowAsync failed: {ex.Message}");
        }
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    /// <summary>
    /// Sends a key combination (modifier + key) using SendInput.
    /// </summary>
    private static void SendKeyCombo(ushort modifierVk, ushort keyVk)
    {
        INPUT[] inputs = new INPUT[4];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].ki = new KEYBDINPUT { wVk = modifierVk };
        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].ki = new KEYBDINPUT { wVk = keyVk };
        inputs[2].type = INPUT_KEYBOARD;
        inputs[2].ki = new KEYBDINPUT { wVk = keyVk, dwFlags = KEYEVENTF_KEYUP };
        inputs[3].type = INPUT_KEYBOARD;
        inputs[3].ki = new KEYBDINPUT { wVk = modifierVk, dwFlags = KEYEVENTF_KEYUP };
        SendInput(4, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}
