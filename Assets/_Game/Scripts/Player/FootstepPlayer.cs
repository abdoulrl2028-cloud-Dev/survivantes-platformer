using UnityEngine;
using BlackHorizon.Systems;

namespace BlackHorizon.Player
{
    /// <summary>
    /// Generates procedural footstep events at a cadence based on movement
    /// speed, so the sound layer can play them. No audio clips are owned here;
    /// it just raises a world-space event.
    /// </summary>
    public class FootstepPlayer : MonoBehaviour
    {
        public float walkInterval = 0.45f;
        public float runInterval = 0.28f;

        private float _timer;

        public void UpdateFootsteps(float speed, bool running)
        {
            if (speed < 0.1f) return;

            float interval = running ? runInterval : walkInterval;
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = interval;
                EventBus.FireFootstep(transform.position);
            }
        }
    }
}
