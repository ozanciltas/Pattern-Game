using System;
using UnityEngine;
using PatternGame.Gameplay.Progress;

namespace PatternGame.Bootstrap
{
    public sealed class PlayerPrefsProgressStorage : IProgressStorage
    {
        const string DefaultKey = "PatternGame.BestLevel";

        readonly string key;

        public PlayerPrefsProgressStorage()
            : this(DefaultKey)
        {
        }

        public PlayerPrefsProgressStorage(string storageKey)
        {
            key = string.IsNullOrEmpty(storageKey) ? DefaultKey : storageKey;
        }

        public int LoadBestLevel()
        {
            return Math.Max(0, PlayerPrefs.GetInt(key, 0));
        }

        public void SaveBestLevel(int bestLevel)
        {
            PlayerPrefs.SetInt(key, Math.Max(0, bestLevel));
            PlayerPrefs.Save();
        }
    }
}
