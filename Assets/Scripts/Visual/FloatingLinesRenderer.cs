using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace InformationString.Visual
{
    /// <summary>
    /// Drives the FloatingLines.shader uniform values every frame from a UI RawImage.
    /// Mirrors the props of the React Bits FloatingLines component so the effect can be
    /// tuned in the Inspector without any code changes.
    ///
    /// Scene setup:
    ///   Canvas (Screen Space – Overlay)
    ///   └─ RawImage (stretch-fill canvas, color = white, material = FloatingLines material)
    ///      └─ this component
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class FloatingLinesRenderer : MonoBehaviour
    {
        // ── Wave toggles ──────────────────────────────────────────────────────────
        [Header("Wave Layers")]
        public bool enableTop = true;
        public bool enableMiddle = true;
        public bool enableBottom = true;

        // ── Line counts (matching lineCount prop: array [top, middle, bottom]) ───
        [Header("Line Counts")]
        [Min(0)] public int topLineCount = 10;
        [Min(0)] public int middleLineCount = 15;
        [Min(0)] public int bottomLineCount = 20;

        // ── Line spacing (multiplied by 0.01 when sent to shader, matching React) ─
        [Header("Line Distance")]
        public float topLineDistance = 8f;
        public float middleLineDistance = 6f;
        public float bottomLineDistance = 4f;

        // ── Wave positions (x, y, rotate) ────────────────────────────────────────
        [Header("Wave Positions")]
        public Vector3 topWavePosition = new Vector3(10f, 0.5f, -0.4f);
        public Vector3 middleWavePosition = new Vector3(5f, 0.0f, 0.2f);
        public Vector3 bottomWavePosition = new Vector3(2f, -0.7f, -1.0f);

        // ── Animation ────────────────────────────────────────────────────────────
        [Header("Animation")]
        [Min(0f)] public float animationSpeed = 1f;

        // ── Mouse interaction (mirrors bendRadius / bendStrength props) ──────────
        [Header("Interaction")]
        public bool interactive = true;
        public float bendRadius = 5.0f;
        public float bendStrength = -0.5f;
        [Range(0f, 1f)] public float mouseDamping = 0.05f;

        // ── Parallax ─────────────────────────────────────────────────────────────
        [Header("Parallax")]
        public bool parallax = true;
        [Range(0f, 1f)] public float parallaxStrength = 0.2f;

        // ── Gradient (up to 8 hex colour stops, e.g. "#E947F5") ─────────────────
        [Header("Gradient (optional, max 8)")]
        public List<Color> linesGradient = new List<Color>();

        // ── Internal state ───────────────────────────────────────────────────────
        private Material _mat;
        private Vector2 _currentMouse = new Vector2(-1000f, -1000f);
        private Vector2 _targetMouse = new Vector2(-1000f, -1000f);
        private float _currentInfluence;
        private float _targetInfluence;
        private Vector2 _currentParallax;
        private Vector2 _targetParallax;

        // Cached shader property IDs
        private static readonly int ID_EnableTop = Shader.PropertyToID("_EnableTop");
        private static readonly int ID_EnableMiddle = Shader.PropertyToID("_EnableMiddle");
        private static readonly int ID_EnableBottom = Shader.PropertyToID("_EnableBottom");

        private static readonly int ID_TopCount = Shader.PropertyToID("_TopLineCount");
        private static readonly int ID_MidCount = Shader.PropertyToID("_MiddleLineCount");
        private static readonly int ID_BotCount = Shader.PropertyToID("_BottomLineCount");

        private static readonly int ID_TopDist = Shader.PropertyToID("_TopLineDistance");
        private static readonly int ID_MidDist = Shader.PropertyToID("_MiddleLineDistance");
        private static readonly int ID_BotDist = Shader.PropertyToID("_BottomLineDistance");

        private static readonly int ID_TopPos = Shader.PropertyToID("_TopWavePosition");
        private static readonly int ID_MidPos = Shader.PropertyToID("_MiddleWavePosition");
        private static readonly int ID_BotPos = Shader.PropertyToID("_BottomWavePosition");

        private static readonly int ID_AnimSpeed = Shader.PropertyToID("_AnimationSpeed");

        private static readonly int ID_Interactive = Shader.PropertyToID("_Interactive");
        private static readonly int ID_BendR = Shader.PropertyToID("_BendRadius");
        private static readonly int ID_BendS = Shader.PropertyToID("_BendStrength");
        private static readonly int ID_BendI = Shader.PropertyToID("_BendInfluence");

        private static readonly int ID_Parallax = Shader.PropertyToID("_Parallax");
        private static readonly int ID_ParallaxOff = Shader.PropertyToID("_ParallaxOffset");

        private static readonly int ID_Resolution = Shader.PropertyToID("_Resolution");
        private static readonly int ID_Mouse = Shader.PropertyToID("_MousePos");

        private static readonly int ID_GradCount = Shader.PropertyToID("_LineGradientCount");
        private static readonly int[] ID_Grad = new int[8]
        {
            Shader.PropertyToID("_LineGradient0"),
            Shader.PropertyToID("_LineGradient1"),
            Shader.PropertyToID("_LineGradient2"),
            Shader.PropertyToID("_LineGradient3"),
            Shader.PropertyToID("_LineGradient4"),
            Shader.PropertyToID("_LineGradient5"),
            Shader.PropertyToID("_LineGradient6"),
            Shader.PropertyToID("_LineGradient7"),
        };

        private void Awake()
        {
            // Create a per-instance material so multiple instances don't share state.
            var rawImage = GetComponent<RawImage>();
            if (rawImage.material == null)
            {
                Debug.LogError("[FloatingLinesRenderer] No material assigned to RawImage. " +
                               "Create a Material using the InformationString/FloatingLines shader and assign it.");
                enabled = false;
                return;
            }
            _mat = new Material(rawImage.material);
            rawImage.material = _mat;

            ApplyStaticUniforms();
        }

        private void OnEnable()
        {
            // Reset mouse to off-screen so there's no stale influence on re-enable.
            _currentMouse = new Vector2(-1000f, -1000f);
            _targetMouse = new Vector2(-1000f, -1000f);
            _currentInfluence = 0f;
            _targetInfluence = 0f;
            _currentParallax = Vector2.zero;
            _targetParallax = Vector2.zero;
        }

        private void Update()
        {
            UpdateResolution();
            UpdateMouseInteraction();
            UpdateParallax();
            PushDynamicUniforms();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void ApplyStaticUniforms()
        {
            _mat.SetFloat(ID_EnableTop, enableTop ? 1f : 0f);
            _mat.SetFloat(ID_EnableMiddle, enableMiddle ? 1f : 0f);
            _mat.SetFloat(ID_EnableBottom, enableBottom ? 1f : 0f);

            _mat.SetInt(ID_TopCount, topLineCount);
            _mat.SetInt(ID_MidCount, middleLineCount);
            _mat.SetInt(ID_BotCount, bottomLineCount);

            // Scale distances by 0.01 to match the React component's scaling
            _mat.SetFloat(ID_TopDist, topLineDistance * 0.01f);
            _mat.SetFloat(ID_MidDist, middleLineDistance * 0.01f);
            _mat.SetFloat(ID_BotDist, bottomLineDistance * 0.01f);

            _mat.SetVector(ID_TopPos, (Vector4)new Vector4(topWavePosition.x, topWavePosition.y, topWavePosition.z, 0));
            _mat.SetVector(ID_MidPos, (Vector4)new Vector4(middleWavePosition.x, middleWavePosition.y, middleWavePosition.z, 0));
            _mat.SetVector(ID_BotPos, (Vector4)new Vector4(bottomWavePosition.x, bottomWavePosition.y, bottomWavePosition.z, 0));

            _mat.SetFloat(ID_AnimSpeed, animationSpeed);
            _mat.SetFloat(ID_Interactive, interactive ? 1f : 0f);
            _mat.SetFloat(ID_BendR, bendRadius);
            _mat.SetFloat(ID_BendS, bendStrength);
            _mat.SetFloat(ID_Parallax, parallax ? 1f : 0f);

            ApplyGradient();
        }

        private void ApplyGradient()
        {
            int count = Mathf.Min(linesGradient.Count, 8);
            _mat.SetInt(ID_GradCount, count);
            for (int i = 0; i < count; i++)
                _mat.SetColor(ID_Grad[i], linesGradient[i]);
        }

        private void UpdateResolution()
        {
            _mat.SetVector(ID_Resolution, new Vector2(Screen.width, Screen.height));
        }

        private static Vector2 ReadMousePosition()
        {
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();

            return new Vector2(-1000f, -1000f);
        }

        private void UpdateMouseInteraction()
        {
            if (!interactive) return;

            Vector2 mp = ReadMousePosition();
            // Check if mouse is within the screen
            bool inScreen = mp.x >= 0 && mp.x <= Screen.width && mp.y >= 0 && mp.y <= Screen.height;
            if (inScreen)
            {
                // y is already bottom-origin in Unity, matching GLSL convention
                _targetMouse = mp;
                _targetInfluence = 1f;
            }
            else
            {
                _targetInfluence = 0f;
            }

            _currentMouse = Vector2.Lerp(_currentMouse, _targetMouse, mouseDamping);
            _currentInfluence = Mathf.Lerp(_currentInfluence, _targetInfluence, mouseDamping);

            _mat.SetVector(ID_Mouse, _currentMouse);
            _mat.SetFloat(ID_BendI, _currentInfluence);
        }

        private void UpdateParallax()
        {
            if (!parallax) return;

            Vector2 mp = ReadMousePosition();
            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            float offsetX = (mp.x - centerX) / Screen.width * parallaxStrength;
            float offsetY = (mp.y - centerY) / Screen.height * parallaxStrength;

            _targetParallax = new Vector2(offsetX, offsetY);
            _currentParallax = Vector2.Lerp(_currentParallax, _targetParallax, mouseDamping);

            _mat.SetVector(ID_ParallaxOff, new Vector4(_currentParallax.x, _currentParallax.y, 0, 0));
        }

        private void PushDynamicUniforms()
        {
            // _Time.y is handled in the shader via UnityCG built-in.
            // Resolution and mouse are already pushed above; nothing extra needed here
            // unless Inspector values changed at runtime.
        }

        private void OnValidate()
        {
            // Allow live Inspector tweaking in Play mode
            if (_mat != null)
                ApplyStaticUniforms();
        }

        private void OnDestroy()
        {
            if (_mat != null)
                Destroy(_mat);
        }
    }
}
