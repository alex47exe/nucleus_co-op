﻿using Nucleus.Gaming.Coop;
using Nucleus.Gaming.Coop.InputManagement;
using Nucleus.Gaming.Coop.ProtoInput;
using Nucleus.Gaming.Windows;
using Nucleus.Gaming.Windows.Interop;
using Nucleus.Interop.User32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using WindowScrape.Constants;
using WindowScrape.Types;

namespace Nucleus.Gaming.Tools.GlobalWindowMethods
{
    public static class GlobalWindowMethods
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn
        (
           int nLeftRect,     // x-coordinate of upper-left corner
           int nTopRect,      // y-coordinate of upper-left corner
           int nRightRect,    // x-coordinate of lower-right corner
           int nBottomRect,   // y-coordinate of lower-right corner
           int nWidthEllipse, // width of ellipse
           int nHeightEllipse // height of ellipse
        );

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool SetWindowText(IntPtr hWnd, string text);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT Rect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public enum ShowWindowEnum
        {
            Hide = 0,
            ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
            Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
            Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
            Restore = 9, ShowDefault = 10, ForceMinimized = 11
        };

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        private static bool resetingWindows = false;

        public static void ResetWindows(GenericGameHandler genericGameHandler, GenericGameInfo genericGameInfo, ProcessData processData, int x, int y, int w, int h, int i)
        {
            if (processData.HWnd == null)
            {
                profile.PlayersList[i - 1].ProcessData.HWnd = new HwndObject(profile.PlayersList[i - 1].ProcessData.Process.NucleusGetMainWindowHandle());
                processData.HWnd = profile.PlayersList[i - 1].ProcessData.HWnd;
                Thread.Sleep(1000);
            }

            genericGameHandler.Log("Attempting to reposition, resize and strip borders for instance " + (i - 1) + $" - {processData.Process.ProcessName} (pid {processData.Process.Id})");

            try
            {
                if (!genericGameInfo.DontRemoveBorders)
                {
                    uint lStyle = User32Interop.GetWindowLong(processData.HWnd.NativePtr, User32_WS.GWL_STYLE);
                    if (genericGameInfo.WindowStyleValues?.Length > 0)
                    {
                        genericGameHandler.Log("Using user custom window style");
                        foreach (string val in genericGameInfo.WindowStyleValues)
                        {
                            if (val.StartsWith("~"))
                            {
                                lStyle &= ~Convert.ToUInt32(val.Substring(1), 16);
                            }
                            else
                            {
                                lStyle |= Convert.ToUInt32(val, 16);
                            }
                        }
                    }
                    else
                    {
                        lStyle &= ~User32_WS.WS_CAPTION;
                        lStyle &= ~User32_WS.WS_THICKFRAME;
                        lStyle &= ~User32_WS.WS_MINIMIZE;
                        lStyle &= ~User32_WS.WS_MAXIMIZE;
                        lStyle &= ~User32_WS.WS_SYSMENU;

                        lStyle &= ~User32_WS.WS_DLGFRAME;
                        lStyle &= ~User32_WS.WS_BORDER;
                    }
                    int resultCode = User32Interop.SetWindowLong(processData.HWnd.NativePtr, User32_WS.GWL_STYLE, lStyle);

                    lStyle = User32Interop.GetWindowLong(processData.HWnd.NativePtr, User32_WS.GWL_EXSTYLE);

                    if (genericGameInfo.ExtWindowStyleValues?.Length > 0)
                    {
                        genericGameHandler.Log("Using user custom extended window style");
                        foreach (string val in genericGameInfo.ExtWindowStyleValues)
                        {
                            if (val.StartsWith("~"))
                            {
                                lStyle &= ~Convert.ToUInt32(val.Substring(1), 16);
                            }
                            else
                            {
                                lStyle |= Convert.ToUInt32(val, 16);
                            }
                        }
                    }
                    else
                    {
                        lStyle &= ~User32_WS.WS_EX_DLGMODALFRAME;
                        lStyle &= ~User32_WS.WS_EX_CLIENTEDGE;
                        lStyle &= ~User32_WS.WS_EX_STATICEDGE;
                    }


                    int resultCode2 = User32Interop.SetWindowLong(processData.HWnd.NativePtr, User32_WS.GWL_EXSTYLE, lStyle);
                    User32Interop.SetWindowPos(processData.HWnd.NativePtr, IntPtr.Zero, 0, 0, 0, 0, (uint)(PositioningFlags.SWP_FRAMECHANGED | PositioningFlags.SWP_NOMOVE | PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOZORDER | PositioningFlags.SWP_NOOWNERZORDER));

                }

                if (!genericGameInfo.DontReposition)
                {
                    processData.HWnd.Location = new Point(x, y);
                }

                if (!genericGameInfo.DontResize)
                {
                    processData.HWnd.Size = new Size(w, h);
                }
            }
            catch (Exception ex)
            {
                genericGameHandler.Log("ERROR - Exception in ResetWindows for instance " + (i - 1) + ". " + ex.Message);
            }

            try
            {
                if ((processData.HWnd.Location != new Point(x, y) && !genericGameInfo.DontReposition) || (processData.HWnd.Size != new Size(w, h) && !genericGameInfo.DontResize))
                {
                    genericGameHandler.Log("ERROR - ResetWindows was unsuccessful for instance " + (i - 1));
                }
            }
            catch (Exception e)
            {
                genericGameHandler.Log("ERROR - Exception in ResetWindows for instance " + (i - 1) + ", error = " + e);
            }
        }

        public static void ChangeGameWindow(GenericGameHandler genericGameHandler, GenericGameInfo gen, Process proc, List<PlayerInfo> players, int playerIndex)
        {
            var hwnd = WaitForProcWindowHandleNotZero(genericGameHandler, proc);

            Point loc = new Point(players[playerIndex].MonitorBounds.X, players[playerIndex].MonitorBounds.Y);
            Size size = new Size(players[playerIndex].MonitorBounds.Width, players[playerIndex].MonitorBounds.Height);

            if (!gen.DontRemoveBorders)
            {
                genericGameHandler.Log($"Removing game window border for process {proc.ProcessName} (pid {proc.Id})");

                uint lStyle = User32Interop.GetWindowLong(hwnd, User32_WS.GWL_STYLE);
                if (gen.WindowStyleValues?.Length > 0)
                {
                    genericGameHandler.Log("Using user custom window style");
                    foreach (string val in gen.WindowStyleValues)
                    {
                        if (val.StartsWith("~"))
                        {
                            lStyle &= ~Convert.ToUInt32(val.Substring(1), 16);
                        }
                        else
                        {
                            lStyle |= Convert.ToUInt32(val, 16);
                        }
                    }
                }
                else
                {
                    lStyle &= ~User32_WS.WS_CAPTION;
                    lStyle &= ~User32_WS.WS_THICKFRAME;
                    lStyle &= ~User32_WS.WS_MINIMIZE;
                    lStyle &= ~User32_WS.WS_MAXIMIZE;
                    lStyle &= ~User32_WS.WS_SYSMENU;

                    lStyle &= ~User32_WS.WS_DLGFRAME;
                    lStyle &= ~User32_WS.WS_BORDER;
                }
                int resultCode = User32Interop.SetWindowLong(hwnd, User32_WS.GWL_STYLE, lStyle);

                lStyle = User32Interop.GetWindowLong(hwnd, User32_WS.GWL_EXSTYLE);
                if (gen.ExtWindowStyleValues?.Length > 0)
                {
                    genericGameHandler.Log("Using user custom extended window style");
                    foreach (string val in gen.ExtWindowStyleValues)
                    {
                        if (val.StartsWith("~"))
                        {
                            lStyle &= ~Convert.ToUInt32(val.Substring(1), 16);
                        }
                        else
                        {
                            lStyle |= Convert.ToUInt32(val, 16);
                        }
                    }
                }
                else
                {
                    lStyle &= ~User32_WS.WS_EX_DLGMODALFRAME;
                    lStyle &= ~User32_WS.WS_EX_CLIENTEDGE;
                    lStyle &= ~User32_WS.WS_EX_STATICEDGE;
                }

                int resultCode2 = User32Interop.SetWindowLong(hwnd, User32_WS.GWL_EXSTYLE, lStyle);
                User32Interop.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, (uint)(PositioningFlags.SWP_FRAMECHANGED | PositioningFlags.SWP_NOMOVE | PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOZORDER | PositioningFlags.SWP_NOOWNERZORDER));
            }

            if (gen.KeepAspectRatio || gen.KeepMonitorAspectRatio)
            {
                if (gen.KeepMonitorAspectRatio)
                {
                    genericGameHandler.origWidth = genericGameHandler.owner.MonitorBounds.Width;
                    genericGameHandler.origHeight = genericGameHandler.owner.MonitorBounds.Height;
                }
                else
                {
                    if (GetWindowRect(hwnd, out RECT Rect))
                    {
                        genericGameHandler.origWidth = Rect.Right - Rect.Left;
                        genericGameHandler.origHeight = Rect.Bottom - Rect.Top;
                    }
                }

                double newWidth = genericGameHandler.playerBoundsWidth;
                double newHeight = genericGameHandler.playerBoundsHeight;

                if (newWidth < genericGameHandler.origWidth)
                {
                    if (genericGameHandler.origHeight > 0 && genericGameHandler.origWidth > 0)
                    {
                        genericGameHandler.origRatio = (double)genericGameHandler.origWidth / genericGameHandler.origHeight;

                        newHeight = (newWidth / genericGameHandler.origRatio);

                        if (newHeight > genericGameHandler.playerBoundsWidth)
                        {
                            newHeight = genericGameHandler.playerBoundsWidth;
                        }
                    }
                }
                else
                {
                    if (genericGameHandler.origHeight > 0 && genericGameHandler.origWidth > 0)
                    {
                        genericGameHandler.origRatio = (double)genericGameHandler.origWidth / genericGameHandler.origHeight;

                        newWidth = (newHeight * genericGameHandler.origRatio);

                        if (newWidth > genericGameHandler.playerBoundsHeight)
                        {
                            newWidth = genericGameHandler.playerBoundsHeight;
                        }
                    }
                }
                size.Width = (int)newWidth;
                size.Height = (int)newHeight;

                if (newWidth < genericGameHandler.origWidth)
                {
                    int yOffset = Convert.ToInt32(loc.Y + ((genericGameHandler.playerBoundsHeight - newHeight) / 2));
                    loc.Y = yOffset;
                }
                if (newHeight < genericGameHandler.origHeight)
                {
                    int xOffset = Convert.ToInt32(loc.X + ((genericGameHandler.playerBoundsWidth - newWidth) / 2));
                    loc.X = xOffset;
                }
            }

            if (!gen.DontResize)
            {
                genericGameHandler.Log(string.Format("Resizing this game window and keeping aspect ratio. Values: width:{0}, height:{1}, aspectratio:{2}, origwidth:{3}, origheight:{4}, plyrboundwidth:{5}, plyrboundheight:{6}", size.Width, size.Height, genericGameHandler.origRatio, genericGameHandler.origWidth, genericGameHandler.origHeight, genericGameHandler.playerBoundsWidth, genericGameHandler.playerBoundsHeight));
                WindowScrape.Static.HwndInterface.SetHwndSize(hwnd, size.Width, size.Height);
            }

            if (!gen.DontReposition)
            {
                genericGameHandler.Log(string.Format("Repositioning this game window to coords x:{0},y:{1}", loc.X, loc.Y));
                WindowScrape.Static.HwndInterface.SetHwndPos(hwnd, loc.X, loc.Y);
            }

            if (!gen.NotTopMost)
            {
                genericGameHandler.Log("Setting this game window to top most");
                WindowScrape.Static.HwndInterface.MakeTopMost(hwnd);
            }
        }

        public static void WindowStyleChanges(GenericGameHandler genericGameHandler, GenericGameInfo gen, ProcessData processData, int i)
        {
            try
            {
                genericGameHandler.Log("WindowStyleChanges called");
                if (gen.WindowStyleEndChanges?.Length > 0)
                {
                    uint lStyle = User32Interop.GetWindowLong(processData.HWnd.NativePtr, User32_WS.GWL_STYLE);
                    genericGameHandler.Log("Using user custom window style");
                    foreach (string val in gen.WindowStyleEndChanges)
                    {
                        if (val.StartsWith("~"))
                        {
                            lStyle &= ~Convert.ToUInt32(val.Substring(1), 16);
                        }
                        else
                        {
                            lStyle |= Convert.ToUInt32(val, 16);
                        }
                    }
                    User32Interop.SetWindowLong(processData.HWnd.NativePtr, User32_WS.GWL_STYLE, lStyle);
                }

                if (gen.ExtWindowStyleEndChanges?.Length > 0)
                {
                    uint lStyle = User32Interop.GetWindowLong(processData.HWnd.NativePtr, User32_WS.GWL_EXSTYLE);
                    genericGameHandler.Log("Using user custom extended window style");
                    foreach (string val in gen.ExtWindowStyleEndChanges)
                    {
                        if (val.StartsWith("~"))
                        {
                            lStyle &= ~Convert.ToUInt32(val.Substring(1), 16);
                        }
                        else
                        {
                            lStyle |= Convert.ToUInt32(val, 16);
                        }
                    }
                    User32Interop.SetWindowLong(processData.HWnd.NativePtr, User32_WS.GWL_EXSTYLE, lStyle);
                }

                User32Interop.SetWindowPos(processData.HWnd.NativePtr, IntPtr.Zero, 0, 0, 0, 0, (uint)(PositioningFlags.SWP_FRAMECHANGED | PositioningFlags.SWP_NOMOVE | PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOZORDER | PositioningFlags.SWP_NOOWNERZORDER));
            }
            catch (Exception ex)
            {
                genericGameHandler.Log("ERROR - Exception in WindowStyleChanges for instance " + i + ". " + ex.Message);
            }
        }

        public static Window CreateRawInputWindow(GenericGameHandler genericGameHandler, GenericGameInfo gen, Process proc, PlayerInfo player)//probably not the best class where to place this :/
        {
            genericGameHandler.Log("Creating raw input window");

            var hWnd = WaitForProcWindowHandleNotZero(genericGameHandler, proc);

            var mouseHdev = player.IsRawKeyboard ? player.RawMouseDeviceHandle : (IntPtr)(-1);
            var keyboardHdev = player.IsRawMouse ? player.RawKeyboardDeviceHandle : (IntPtr)(-1);

            var window = new Window(hWnd)
            {
                CursorVisibility = player.IsRawMouse && !gen.HideCursor && gen.DrawFakeMouseCursor,
                KeyboardAttached = keyboardHdev,
                MouseAttached = mouseHdev
            };

            window.CreateHookPipe(gen);

            RawInputManager.windows.Add(window);
            return window;
        }

        public static IntPtr WaitForProcWindowHandleNotZero(GenericGameHandler genericGameHandler, Process proc)
        {
            try
            {
                if ((int)proc.NucleusGetMainWindowHandle() == 0)
                {
                    for (int times = 0; times < 200; times++)
                    {
                        Thread.Sleep(500);
                        if ((int)proc.NucleusGetMainWindowHandle() > 0)
                        {
                            break;
                        }

                        if (times == 199 && (int)proc.NucleusGetMainWindowHandle() == 0)
                        {
                            if (!proc.HasExited)
                                genericGameHandler.Log(string.Format("ERROR - WaitForProcWindowHandleNotZero could not find main window handle for {0} (pid {1})", proc.ProcessName, proc.Id));
                        }
                    }
                }

                return proc.NucleusGetMainWindowHandle();
            }
            catch
            {
                genericGameHandler.Log("ERROR - WaitForProcWindowHandleNotZero encountered an exception");
                return (IntPtr)(-1);
            }
        }
        /// https://stackoverflow.com/questions/20938934/controlling-applications-volume-by-process-id/25584074#25584074
        public class VolumeMixer//Need to be moved to the "audio" class
        {
            public static float? GetApplicationVolume(int pid)
            {
                ISimpleAudioVolume volume = GetVolumeObject(pid);
                if (volume == null)
                    return null;

                float level;
                volume.GetMasterVolume(out level);
                Marshal.ReleaseComObject(volume);
                return level * 100;
            }

            public static bool? GetApplicationMute(int pid)
            {
                ISimpleAudioVolume volume = GetVolumeObject(pid);
                if (volume == null)
                    return null;

                bool mute;
                volume.GetMute(out mute);
                Marshal.ReleaseComObject(volume);
                return mute;
            }

            public static void SetApplicationVolume(int pid, float level)
            {
                ISimpleAudioVolume volume = GetVolumeObject(pid);
                if (volume == null)
                    return;

                Guid guid = Guid.Empty;
                volume.SetMasterVolume(level / 100, ref guid);
                Marshal.ReleaseComObject(volume);
            }

            public static void SetApplicationMute(int pid, bool mute)
            {
                ISimpleAudioVolume volume = GetVolumeObject(pid);
                if (volume == null)
                    return;

                Guid guid = Guid.Empty;
                volume.SetMute(mute, ref guid);
                Marshal.ReleaseComObject(volume);
            }

            private static ISimpleAudioVolume GetVolumeObject(int pid)
            {
                // get the speakers (1st render + multimedia) device
                IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                IMMDevice speakers;
                deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                object o;
                speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                IAudioSessionManager2 mgr = (IAudioSessionManager2)o;

                // enumerate sessions for on this device
                IAudioSessionEnumerator sessionEnumerator;
                mgr.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);

                // search for an audio session with the required name
                // NOTE: we could also use the process id instead of the app name (with IAudioSessionControl2)
                ISimpleAudioVolume volumeControl = null;
                for (int i = 0; i < count; i++)
                {
                    IAudioSessionControl2 ctl;
                    sessionEnumerator.GetSession(i, out ctl);
                    int cpid;
                    ctl.GetProcessId(out cpid);

                    if (cpid == pid)
                    {
                        volumeControl = ctl as ISimpleAudioVolume;
                        break;
                    }
                    Marshal.ReleaseComObject(ctl);
                }
                Marshal.ReleaseComObject(sessionEnumerator);
                Marshal.ReleaseComObject(mgr);
                Marshal.ReleaseComObject(speakers);
                Marshal.ReleaseComObject(deviceEnumerator);
                return volumeControl;
            }
        }

        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        internal class MMDeviceEnumerator
        {
        }

        internal enum EDataFlow
        {
            eRender,
            eCapture,
            eAll,
            EDataFlow_enum_count
        }

        internal enum ERole
        {
            eConsole,
            eMultimedia,
            eCommunications,
            ERole_enum_count
        }

        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMMDeviceEnumerator
        {
            int NotImpl1();

            [PreserveSig]
            int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);

            // the rest is not implemented
        }

        [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMMDevice
        {
            [PreserveSig]
            int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

            // the rest is not implemented
        }

        [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAudioSessionManager2
        {
            int NotImpl1();
            int NotImpl2();

            [PreserveSig]
            int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);

            // the rest is not implemented
        }

        [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAudioSessionEnumerator
        {
            [PreserveSig]
            int GetCount(out int SessionCount);

            [PreserveSig]
            int GetSession(int SessionCount, out IAudioSessionControl2 Session);
        }

        [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface ISimpleAudioVolume
        {
            [PreserveSig]
            int SetMasterVolume(float fLevel, ref Guid EventContext);

            [PreserveSig]
            int GetMasterVolume(out float pfLevel);

            [PreserveSig]
            int SetMute(bool bMute, ref Guid EventContext);

            [PreserveSig]
            int GetMute(out bool pbMute);
        }

        [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAudioSessionControl2
        {
            // IAudioSessionControl
            [PreserveSig]
            int NotImpl0();

            [PreserveSig]
            int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            [PreserveSig]
            int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

            [PreserveSig]
            int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            [PreserveSig]
            int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

            [PreserveSig]
            int GetGroupingParam(out Guid pRetVal);

            [PreserveSig]
            int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

            [PreserveSig]
            int NotImpl1();

            [PreserveSig]
            int NotImpl2();

            // IAudioSessionControl2
            [PreserveSig]
            int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            [PreserveSig]
            int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

            [PreserveSig]
            int GetProcessId(out int pRetVal);

            [PreserveSig]
            int IsSystemSoundsSession();

            [PreserveSig]
            int SetDuckingPreference(bool optOut);
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public static bool finish = false;
        public static GameProfile profile;
        private static bool canSwitch = true;

        public static void SwitchLayout()
        {
            if (profile == null || !GameProfile.Saved || !canSwitch)
            {
                Globals.MainOSD.Show(1600, $"Can't Be Used For Now");
                return;
            }

            List<PlayerInfo> players = profile.PlayersList.OrderBy(c => c.ScreenPriority).ThenBy(c => c.MonitorBounds.Y).ThenBy(c => c.MonitorBounds.X).ToList();
            bool adjust = GameProfile.UseSplitDiv;

            var screens = ScreensUtil.AllScreens();
            PlayerInfo prev = null;

            int shift = -2;
            int reset = 0;//set to 1  reset debugging purpose

            if (reset == 1)
            {
                foreach (PlayerInfo pl in players)
                {
                    pl.OtherLayout = null;
                    pl.CurrentLayout = 0;
                    finish = false;
                    pl.MonitorBounds = new Rectangle(pl.ProcessData.HWnd.Location, pl.ProcessData.HWnd.Size);
                }
                return;
            }

            for (int i = 0; i < players.Count; i++)
            {

                PlayerInfo p = players[i];

                ProcessData data = p.ProcessData;

                if (data == null)
                {
                    continue;
                }

                if (p.OtherLayout == null)
                {
                    p.OtherLayout = new List<Rectangle>();
                }

                // data.HWnd.TopMost = false;//debug

                Rectangle localizeFirstOfScr = new Rectangle(p.Owner.display.Location.X, p.Owner.display.Location.Y, 20, 20);
                Rectangle playerWindow = new Rectangle(data.HWnd.Location.X, data.HWnd.Location.Y, data.HWnd.Size.Width, data.HWnd.Size.Height);
                bool fisrtOfScr = localizeFirstOfScr.IntersectsWith(playerWindow);

                if (data.HWnd.Location.X <= -32000)//if minimized or in cutscenes mode
                {
                    return;
                }

                if (p.Owner.Type == UserScreenType.DualHorizontal)
                {
                    foreach (UserScreen screen in screens)
                    {
                        if (screen.DisplayIndex == p.Owner.DisplayIndex)
                        {
                            if (fisrtOfScr)
                            {
                                Rectangle Win = new Rectangle(adjust ? p.Owner.display.Location.X + 1 : p.Owner.display.Location.X,
                                                              adjust ? p.Owner.display.Location.Y + 1 : p.Owner.display.Location.Y,
                                                              adjust ? (p.Owner.display.Width / 2) - 3 : p.Owner.display.Width / 2,
                                                              adjust ? p.Owner.display.Height - 2 : p.Owner.display.Height);
                                data.HWnd.Size = Win.Size;
                                data.HWnd.Location = Win.Location;
                                p.MonitorBounds = Win;

                                continue;
                            }
                            else
                            {
                                Rectangle Win = new Rectangle(adjust ? (screen.MonitorBounds.X + (screen.MonitorBounds.Width / 2)) + 1 : screen.MonitorBounds.X + screen.MonitorBounds.Width / 2,
                                                              adjust ? screen.MonitorBounds.Y + 1 : screen.MonitorBounds.Y,
                                                              adjust ? (screen.MonitorBounds.Width / 2) - 4 : screen.MonitorBounds.Width / 2,
                                                              adjust ? screen.MonitorBounds.Height - 2 : screen.MonitorBounds.Height);
                                data.HWnd.Size = Win.Size;
                                data.HWnd.Location = Win.Location;
                                p.MonitorBounds = Win;
                                // break;
                            }

                            p.Owner.Type = UserScreenType.DualVertical;
                        }
                    }
                }
                else if (p.Owner.Type == UserScreenType.DualVertical)
                {
                    foreach (UserScreen screen in ScreensUtil.AllScreens())
                    {
                        //Console.WriteLine(p.Owner.MonitorBounds + " " + screen.MonitorBounds);
                        if (screen.DisplayIndex == p.Owner.DisplayIndex)
                        {

                            if (fisrtOfScr)
                            {
                                Rectangle Win = new Rectangle(adjust ? p.Owner.display.X + 2 : p.Owner.display.X,
                                                               adjust ? p.Owner.display.Y + 2 : p.Owner.display.Y,
                                                               adjust ? p.Owner.display.Width - 4 : p.Owner.display.Width,
                                                               adjust ? (p.Owner.display.Height / 2) - 2 : p.Owner.display.Height / 2);
                                data.HWnd.Size = Win.Size;
                                data.HWnd.Location = Win.Location;
                                p.MonitorBounds = Win;

                                continue;
                            }
                            else
                            {

                                Rectangle Win = new Rectangle(adjust ? screen.MonitorBounds.X + 2 : screen.MonitorBounds.X,
                                                              adjust ? (screen.MonitorBounds.Y + (screen.MonitorBounds.Height / 2)) + 2 : screen.MonitorBounds.Y + screen.MonitorBounds.Height / 2,
                                                              adjust ? screen.MonitorBounds.Width - 4 : screen.MonitorBounds.Width,
                                                              adjust ? (screen.MonitorBounds.Height / 2) - 4 : screen.MonitorBounds.Height / 2);

                                data.HWnd.Size = Win.Size;
                                data.HWnd.Location = Win.Location;
                                p.MonitorBounds = Win;
                            }

                            p.Owner.Type = UserScreenType.DualHorizontal;
                        }
                    }
                }

                else if (p.Owner.Type == UserScreenType.FourPlayers && !finish)
                {

                    foreach (UserScreen screen in ScreensUtil.AllScreens())
                    {
                        if (screen.DisplayIndex == p.Owner.DisplayIndex)
                        {

                            p.OtherLayout.Add(p.MonitorBounds);
                            p.CurrentLayout++;//Skip Original default bounds on first swap

                            if (fisrtOfScr)//Player 0
                            {
                                //All Vertical
                                Rectangle avail1 = new Rectangle(adjust ? screen.MonitorBounds.X + 2 : screen.MonitorBounds.X,
                                                                 adjust ? screen.MonitorBounds.Y + 2 : screen.MonitorBounds.Y,
                                                                 adjust ? (screen.MonitorBounds.Width / p.Owner.PlayerOnScreen) - 3 : screen.MonitorBounds.Width / p.Owner.PlayerOnScreen,
                                                                 adjust ? p.Owner.display.Height - 4 : p.Owner.display.Height);
                                p.OtherLayout.Add(avail1);

                                //All Horizontal
                                Rectangle avail2 = new Rectangle(adjust ? screen.MonitorBounds.X + 2 : screen.MonitorBounds.X,
                                                                 adjust ? screen.MonitorBounds.Y + 2 : screen.MonitorBounds.Y,
                                                                 adjust ? screen.MonitorBounds.Width - 4 : screen.MonitorBounds.Width,
                                                                 adjust ? (screen.MonitorBounds.Height / p.Owner.PlayerOnScreen) - 3 : screen.MonitorBounds.Height / p.Owner.PlayerOnScreen);
                                p.OtherLayout.Add(avail2);

                                //Four player Layout but with only 3 players
                                if (p.Owner.PlayerOnScreen == 3)
                                {

                                    // Top player full Width
                                    Rectangle avail3 = new Rectangle(adjust ? screen.MonitorBounds.X + 2 : screen.MonitorBounds.X,
                                                                     adjust ? screen.MonitorBounds.Y + 2 : screen.MonitorBounds.Y,
                                                                     adjust ? screen.MonitorBounds.Width - 4 : screen.MonitorBounds.Width,
                                                                     adjust ? (screen.MonitorBounds.Height / 2) - 2 : screen.MonitorBounds.Height / 2);
                                    p.OtherLayout.Add(avail3);

                                    //Full height vertical left
                                    Rectangle avail4 = new Rectangle(adjust ? screen.MonitorBounds.X + 2 : screen.MonitorBounds.X,
                                                                     adjust ? screen.MonitorBounds.Y + 2 : screen.MonitorBounds.Y,
                                                                     adjust ? (screen.MonitorBounds.Width / 2) - 2 : screen.MonitorBounds.Width / 2,
                                                                     adjust ? screen.MonitorBounds.Height - 3 : screen.MonitorBounds.Height);
                                    p.OtherLayout.Add(avail4);


                                    //Top left
                                    Rectangle avail5 = new Rectangle(adjust ? screen.MonitorBounds.X + 2 : screen.MonitorBounds.X,
                                                                     adjust ? screen.MonitorBounds.Y + 2 : screen.MonitorBounds.Y,
                                                                     adjust ? (screen.MonitorBounds.Width / 2) - 2 : screen.MonitorBounds.Width / 2,
                                                                     adjust ? (screen.MonitorBounds.Height / 2) - 2 : screen.MonitorBounds.Height / 2);
                                    p.OtherLayout.Add(avail5);

                                    //Top left
                                    Rectangle avail6 = avail5;

                                    p.OtherLayout.Add(avail6);

                                }

                                shift += 3;

                                prev = p;

                                continue;
                            }
                            else//All players above 0
                            {
                                //All Vertical
                                Rectangle avail1 = new Rectangle(adjust ? prev.OtherLayout[1].Right + 3 : prev.OtherLayout[1].Right,
                                                                 adjust ? screen.MonitorBounds.Y + 2 : screen.MonitorBounds.Y,
                                                                 adjust ? prev.OtherLayout[1].Width : prev.OtherLayout[1].Width,
                                                                 adjust ? prev.OtherLayout[1].Height : prev.OtherLayout[1].Height);
                                p.OtherLayout.Add(avail1);

                                //All Horizontal
                                Rectangle avail2 = new Rectangle(adjust ? screen.MonitorBounds.X + 2 : screen.MonitorBounds.X,
                                                                 adjust ? prev.OtherLayout[2].Bottom + 2 : prev.OtherLayout[2].Bottom,
                                                                 adjust ? prev.OtherLayout[2].Width : prev.OtherLayout[2].Width,
                                                                 adjust ? prev.OtherLayout[2].Height : prev.OtherLayout[2].Height);
                                p.OtherLayout.Add(avail2);

                                if (i >= 1 && prev.DisplayIndex == p.DisplayIndex)
                                {
                                    prev = p;
                                }

                                //Four player Layout but with only 3 players//
                                //Top player full Width
                                if (p.Owner.PlayerOnScreen == 3)
                                {

                                    //if (i == 1 || i == 4)// 
                                    if (i == shift)
                                    {
                                        //Bottom left
                                        Rectangle avail3 = new Rectangle(adjust ? screen.MonitorBounds.X + 2 : screen.MonitorBounds.X,
                                                                         adjust ? (screen.MonitorBounds.Y + (screen.MonitorBounds.Height / 2)) + 2 : screen.MonitorBounds.Y + screen.MonitorBounds.Height / 2,
                                                                         adjust ? (screen.MonitorBounds.Width / 2) - 3 : screen.MonitorBounds.Width / 2,
                                                                         adjust ? (screen.MonitorBounds.Height / 2) - 4 : screen.MonitorBounds.Height / 2);
                                        p.OtherLayout.Add(avail3);

                                        //Top right
                                        Rectangle avail4 = new Rectangle(adjust ? (screen.MonitorBounds.X + (screen.MonitorBounds.Width / 2)) + 2 : screen.MonitorBounds.X + screen.MonitorBounds.Width / 2,
                                                                         adjust ? screen.MonitorBounds.Y + screen.MonitorBounds.Y + 2 : screen.MonitorBounds.Y + screen.MonitorBounds.Y,
                                                                         adjust ? (screen.MonitorBounds.Width / 2) - 4 : screen.MonitorBounds.Width / 2,
                                                                         adjust ? (screen.MonitorBounds.Height / 2) - 2 : screen.MonitorBounds.Height / 2);
                                        p.OtherLayout.Add(avail4);

                                        //Top right
                                        Rectangle avail5 = avail4;

                                        p.OtherLayout.Add(avail5);

                                        //Right full height
                                        Rectangle avail6 = new Rectangle(adjust ? (screen.MonitorBounds.X + (screen.MonitorBounds.Width / 2)) + 2 : screen.MonitorBounds.X + screen.MonitorBounds.Width / 2,
                                                                         adjust ? screen.MonitorBounds.Y + screen.MonitorBounds.Y + 2 : screen.MonitorBounds.Y + screen.MonitorBounds.Y,
                                                                         adjust ? (screen.MonitorBounds.Width / 2) - 3 : screen.MonitorBounds.Width / 2,
                                                                         adjust ? screen.MonitorBounds.Height - 3 : screen.MonitorBounds.Height);
                                        p.OtherLayout.Add(avail6);
                                    }
                                    else if (i == shift + 1)//f(i == 2 || i == 5)
                                    {
                                        //Bottom right
                                        Rectangle avail3 = new Rectangle(adjust ? screen.MonitorBounds.X + (screen.MonitorBounds.Width / 2) + 2 : screen.MonitorBounds.X + screen.MonitorBounds.Width / 2,
                                                                         adjust ? (screen.MonitorBounds.Y + (screen.MonitorBounds.Height / 2)) + 2 : screen.MonitorBounds.Y + screen.MonitorBounds.Height / 2,
                                                                         adjust ? (screen.MonitorBounds.Width / 2) - 4 : screen.MonitorBounds.Width / 2,
                                                                         adjust ? (screen.MonitorBounds.Height / 2) - 4 : screen.MonitorBounds.Height / 2);
                                        p.OtherLayout.Add(avail3);

                                        //Bottom right
                                        Rectangle avail4 = avail3;

                                        p.OtherLayout.Add(avail4);

                                        //Bottom full width
                                        Rectangle avail5 = new Rectangle(adjust ? screen.MonitorBounds.X + 2 : screen.MonitorBounds.X,
                                                                         adjust ? (screen.MonitorBounds.Y + (screen.MonitorBounds.Height / 2)) + 2 : screen.MonitorBounds.Y + screen.MonitorBounds.Height / 2,
                                                                         adjust ? screen.MonitorBounds.Width - 4 : screen.MonitorBounds.Width,
                                                                         adjust ? (screen.MonitorBounds.Height / 2) - 3 : screen.MonitorBounds.Height / 2);
                                        p.OtherLayout.Add(avail5);

                                        //Bottom left
                                        Rectangle avail6 = new Rectangle(adjust ? screen.MonitorBounds.X + 2 : screen.MonitorBounds.X,
                                                                         adjust ? (screen.MonitorBounds.Y + (screen.MonitorBounds.Height / 2)) + 2 : screen.MonitorBounds.Y + screen.MonitorBounds.Height / 2,
                                                                         adjust ? (screen.MonitorBounds.Width / 2) - 2 : screen.MonitorBounds.Width / 2,
                                                                         adjust ? (screen.MonitorBounds.Height / 2) - 3 : screen.MonitorBounds.Height / 2);
                                        p.OtherLayout.Add(avail6);

                                    }
                                }
                            }

                            break;
                        }

                    }

                    if (i == players.Count - 1)
                    {
                        finish = true;
                    }
                }

            }

            //for (int i = 0; i < players.Count; i++)
            //{
            //    PlayerInfo p = players[i];
            //    ProcessData data = p.ProcessData;

            //    //Swaping 4p layout here 
            //    if (p.Owner.Type == UserScreenType.FourPlayers)
            //    {
            //        if (p.OtherLayout.Count == 0)
            //        {
            //            Console.WriteLine("No other Layout found");
            //            return;
            //        }

            //        data.HWnd.Size = p.OtherLayout[p.CurrentLayout].Size;
            //        data.HWnd.Location = p.OtherLayout[p.CurrentLayout].Location;
            //        p.MonitorBounds = new Rectangle(data.HWnd.Location, data.HWnd.Size);
            //        //Console.WriteLine(i +": "+p.OtherLayout.Count);
            //        if (p.CurrentLayout == p.OtherLayout.Count() - 1)
            //        {
            //            p.CurrentLayout = 0;
            //        }
            //        else
            //        {
            //            p.CurrentLayout++;
            //        }
            //    }
            //}

            //Globals.MainOSD.Settings(1600, $"Switching Layouts");
        }


        public static void ToggleCutScenesMode(bool on)
        {
            if (profile == null || !GameProfile.Saved)
            {
                Globals.MainOSD.Show(1600, $"Can't Be Used For Now");
                return;
            }

            List<PlayerInfo> players = profile.PlayersList;

            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfo p = players[i];
                ProcessData data = p.ProcessData;

                if (data == null)
                {
                    continue;
                }

                Rectangle localizeFirstOfScr = new Rectangle(p.Owner.display.Location.X, p.Owner.display.Location.Y, 20, 20);
                Rectangle playerWindow = new Rectangle(data.HWnd.Location.X, data.HWnd.Location.Y, data.HWnd.Size.Width, data.HWnd.Size.Height);
                bool fisrtOfScr = localizeFirstOfScr.IntersectsWith(playerWindow);

                // Console.WriteLine(fisrtOfScr + " " + p.PlayerID);

                if (on)
                {
                    ///need now to not mute sound for only one instance between all screens it could be a profile setting.

                    //monitorcbounds ne peut pas etre utiliser pour localiser la position de la fenetre sur un ecran donné (peut etre negatif)                  
                    if (fisrtOfScr)//(p.PlayerID == 0)
                    {
                        if (!GameProfile.Cts_KeepAspectRatio && !GameProfile.Cts_MuteAudioOnly)//will set player 0 window fullscreened.
                        {
                            //Console.WriteLine($"Set player {p.PlayerID} window fullsceened.");
                            Rectangle setfullScreen = new Rectangle(p.Owner.display.X, p.Owner.display.Y, p.Owner.display.Width, p.Owner.display.Height);

                            data.HWnd.Size = setfullScreen.Size;
                            data.HWnd.Location = new Point(setfullScreen.X, setfullScreen.Y);
                        }
                        else if (!GameProfile.Cts_MuteAudioOnly) //will keep player window size and center it on screen.
                        {
                            //Console.WriteLine($"Set player {p.PlayerID} centered without resizing it.");
                            Rectangle centeredOnly = RectangleUtil.Center(p.MonitorBounds, p.Owner.display);

                            data.HWnd.Size = centeredOnly.Size;
                            data.HWnd.Location = centeredOnly.Location;
                        }
                    }
                    else//Move all other player windows in the wild and mute sound 
                    {
                        if (!GameProfile.Cts_MuteAudioOnly)
                        {
                            Rectangle hiddenWindow = new Rectangle(p.MonitorBounds.X, p.MonitorBounds.Y, p.MonitorBounds.Width, p.MonitorBounds.Height);

                            data.HWnd.Location = new Point(-32000, -32000);//Minizing is probably a better option
                            data.HWnd.Size = hiddenWindow.Size;
                        }

                        VolumeMixer.SetApplicationVolume(data.Process.Id, 0.0f);
                    }
                }
                else//Reset all player windows and unmute
                {
                    if (fisrtOfScr)//(p.PlayerID == 0)
                    {
                        if (!GameProfile.Cts_MuteAudioOnly)//Reset player 0 window
                        {
                            Rectangle resizeReposition = new Rectangle(p.MonitorBounds.X, p.MonitorBounds.Y, p.MonitorBounds.Width, p.MonitorBounds.Height);

                            data.HWnd.Location = resizeReposition.Location;
                            data.HWnd.Size = resizeReposition.Size;
                        }
                    }
                    else
                    {
                        if (!GameProfile.Cts_MuteAudioOnly)//Reset all other player window
                        {
                            Rectangle reposition = new Rectangle(p.MonitorBounds.X, p.MonitorBounds.Y, p.MonitorBounds.Width, p.MonitorBounds.Height);

                            data.HWnd.Location = reposition.Location;
                            data.HWnd.Size = reposition.Size;
                        }

                        VolumeMixer.SetApplicationVolume(data.Process.Id, 100.0f);
                    }
                }
            }

            if (on)
            {
                canSwitch = false;
                Globals.MainOSD.Show(1600, "Cutscenes Mode On");
            }
            else
            {
                canSwitch = true;
                Globals.MainOSD.Show(1600, "Cutscenes Mode Off");

                if (GameProfile.Cts_Unfocus)
                {
                    ChangeForegroundWindow();
                }
            }
        }

        public static void UpdateAndRefreshGameWindows(GenericGameHandler genericGameHandler, GenericGameInfo gen, GameProfile profile, double delayMS, bool refresh)
        {
            if (profile == null)
            {
                return;
            }

            genericGameHandler.exited = 0;
            List<PlayerInfo> players = profile.PlayersList;
            genericGameHandler.timer += delayMS;

            bool updatedHwnd = false;

            if (genericGameHandler.timer > genericGameHandler.HWndInterval)
            {
                updatedHwnd = true;
                genericGameHandler.timer = 0;
            }

            Application.DoEvents();

            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfo p = players[i];
                ProcessData data = p.ProcessData;

                if (data == null)
                {
                    continue;
                }

                if (refresh)
                {
                    genericGameHandler.TriggerOSD(100000, "Reseting game windows. Please wait...");
                    data.HWNDRetry = false;
                    // data.HWnd = null;
                    //updatedHwnd = false;
                    data.Setted = false;
                    data.Finished = false;
                    data.Status = 0;
                    resetingWindows = true;
                }

                if (data.Finished)
                {
                    if (data.Process.HasExited)
                    {
                        genericGameHandler.exited++;
                    }

                    continue;
                }

                if (p.SteamEmu)
                {
                    List<int> children = ProcessUtil.GetChildrenProcesses(data.Process);

                    // catch the game process, that was spawned from Smart Steam Emu
                    if (children.Count > 0)
                    {
                        for (int j = 0; j < children.Count; j++)
                        {
                            int id = children[j];
                            Process child = Process.GetProcessById(id);
                            try
                            {
                                if (child.ProcessName.Contains("conhost"))
                                {
                                    continue;
                                }
                            }
                            catch
                            {
                                continue;
                            }

                            data.AssignProcess(child);
                            p.SteamEmu = child.ProcessName.Contains("SmartSteamLoader") || child.ProcessName.Contains("cmd");
                        }
                    }
                }
                else
                {
                    if (updatedHwnd)
                    {
                        if (data.Setted)
                        {
                            if (data.Process.HasExited)
                            {
                                genericGameHandler.exited++;
                                continue;
                            }

                            if (!gen.PromptBetweenInstances)
                            {
                                if (!gen.NotTopMost)
                                {
                                    if (!gen.SetTopMostAtEnd)
                                    {
                                        genericGameHandler.Log("(Update) Setting game window to top most");
                                        data.HWnd.TopMost = true;
                                    }
                                }
                            }

                            if (data.Status == 2)
                            {
                                if (!gen.DontRemoveBorders)
                                {
                                    genericGameHandler.Log("(Update) Removing game window border for pid " + data.Process.Id);
                                    uint lStyle = User32Interop.GetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_STYLE);
                                    if (gen.WindowStyleValues?.Length > 0)
                                    {
                                        genericGameHandler.Log("(Update) Using user custom window style");
                                        foreach (string val in gen.WindowStyleValues)
                                        {
                                            if (val.StartsWith("~"))
                                            {
                                                lStyle &= ~Convert.ToUInt32(val.Substring(1), 16);
                                            }
                                            else
                                            {
                                                lStyle |= Convert.ToUInt32(val, 16);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        lStyle &= ~User32_WS.WS_CAPTION;
                                        lStyle &= ~User32_WS.WS_THICKFRAME;
                                        lStyle &= ~User32_WS.WS_MINIMIZE;
                                        lStyle &= ~User32_WS.WS_MAXIMIZE;
                                        lStyle &= ~User32_WS.WS_SYSMENU;

                                        lStyle &= ~User32_WS.WS_DLGFRAME;
                                        lStyle &= ~User32_WS.WS_BORDER;
                                    }

                                    int resultCode = User32Interop.SetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_STYLE, lStyle);

                                    lStyle = User32Interop.GetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_EXSTYLE);

                                    if (gen.ExtWindowStyleValues?.Length > 0)
                                    {
                                        genericGameHandler.Log("(Update) Using user custom extended window style");
                                        foreach (string val in gen.ExtWindowStyleValues)
                                        {
                                            if (val.StartsWith("~"))
                                            {
                                                lStyle &= ~Convert.ToUInt32(val.Substring(1), 16);
                                            }
                                            else
                                            {
                                                lStyle |= Convert.ToUInt32(val, 16);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        lStyle &= ~User32_WS.WS_EX_DLGMODALFRAME;
                                        lStyle &= ~User32_WS.WS_EX_CLIENTEDGE;
                                        lStyle &= ~User32_WS.WS_EX_STATICEDGE;
                                    }

                                    int resultCode2 = User32Interop.SetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_EXSTYLE, lStyle);

                                    User32Interop.SetWindowPos(data.HWnd.NativePtr, IntPtr.Zero, 0, 0, 0, 0, (uint)(PositioningFlags.SWP_FRAMECHANGED | PositioningFlags.SWP_NOMOVE | PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOZORDER | PositioningFlags.SWP_NOOWNERZORDER));
                                }

                                if (gen.EnableWindows)
                                {
                                    EnableWindow(data.HWnd.NativePtr, true);
                                }

                                //Minimise and un-minimise the window. Fixes black borders in Minecraft, but causing stretching issues in games like Minecraft.
                                if (gen.RefreshWindowAfterStart)
                                {
                                    genericGameHandler.Log("Refreshing game window");
                                    ShowWindow(data.HWnd.NativePtr, 6);
                                    ShowWindow(data.HWnd.NativePtr, 9);

                                    if (gen.SetForegroundWindowElsewhere)
                                    {
                                        ChangeForegroundWindow();
                                    }
                                }

                                if (gen.WindowStyleEndChanges?.Length > 0 || gen.ExtWindowStyleEndChanges?.Length > 0)
                                {
                                    Thread.Sleep(1000);
                                    WindowStyleChanges(genericGameHandler, gen, data, i);
                                }

                                data.Finished = true;

                                Debug.WriteLine("State 2");
                                genericGameHandler.Log("Update State 2");

                                if (i == (players.Count - 1))
                                {
                                    if (gen.LockMouse)
                                    {
                                        genericGameHandler._cursorModule.SetActiveWindow();
                                    }

                                    if (resetingWindows)
                                    {
                                        if (gen.SetForegroundWindowElsewhere)
                                        {
                                            ChangeForegroundWindow();
                                        }
                                        genericGameHandler.TriggerOSD(2000, "Game Windows Reseted");
                                        resetingWindows = false;
                                    }
                                }
                            }
                            else if (data.Status == 1)
                            {
                                if (!gen.KeepAspectRatio && !gen.KeepMonitorAspectRatio && !genericGameHandler.dllRepos && !gen.DontResize)
                                {
                                    if (data.Position.X != p.MonitorBounds.X || data.Position.Y != p.MonitorBounds.Y)
                                    {
                                        genericGameHandler.Log("(Update) Data position X or Y does not match player bounds for pid " + data.Process.Id + ", using player bound variables");
                                        genericGameHandler.Log(string.Format("(Update) Repostioning game window for pid {0} to coords x:{1},y:{2}", data.Process.Id, p.MonitorBounds.X, p.MonitorBounds.Y));
                                        //data.HWnd.Location = data.Position;
                                        data.HWnd.Location = new Point(p.MonitorBounds.X, p.MonitorBounds.Y);
                                    }
                                    else
                                    {
                                        genericGameHandler.Log(string.Format("(Update) Repostioning game window for pid {0} to coords x:{1},y:{2}", data.Process.Id, data.Position.X, data.Position.Y));
                                        data.HWnd.Location = data.Position;
                                    }
                                }

                                data.Status++;
                                genericGameHandler.Log("Update State 1");

                                if (gen.LockMouse)
                                {
                                    if (p.IsKeyboardPlayer && !p.IsRawKeyboard)
                                    {
                                        genericGameHandler._cursorModule.Setup(data.Process, p.MonitorBounds);
                                    }
                                    else
                                    {
                                        genericGameHandler._cursorModule.AddOtherGameHandle(data.Process.NucleusGetMainWindowHandle());
                                    }
                                }

                                if (i == (players.Count - 1) && !gen.ProcessChangesAtEnd)
                                {
                                    if (resetingWindows)
                                    {
                                        if (gen.SetForegroundWindowElsewhere)
                                        {
                                            ChangeForegroundWindow();
                                        }

                                        genericGameHandler.TriggerOSD(2000, "Game Windows Reseted");
                                        resetingWindows = false;
                                    }

                                    genericGameHandler.Log("(Update) All done!");
                                    //try
                                    //{
                                    //    if (statusWinThread != null && statusWinThread.IsAlive)
                                    //    {
                                    //        Thread.Sleep(5000);
                                    //        if (statusWinThread != null && statusWinThread.IsAlive)
                                    //        {
                                    //            statusWinThread.Abort();
                                    //        }
                                    //    }
                                    //}
                                    //catch { }
                                }
                            }
                            else if (data.Status == 0)
                            {
                                if (!genericGameHandler.dllResize && !gen.DontResize)
                                {
                                    if (gen.KeepAspectRatio || gen.KeepMonitorAspectRatio)
                                    {
                                        if (gen.KeepMonitorAspectRatio)
                                        {
                                            genericGameHandler.origWidth = genericGameHandler.owner.MonitorBounds.Width;
                                            genericGameHandler.origHeight = genericGameHandler.owner.MonitorBounds.Height;
                                        }
                                        else
                                        {
                                            if (GetWindowRect(data.Process.NucleusGetMainWindowHandle(), out RECT Rect))
                                            {
                                                genericGameHandler.origWidth = Rect.Right - Rect.Left;
                                                genericGameHandler.origHeight = Rect.Bottom - Rect.Top;
                                            }
                                        }

                                        double newWidth = genericGameHandler.playerBoundsWidth;
                                        double newHeight = genericGameHandler.playerBoundsHeight;

                                        if (newWidth < genericGameHandler.origWidth)
                                        {
                                            if (genericGameHandler.origHeight > 0 && genericGameHandler.origWidth > 0)
                                            {
                                                genericGameHandler.origRatio = (double)genericGameHandler.origWidth / genericGameHandler.origHeight;

                                                newHeight = (newWidth / genericGameHandler.origRatio);

                                                if (newHeight > genericGameHandler.playerBoundsHeight)
                                                {
                                                    newHeight = genericGameHandler.playerBoundsHeight;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (genericGameHandler.origHeight > 0 && genericGameHandler.origWidth > 0)
                                            {
                                                genericGameHandler.origRatio = (double)genericGameHandler.origWidth / genericGameHandler.origHeight;

                                                newWidth = (newHeight * genericGameHandler.origRatio);

                                                if (newWidth > genericGameHandler.playerBoundsWidth)
                                                {
                                                    newWidth = genericGameHandler.playerBoundsWidth;
                                                }
                                            }
                                        }

                                        genericGameHandler.Log(string.Format("(Update) Resizing game window for pid {0} and keeping aspect ratio. Values: width:{1}, height:{2}, aspectratio:{3}, origwidth:{4}, origheight:{5}, plyrboundwidth:{6}, plyrboundheight:{7}", data.Process.Id, (int)newWidth, (int)newHeight, (Math.Truncate(genericGameHandler.origRatio * 100) / 100), genericGameHandler.origWidth, genericGameHandler.origHeight, genericGameHandler.playerBoundsWidth, genericGameHandler.playerBoundsHeight));
                                        data.HWnd.Size = new Size(Convert.ToInt32(newWidth), Convert.ToInt32(newHeight));

                                        //x horizontal , y vertical
                                        if (newWidth < genericGameHandler.origWidth)
                                        {
                                            int yOffset = Convert.ToInt32(data.Position.Y + ((genericGameHandler.playerBoundsHeight - newHeight) / 2));
                                            data.Position.Y = yOffset;
                                        }
                                        if (newHeight < genericGameHandler.origHeight)
                                        {
                                            int xOffset = Convert.ToInt32(data.Position.X + ((genericGameHandler.playerBoundsWidth - newWidth) / 2));
                                            data.Position.X = xOffset;
                                        }

                                        genericGameHandler.Log(string.Format("(Update) Resizing game window (for horizontal centering), coords x:{1},y:{2}", data.Process.Id, data.Position.X, data.Position.Y));
                                        data.HWnd.Location = data.Position;
                                    }
                                    else
                                    {
                                        if (data.Size.Width == 0 || data.Size.Height == 0)
                                        {
                                            genericGameHandler.Log("(Update) Data size width or height is showing as 0 for pid " + data.Process.Id + ", using player bound variables");
                                            data.Size.Width = genericGameHandler.playerBoundsWidth;
                                            data.Size.Height = genericGameHandler.playerBoundsHeight;

                                            if (genericGameHandler.playerBoundsWidth == 0 || genericGameHandler.playerBoundsHeight == 0)
                                            {
                                                genericGameHandler.Log("(Update) Play bounds width or height is showing as 0 for pid " + data.Process.Id + ", using monitor bound variables");
                                                data.Size.Width = p.MonitorBounds.Width;
                                                data.Size.Height = p.MonitorBounds.Height;
                                            }
                                        }
                                        genericGameHandler.Log(string.Format("(Update) Resizing game window for pid {0} to the following width:{1}, height:{2}", data.Process.Id, data.Size.Width, data.Size.Height));
                                        data.HWnd.Size = data.Size;
                                    }
                                }

                                data.Status++;
                                genericGameHandler.Log("Update State 0");
                            }
                        }
                        else
                        {
                            data.Process.Refresh();

                            if (data.Process.HasExited)
                            {
                                if (p.GotLauncher)
                                {
                                    if (p.GotGame)
                                    {
                                        genericGameHandler.exited++;
                                    }
                                    else
                                    {
                                        List<int> children = ProcessUtil.GetChildrenProcesses(data.Process);
                                        if (children.Count > 0)
                                        {
                                            for (int j = 0; j < children.Count; j++)
                                            {
                                                int id = children[j];
                                                Process pro = Process.GetProcessById(id);

                                                if (!genericGameHandler.attached.Contains(pro))
                                                {
                                                    genericGameHandler.attached.Add(pro);
                                                    genericGameHandler.attachedIds.Add(pro.Id);
                                                    p.ProcessID = pro.Id;
                                                    data.HWnd = null;
                                                    p.GotGame = true;
                                                    data.AssignProcess(pro);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Steam showing a launcher, need to find our game process
                                    string launcher = gen.LauncherExe;
                                    if (!string.IsNullOrEmpty(launcher))
                                    {
                                        if (launcher.ToLower().EndsWith(".exe"))
                                        {
                                            launcher = launcher.Remove(launcher.Length - 4, 4);
                                        }

                                        Process[] procs = Process.GetProcessesByName(launcher);
                                        for (int j = 0; j < procs.Length; j++)
                                        {
                                            Process pro = procs[j];

                                            if (!genericGameHandler.attachedIds.Contains(pro.Id))
                                            {
                                                genericGameHandler.attached.Add(pro);
                                                genericGameHandler.attachedIds.Add(pro.Id);
                                                p.ProcessID = pro.Id;
                                                data.AssignProcess(pro);
                                                p.GotLauncher = true;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (data.HWNDRetry || data.HWnd == null || data.HWnd.NativePtr != data.Process.NucleusGetMainWindowHandle())
                                {
                                    genericGameHandler.Log("Update data process has not exited");
                                    data.HWnd = new HwndObject(data.Process.NucleusGetMainWindowHandle());
                                    Point pos = data.HWnd.Location;

                                    if (string.IsNullOrEmpty(data.HWnd.Title) ||
                                        pos.X == -32000 ||
                                        data.HWnd.Title.ToLower() == gen.LauncherTitle?.ToLower())
                                    {
                                        data.HWNDRetry = true;
                                    }
                                    else if (!string.IsNullOrEmpty(gen.Hook.ForceFocusWindowName) &&
                                        // TODO: this Levenshtein distance is being used to help us around Call of Duty Black Ops, as it uses a ® icon in the title bar
                                        //       there must be a better way
                                        StringUtil.ComputeLevenshteinDistance(data.HWnd.Title, gen.Hook.ForceFocusWindowName) > 2 && !gen.HasDynamicWindowTitle)
                                    {
                                        data.HWNDRetry = true;
                                    }
                                    else
                                    {
                                        Size s = data.Size;
                                        data.Setted = true;
                                    }
                                }
                                else
                                {
                                    genericGameHandler.Log("Assigning window handle");
                                    data.HWnd = new HwndObject(data.Process.NucleusGetMainWindowHandle());
                                    Point pos = data.HWnd.Location;

                                    Size s = data.Size;
                                    data.Setted = true;
                                }
                            }
                        }
                    }
                }
            }

            if (genericGameHandler.exited == players.Count)
            {
                if (!genericGameHandler.hasEnded)
                {
                    genericGameHandler.hasEnded = true;
                    genericGameHandler.Log("Update method calling Handler End function");
                    genericGameHandler.End(false);
                }
            }
        }

        public static void SetWindowText(Process proc, string windowTitle)
        {
            SetWindowText(proc.NucleusGetMainWindowHandle(), windowTitle);
        }

        public static bool TopMostToggle = true;

        public static void ShowHideWindows(GenericGameInfo game)
        {
            if (!Gaming.Coop.InputManagement.LockInput.IsLocked)
            {
                if (TopMostToggle)
                {
                    try
                    {
                        foreach (Form back in GenericGameHandler.Instance.splitForms)
                        {
                            IntPtr splitHandle = FindWindow(null, back.Name);//Get the background Form(s)

                            if (splitHandle != null && splitHandle != IntPtr.Zero)
                            {
                                User32Interop.SetWindowPos(splitHandle, new IntPtr(-2), 0, 0, 0, 0,
                                (uint)(PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOMOVE));
                                ShowWindow(splitHandle, ShowWindowEnum.Minimize);
                            }
                        }

                        Process[] procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(game.ExecutableName.ToLower()));
                        if (procs.Length > 0)
                        {
                            for (int i = 0; i < procs.Length; i++)
                            {
                                IntPtr hWnd = procs[i].NucleusGetMainWindowHandle();
                                User32Interop.SetWindowPos(hWnd, new IntPtr(-2), 0, 0, 0, 0,
                                (uint)(PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOMOVE));
                                ShowWindow(hWnd, ShowWindowEnum.Minimize);
                            }
                        }
                    }
                    catch
                    { }

                    Globals.MainOSD.Show(1600, $"Game Windows Minimized");

                    User32Util.ShowTaskBar();

                    TopMostToggle = false;
                }
                else if (!TopMostToggle)
                {
                    foreach (Form back in GenericGameHandler.Instance.splitForms)
                    {
                        IntPtr splitHandle = FindWindow(null, back.Name);//Get the background Form(s)

                        if (splitHandle != null && splitHandle != IntPtr.Zero)
                        {
                            ShowWindow(splitHandle, ShowWindowEnum.Restore);
                            User32Interop.SetWindowPos(splitHandle, new IntPtr(-1), 0, 0, 0, 0,
                                (uint)(PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOMOVE));
                        }
                    }

                    Process[] procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(game.ExecutableName.ToLower()));
                    if (procs.Length > 0)
                    {
                        for (int i = 0; i < procs.Length; i++)
                        {
                            IntPtr hWnd = procs[i].NucleusGetMainWindowHandle();
                            ShowWindow(hWnd, ShowWindowEnum.Restore);
                            User32Interop.SetWindowPos(hWnd, new IntPtr(-1), 0, 0, 0, 0,
                                (uint)(PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOMOVE));
                        }
                    }

                    User32Util.HideTaskbar();
                    Globals.MainOSD.Show(1600, $"Game Windows Restored");

                    TopMostToggle = true;
                }
            }
            else
            {
                Globals.MainOSD.Show(1600, $"Unlock Inputs First (Press {Globals.ini.IniReadValue("Hotkeys", "LockKey")} key)");
            }
        }

        public static void ChangeForegroundWindow()
        {
            IntPtr nucHwnd = User32Interop.FindWindow(null, "Nucleus Co-op");

            if (nucHwnd != IntPtr.Zero)
                User32Interop.SetForegroundWindow(nucHwnd);
        }
    }
}
