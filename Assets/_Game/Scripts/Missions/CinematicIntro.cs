using System.Collections;
using UnityEngine;
using BlackHorizon.Systems;

namespace BlackHorizon.Missions
{
    /// <summary>
    /// Plays an original cinematic opening for the mission: black screen,
    /// ambient audio, camera pans to the sky, a placeholder aircraft flies
    /// over, then fades into gameplay and fires an objective update.
    /// Runs as a camera-anchored sequence; the player camera is faded in.
    /// </summary>
    public class CinematicIntro : MonoBehaviour
    {
        [Header("Timings (seconds)")]
        [SerializeField] private float blackScreenDuration = 1.5f;
        [SerializeField] private float skyDuration = 2.5f;
        [SerializeField] private float planeWaitBeforeFlyover = 1.0f;
        [SerializeField] private float flyoverDuration = 4.0f;
        [SerializeField] private float fadeInDuration = 1.0f;

        [Header("Placeholders")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private GameObject placeholderAircraft;

        [Header("Intro Objective")]
        [SerializeField] private string introObjectiveText = "MISSION START";

        [Header("Fade")]
        [SerializeField] private GameObject fadePlane;       // fullscreen black quad
        [SerializeField] private Material fadeMaterial;

        private CanvasGroup _hudHint;
        private Coroutine _routine;

        private void Start()
        {
            if (gameplayCamera == null) gameplayCamera = Camera.main;
            _routine = StartCoroutine(RunIntro());
        }

        public void Skip()
        {
            if (_routine != null) StopCoroutine(_routine);
            FinishIntro();
        }

        private IEnumerator RunIntro()
        {
            var main = gameplayCamera;
            var prevFov = main.fieldOfView;
            main.enabled = true;

            // Black screen.
            ShowFade(1f);
            yield return new WaitForSeconds(blackScreenDuration);

            // Sky pan.
            Vector3 startLook = main.transform.forward;
            float t = 0f;
            while (t < skyDuration)
            {
                t += Time.deltaTime;
                float k = t / skyDuration;
                main.transform.rotation = Quaternion.Slerp(
                    Quaternion.LookRotation(startLook),
                    Quaternion.LookRotation(new Vector3(0.3f, 0.85f, -0.4f)),
                    k);
                yield return null;
            }

            // Aircraft flyover placeholder.
            if (placeholderAircraft != null)
            {
                placeholderAircraft.SetActive(true);
                var start = playerTransform.position + main.transform.right * 90f + Vector3.up * 40f;
                var end = playerTransform.position - main.transform.right * 110f + Vector3.up * 30f;
                placeholderAircraft.transform.position = start;

                float ft = 0f;
                while (ft < flyoverDuration)
                {
                    ft += Time.deltaTime;
                    float k = ft / flyoverDuration;
                    placeholderAircraft.transform.position = Vector3.Lerp(start, end, k);
                    placeholderAircraft.transform.forward = (end - start).normalized;
                    // Track the plane with the camera gently.
                    main.transform.LookAt(placeholderAircraft.transform.position + Vector3.up * 10f);
                    yield return null;
                }
                placeholderAircraft.SetActive(false);
            }

            // Restore looks toward player forward (gameplay).
            main.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, 1f), Vector3.up);

            // Fade into gameplay.
            yield return StartCoroutine(FadeTo(0f, fadeInDuration));

            FinishIntro();
        }

        private void FinishIntro()
        {
            // Signal the mission systems that gameplay is active.
            if (GameManager.Instance != null) GameManager.Instance.SetGameplayActive(true);
            EventBus.FireObjective(introObjectiveText);
            var mission = MissionManager.Instance;
            if (mission != null && mission.CurrentObjectiveIndex < 0) mission.StartMission();
            if (fadePlane != null) fadePlane.SetActive(false);
        }

        private void ShowFade(float alpha)
        {
            if (fadePlane != null)
            {
                fadePlane.SetActive(true);
                if (fadeMaterial != null) fadeMaterial.color = new Color(0f, 0f, 0f, alpha);
            }
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (fadePlane == null || fadeMaterial == null) { yield break; }
            float start = fadeMaterial.color.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                fadeMaterial.color = new Color(0f, 0f, 0f, Mathf.Lerp(start, targetAlpha, t / duration));
                yield return null;
            }
        }

        public static void SkipStatic()
        {
            var intro = FindFirstObjectByType<CinematicIntro>();
            if (intro != null) intro.Skip();
        }
    }
}
