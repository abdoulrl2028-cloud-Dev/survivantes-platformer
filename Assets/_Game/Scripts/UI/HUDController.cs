using UnityEngine;
using UnityEngine.UI;
using BlackHorizon.Player;
using BlackHorizon.Weapons;
using BlackHorizon.Systems;

namespace BlackHorizon.UI
{
    /// <summary>
    /// Minimalist FPS HUD. Builds its text/bar-based UI at runtime so no art
    /// assets are required. Displays health, ammo, weapon, objective and an
    /// interaction prompt. Listens to the player and mission event buses.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private Transform player;

        private Text _ammoText;
        private Text _weaponText;
        private Image _healthBar;
        private Text _healthText;
        private Text _objectiveText;
        private Text _interactText;
        private Sprite _crosshairSprite;

        private void Awake()
        {
            BuildCanvas();
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            EventBus.ObjectiveUpdated += OnObjective;
            var health = player != null ? player.GetComponent<PlayerHealth>() : null;
            if (health != null) health.OnHealthUIChanged += OnHealth;

            var weaponManager = player != null ? player.GetComponent<WeaponManager>() : null;
            if (weaponManager != null)
            {
                weaponManager.OnAmmoChanged += OnAmmoChanged;
                weaponManager.OnWeaponSwitched += OnWeaponSwitch;
            }

            var interact = player != null ? player.GetComponent<Interaction.InteractionController>() : null;
            if (interact != null)
            {
                interact.OnPromptChanged += OnPrompt;
                interact.OnPromptHide += OnPromptHide;
            }
        }

        private void Unsubscribe()
        {
            EventBus.ObjectiveUpdated -= OnObjective;
        }

        private void OnObjective(string text)
        {
            if (_objectiveText != null) _objectiveText.text = text;
        }

        private void OnHealth(float current, float max)
        {
            if (_healthBar != null) _healthBar.fillAmount = Mathf.Clamp01(current / max);
            if (_healthText != null) _healthText.text = $"{Mathf.CeilToInt(current)}";
        }

        private void OnAmmoChanged()
        {
            var manager = player != null ? player.GetComponent<WeaponManager>() : null;
            var weapon = manager != null ? manager.CurrentWeapon : null;
            if (weapon != null)
            {
                if (_ammoText != null) _ammoText.text = $"{weapon.CurrentMag} / {weapon.CurrentReserve}";
                if (_weaponText != null && weapon.Data != null) _weaponText.text = weapon.Data.weaponName.ToUpper();
            }
        }

        private void OnWeaponSwitch(int index)
        {
            OnAmmoChanged();
        }

        private void OnPrompt(string text)
        {
            if (_interactText != null) _interactText.text = $"[E] {text}";
        }

        private void OnPromptHide()
        {
            if (_interactText != null) _interactText.text = "";
        }

        // ---- Canvas construction ----

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("HUD");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<GraphicRaycaster>();
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _crosshairSprite = CreatePlusSprite();

            CreateCrosshair(canvasGO.transform);

            _healthBar = CreateBar(canvasGO.transform, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(400, 22));
            _healthText = CreateText(canvasGO.transform, "100", new Vector2(0f, 0f), new Vector2(40f, 76f), new Vector2(120, 30), 28, TextAnchor.MiddleLeft);

            _ammoText = CreateText(canvasGO.transform, "30 / 120", new Vector2(1f, 0f), new Vector2(-60f, 60f), new Vector2(300, 40), 40, TextAnchor.MiddleRight);
            _weaponText = CreateText(canvasGO.transform, "", new Vector2(1f, 0f), new Vector2(-60f, 20f), new Vector2(300, 26), 20, TextAnchor.MiddleRight);

            _objectiveText = CreateText(canvasGO.transform, "", new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1100, 40), 30, TextAnchor.MiddleCenter);
            _interactText = CreateText(canvasGO.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(600, 30), 26, TextAnchor.MiddleCenter);
        }

        private void CreateCrosshair(Transform parent)
        {
            var go = new GameObject("Crosshair", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(24, 24);
            var img = go.AddComponent<Image>();
            img.sprite = _crosshairSprite;
            img.color = new Color(1f, 1f, 1f, 0.85f);
        }

        private Image CreateBar(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Bar", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);

            var fillGO = new GameObject("Fill", typeof(RectTransform));
            fillGO.transform.SetParent(go.transform, false);
            var frt = fillGO.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            var fimg = fillGO.AddComponent<Image>();
            fimg.color = new Color(0.8f, 0.05f, 0.05f, 1f);
            fimg.type = Image.Type.Filled;
            fimg.fillMethod = Image.FillMethod.Horizontal;
            fimg.fillAmount = 1f;
            return fimg;
        }

        private Text CreateText(Transform parent, string content, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor textAnchor)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size == Vector2.zero ? new Vector2(300, 30) : size;
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = textAnchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Sprite CreatePlusSprite()
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    tex.SetPixel(x, y, (x == 4 || y == 4) ? Color.white : Color.clear);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f));
        }

        public void SetPlayer(Transform playerTransform)
        {
            player = playerTransform;
        }
    }
}
