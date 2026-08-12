using UnityEngine;
using PatternGame.Gameplay;
using PatternGame.Gameplay.Flow;
using PatternGame.Gameplay.Levels;
using PatternGame.Gameplay.Progress;
using PatternGame.Input;
using PatternGame.Presentation;
using PatternGame.UI;

namespace PatternGame.Bootstrap
{
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField]
        DifficultyConfig difficultyConfig;

        [SerializeField]
        ColorPalette colorPalette;

        [SerializeField]
        WallController wallController;

        [SerializeField]
        KeyPieceController keyPieceController;

        [SerializeField]
        DragController dragController;

        [SerializeField]
        MatchEffectController matchEffectController;

        [SerializeField]
        HudView hudView;

        [SerializeField, Min(0f)]
        float readyDelay = 1f;

        [SerializeField]
        bool useRandomSeed = true;

        [SerializeField]
        int fixedSeed = 12345;

        PointerInputService pointerInput;
        Playfield playfield;
        GameSession session;
        GameFlow gameFlow;

        public GameFlow Flow => gameFlow;

        public void StartNewRun()
        {
            if (gameFlow == null)
            {
                return;
            }

            int seed = useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : fixedSeed;

            gameFlow.StartRun(seed);
        }

        void Awake()
        {
            if (!HasRequiredReferences())
            {
                enabled = false;
                return;
            }

            if (colorPalette == null)
            {
                Debug.LogWarning($"{name}: Color Palette is not assigned; cells keep their prefab colours.", this);
            }

            int paletteVariantCount = colorPalette == null ? 1 : colorPalette.PairCount;

            pointerInput = new PointerInputService();
            playfield = new Playfield();
            session = new GameSession(
                new LevelGenerator(),
                difficultyConfig,
                playfield,
                new PlayerPrefsProgressStorage(),
                paletteVariantCount);

            wallController.Initialize(colorPalette);
            keyPieceController.Initialize(colorPalette);
            dragController.Initialize(pointerInput, playfield);

            IEffectPresenter effects;

            if (matchEffectController == null)
            {
                Debug.LogWarning($"{name}: Match Effect Controller is not assigned; match effects are disabled.", this);
                effects = new NullEffectPresenter();
            }
            else
            {
                matchEffectController.Initialize(colorPalette);
                effects = matchEffectController;
            }

            IHudPresenter hud;

            if (hudView == null)
            {
                Debug.LogWarning($"{name}: Hud View is not assigned; the HUD is disabled.", this);
                hud = new NullHudPresenter();
            }
            else
            {
                hud = hudView;
            }

            gameFlow = new GameFlow(
                session,
                wallController,
                keyPieceController,
                dragController,
                effects,
                hud,
                readyDelay);
        }

        void Start()
        {
            StartNewRun();
        }

        void Update()
        {
            if (gameFlow == null)
            {
                return;
            }

            gameFlow.Tick(Time.deltaTime);

            if (gameFlow.IsInState<GameOverState>() && pointerInput.PressedThisFrame)
            {
                StartNewRun();
            }
        }

        void OnDestroy()
        {
            gameFlow?.Stop();
        }

        bool HasRequiredReferences()
        {
            if (difficultyConfig == null)
            {
                Debug.LogError($"{name}: Difficulty Config is not assigned.", this);
                return false;
            }

            if (wallController == null)
            {
                Debug.LogError($"{name}: Wall Controller is not assigned.", this);
                return false;
            }

            if (keyPieceController == null)
            {
                Debug.LogError($"{name}: Key Piece Controller is not assigned.", this);
                return false;
            }

            if (dragController == null)
            {
                Debug.LogError($"{name}: Drag Controller is not assigned.", this);
                return false;
            }

            return true;
        }
    }
}
