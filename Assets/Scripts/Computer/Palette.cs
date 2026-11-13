using UnityEngine;

namespace Computer
{
    // The colours of the desktop. Computer.uss uses the same values.
    public static class Palette
    {
        public static readonly Color32 Black = Hex(0x0d070b);
        public static readonly Color32 Iron = Hex(0x3b2b31);
        public static readonly Color32 IronLight = Hex(0x5f4a50);
        public static readonly Color32 IronDark = Hex(0x1f1317);
        public static readonly Color32 Blood = Hex(0xa3202b);
        public static readonly Color32 BloodLight = Hex(0xd23b3b);
        public static readonly Color32 Gold = Hex(0xe8b44a);
        public static readonly Color32 Parchment = Hex(0xead9a8);
        public static readonly Color32 ParchmentDark = Hex(0xcdb47e);
        public static readonly Color32 Ink = Hex(0x2a1a12);
        public static readonly Color32 Ember = Hex(0xff7a2f);
        public static readonly Color32 Scry = Hex(0x6fe3e0);
        public static readonly Color32 ScryDark = Hex(0x2aa0a8);
        public static readonly Color32 Wood = Hex(0x9c6b35);
        public static readonly Color32 WoodLight = Hex(0xc58e4d);
        public static readonly Color32 WoodDark = Hex(0x6b4423);
        public static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        public static Color32 Hex(int rgb)
        {
            return new Color32((byte)((rgb >> 16) & 255), (byte)((rgb >> 8) & 255), (byte)(rgb & 255), 255);
        }
    }
}
