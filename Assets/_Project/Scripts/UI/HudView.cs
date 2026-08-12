using TMPro;
using UnityEngine;
using PatternGame.Gameplay.Flow;

namespace PatternGame.UI
{
    public sealed class HudView : MonoBehaviour, IHudPresenter
    {
        [SerializeField]
        TMP_Text levelLabel;

        [SerializeField]
        GameObject gameOverRoot;

        [SerializeField]
        TMP_Text resultLabel;

        [SerializeField]
        TMP_Text bestLabel;

        bool isReady;

        public void ShowLevel(int levelNumber)
        {
            if (!EnsureReady())
            {
                return;
            }

            levelLabel.text = $"LEVEL {levelNumber}";
            levelLabel.gameObject.SetActive(true);
        }

        public void ShowGameOver(int completedLevels, int bestLevel)
        {
            if (!EnsureReady())
            {
                return;
            }

            levelLabel.gameObject.SetActive(false);
            resultLabel.text = $"LEVEL {completedLevels}";
            bestLabel.text = $"BEST {bestLevel}";
            gameOverRoot.SetActive(true);
        }

        public void HideGameOver()
        {
            if (!EnsureReady())
            {
                return;
            }

            gameOverRoot.SetActive(false);
        }

        public void Clear()
        {
            if (!EnsureReady())
            {
                return;
            }

            levelLabel.gameObject.SetActive(false);
            gameOverRoot.SetActive(false);
        }

        void Awake()
        {
            if (EnsureReady())
            {
                Clear();
            }
        }

        bool EnsureReady()
        {
            if (isReady)
            {
                return true;
            }

            if (levelLabel == null)
            {
                Debug.LogError($"{name}: Level Label is not assigned.", this);
                return false;
            }

            if (gameOverRoot == null)
            {
                Debug.LogError($"{name}: Game Over Root is not assigned.", this);
                return false;
            }

            if (resultLabel == null)
            {
                Debug.LogError($"{name}: Result Label is not assigned.", this);
                return false;
            }

            if (bestLabel == null)
            {
                Debug.LogError($"{name}: Best Label is not assigned.", this);
                return false;
            }

            isReady = true;
            return true;
        }
    }
}
