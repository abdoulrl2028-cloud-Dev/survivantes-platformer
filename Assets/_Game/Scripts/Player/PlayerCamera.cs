using UnityEngine;

namespace BlackHorizon.Player
{
    /// <summary>
    /// Cinematic FPS camera rig. Owns the camera child and applies:
    /// headbob, run/walk sway, recoil, aim FOV transition and impact shake.
    /// All values stay moderate to avoid discomfort and are editable.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera cam;

        [Header("Head Bob")]
        [SerializeField] private float walkBobAmp = 0.02f;
        [SerializeField] private float runBobAmp = 0.045f;
        [SerializeField] private float walkBobFreq = 8f;
        [SerializeField] private float runBobFreq = 10f;

        [Header("Sway")]
        [SerializeField] private float swayAmount = 0.015f;
        [SerializeField] private float swaySmooth = 8f;

        [Header("FOV")]
        [SerializeField] private float baseFov = 70f;
        [SerializeField] private float runFov = 76f;
        [SerializeField] private float aimFov = 55f;
        [SerializeField] private float fovLerpSpeed = 8f;

        [Header("Recoil")]
        [SerializeField] private float recoilKick = 0f;
        [SerializeField] private float recoilRecovery = 6f;

        [Header("Impact Shake")]
        [SerializeField] private float shakeAmount = 0.05f;
        [SerializeField] private float shakeDuration = 0.2f;

        private float _bobTimer;
        private float _recoilOffset;
        private float _shakeTimer;
        private float _shakeIntensity;

        public Camera Camera => cam;
        public event System.Action OnAimToggle;

        private void Awake()
        {
            if (cam == null) cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            cam.fieldOfView = baseFov;
        }

        /// <summary>
        /// Called each frame by FirstPersonController with how much motion is happening.
        /// headBobAmount: 0 idle, 0.5 walk, 1 run. moveSpeed used for scaling.
        /// </summary>
        public void ApplyMotion(float headBobAmount, float moveSpeed)
        {
            ApplyHeadBob(headBobAmount, moveSpeed);
            ApplySway();
            ApplyRecoilAndShake();
        }

        private void ApplyHeadBob(float amount, float moveSpeed)
        {
            if (moveSpeed < 0.1f)
            {
                _bobTimer = 0f;
                cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, Vector3.zero, 8f * Time.deltaTime);
                return;
            }

            float amp = Mathf.Lerp(walkBobAmp, runBobAmp, amount);
            float freq = Mathf.Lerp(walkBobFreq, runBobFreq, amount);
            _bobTimer += Time.deltaTime * freq;

            float bobY = Mathf.Sin(_bobTimer) * amp;
            float bobX = Mathf.Sin(_bobTimer * 0.5f) * amp * 0.6f;

            Vector3 target = new Vector3(bobX, bobY, 0f);
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, target, 10f * Time.deltaTime);
        }

        private void ApplySway()
        {
            float h = Input.GetAxisRaw("Mouse X");
            float v = Input.GetAxisRaw("Mouse Y");
            float swayX = Mathf.Clamp(-h * swayAmount, -swayAmount * 3f, swayAmount * 3f);
            float swayY = Mathf.Clamp(v * swayAmount, -swayAmount * 3f, swayAmount * 3f);
            cam.transform.localRotation = Quaternion.Slerp(
                cam.transform.localRotation,
                Quaternion.Euler(swayY, swayX, 0f),
                swaySmooth * Time.deltaTime);
        }

        private void ApplyRecoilAndShake()
        {
            if (_recoilOffset > 0f)
            {
                _recoilOffset = Mathf.Lerp(_recoilOffset, 0f, recoilRecovery * Time.deltaTime);
                if (_recoilOffset < 0.01f) _recoilOffset = 0f;
            }

            float shake = _shakeTimer > 0f ? _shakeIntensity : 0f;
            if (_shakeTimer > 0f) _shakeTimer -= Time.deltaTime;

            cam.transform.localPosition += new Vector3(
                Random.Range(-shake, shake) * 0.01f,
                Random.Range(-shake, shake) * 0.01f,
                Random.Range(-shake, shake) * 0.01f);
        }

        /// <summary>Adds upward camera recoil kick.</summary>
        public void ApplyRecoil()
        {
            _recoilOffset = Mathf.Clamp01(_recoilOffset + recoilKick);
        }

        /// <summary>Small impact shake for shots/explosions near the player.</summary>
        public void AddImpact(float intensity)
        {
            _shakeIntensity = Mathf.Max(_shakeIntensity, Mathf.Clamp01(intensity));
            _shakeTimer = shakeDuration;
        }

        /// <summary>Set the FOV to aim (ADS) or back to base.</summary>
        public void SetAiming(bool aiming)
        {
            ApplyFov(aiming ? aimFov : baseFov);
            OnAimToggle?.Invoke();
        }

        public void ApplyFov(float targetFov)
        {
            var target = targetFov;
            var cur = cam.fieldOfView;
            if (Mathf.Abs(cur - target) > 0.1f)
            {
                cam.fieldOfView = Mathf.Lerp(cur, target, fovLerpSpeed * Time.deltaTime);
            }
        }

        public void UpdateFov(float targetFov)
        {
            ApplyFov(targetFov);
        }
    }
}
