using UnityEngine;

namespace BlackHorizon.Systems
{
    /// <summary>
    /// Persistent bootstrap singleton. Manages game state, pause and scene
    /// flow. Survives scene loads; other systems query this via Instance.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Tooltip("If true, the cursor is hidden and locked (FPS mode).")]
        [SerializeField] private bool lockCursorOnStart = true;

        public bool IsPaused { get; private set; }
        public bool IsGameplayActive { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            SetCursorLocked(lockCursorOnStart);
        }

        public void SetGameplayActive(bool active)
        {
            IsGameplayActive = active;
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void TogglePause()
        {
            SetPaused(!IsPaused);
        }

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            EventBus.PauseGame(paused);
            if (paused) SetCursorLocked(false);
        }

        public void QuitToMenu()
        {
            SetPaused(false);
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
