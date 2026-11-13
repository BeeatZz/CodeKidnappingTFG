using System;
using UnityEngine;

namespace ComputerUI
{
    // What the player tuned in the Settings app. It is kept with PlayerPrefs, so it is still there the next time the game starts.
    // Anything that depends on a setting reads it here, and listens to Changed to update itself when it is moved.
    public static class ComputerSettings
    {
        public static event Action Changed;

        private const string Prefix = "Computer.";
        private static readonly string[] Keys =
        {
            "Master", "UiSounds", "UiVolume", "TypingSounds", "TypingVolume", "UiScale", "Wallpaper", "WallpaperPicture", "MusicVolume", "Tooltips", "Clock24"
        };

        // The loudness of everything the computer plays, from 0 to 1
        public static float MasterVolume
        {
            get { return GetFloat("Master", 1f); }
            set { SetFloat("Master", value); }
        }

        public static bool UiSounds
        {
            get { return GetBool("UiSounds", true); }
            set { SetBool("UiSounds", value); }
        }

        public static float UiVolume
        {
            get { return GetFloat("UiVolume", 0.5f); }
            set { SetFloat("UiVolume", value); }
        }

        public static bool TypingSounds
        {
            get { return GetBool("TypingSounds", true); }
            set { SetBool("TypingSounds", value); }
        }

        public static float TypingVolume
        {
            get { return GetFloat("TypingVolume", 0.5f); }
            set { SetFloat("TypingVolume", value); }
        }

        // How many screen pixels one unit of the interface is. 0 means the computer picks what fits the screen.
        public static float UiScale
        {
            get { return GetFloat("UiScale", 0f); }
            set { SetFloat("UiScale", value); }
        }

        // 0 is the scene with the castle, 1 and up are the pictures of the look
        public static int WallpaperPicture
        {
            get { return PlayerPrefs.GetInt(Prefix + "WallpaperPicture", 0); }
            set
            {
                PlayerPrefs.SetInt(Prefix + "WallpaperPicture", value);
                RaiseChanged();
            }
        }

        public static float MusicVolume
        {
            get { return GetFloat("MusicVolume", 0.5f); }
            set { SetFloat("MusicVolume", value); }
        }

        public static bool AnimatedWallpaper
        {
            get { return GetBool("Wallpaper", true); }
            set { SetBool("Wallpaper", value); }
        }

        public static bool Tooltips
        {
            get { return GetBool("Tooltips", true); }
            set { SetBool("Tooltips", value); }
        }

        // The clock shows 16:30, or 4:30 PM
        public static bool Clock24
        {
            get { return GetBool("Clock24", true); }
            set { SetBool("Clock24", value); }
        }

        public static void ResetAll()
        {
            foreach (string key in Keys)
                PlayerPrefs.DeleteKey(Prefix + key);
            RaiseChanged();
        }

        private static float GetFloat(string key, float fallback)
        {
            return PlayerPrefs.GetFloat(Prefix + key, fallback);
        }

        private static void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(Prefix + key, value);
            RaiseChanged();
        }

        private static bool GetBool(string key, bool fallback)
        {
            return PlayerPrefs.GetInt(Prefix + key, fallback ? 1 : 0) == 1;
        }

        private static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(Prefix + key, value ? 1 : 0);
            RaiseChanged();
        }

        private static void RaiseChanged()
        {
            if (Changed != null)
                Changed();
        }
    }
}
