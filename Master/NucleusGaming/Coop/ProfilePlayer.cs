﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Gaming.Coop
{
    public class ProfilePlayer
    {
        public Rectangle MonitorBounds;
        public Rectangle OwnerDisplay;
        public Rectangle OwnerUIBounds;
        public Rectangle EditBounds;

        public Guid GamepadGuid;

        public int ScreenPriority;
        public int ScreenIndex;
        public int PlayerID = -1;
        public int OwnerType;
        public int DisplayIndex;

        public string Nickname;
        public string IdealProcessor;
        public string Affinity;
        public string PriorityClass;
        public string[] HIDDeviceIDs;

        public long SteamID = -1;
        public bool IsDInput;
        public bool IsXInput;
        public bool IsKeyboardPlayer;
        public bool IsRawMouse;
    }
}
