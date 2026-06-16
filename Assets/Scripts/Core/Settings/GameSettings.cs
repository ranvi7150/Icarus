using System;
using UnityEngine;

namespace Icarus.Core.Settings
{
    [Serializable]
    public class GameSettings
    {
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float bgmVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        public bool fullscreen = true;
        public int screenWidth;
        public int screenHeight;

        public static GameSettings CreateDefault()
        {
            return new GameSettings
            {
                masterVolume = 1f,
                bgmVolume = 1f,
                sfxVolume = 1f,
                fullscreen = true,
                screenWidth = 0,
                screenHeight = 0
            };
        }

        public GameSettings Clone()
        {
            return new GameSettings
            {
                masterVolume = masterVolume,
                bgmVolume = bgmVolume,
                sfxVolume = sfxVolume,
                fullscreen = fullscreen,
                screenWidth = screenWidth,
                screenHeight = screenHeight
            };
        }

    }
}
