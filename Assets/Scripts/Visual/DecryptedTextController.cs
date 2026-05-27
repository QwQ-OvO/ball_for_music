using System.Collections;
using TMPro;
using UnityEngine;

namespace InformationString.Visual
{
    /// <summary>
    /// Mirrors the React Bits DecryptedText component behaviour for Unity TextMeshPro.
    ///
    /// The effect runs as a continuous loop:
    ///   1. All characters are scrambled instantly.
    ///   2. Characters are revealed one-by-one from the centre outward.
    ///   3. The fully-revealed text is held for <see cref="revealHoldSeconds"/> seconds.
    ///   4. Repeat indefinitely (matching animateOn="view" with continuous loop intent).
    ///
    /// Scene setup:
    ///   Canvas
    ///   └─ TextMeshProUGUI (centred, large font, white colour)
    ///      └─ this component
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class DecryptedTextController : MonoBehaviour
    {
        [Header("Content")]
        [Tooltip("The text to decrypt-animate. Changing this at runtime restarts the loop.")]
        public string text = "Explore Your Music";

        [Header("Timing")]
        [Tooltip("Seconds between each character reveal step.")]
        [Min(0.001f)] public float revealStepSeconds = 0.05f;

        [Tooltip("Seconds to hold the fully-revealed text before restarting.")]
        [Min(0f)] public float revealHoldSeconds = 1.5f;

        [Tooltip("Seconds to hold the fully-scrambled text before starting the reveal.")]
        [Min(0f)] public float scrambleHoldSeconds = 0.1f;

        [Header("Scramble Characters")]
        [Tooltip("Pool of characters used for scrambling. Matches the React Bits default.")]
        public string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!@#$%^&*()_+";

        // ── Private ───────────────────────────────────────────────────────────────

        private TextMeshProUGUI _label;
        private Coroutine       _loop;
        private char[]          _charArray;   // working buffer
        private bool[]          _revealed;    // which indices are locked to original char

        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            StartLoop();
        }

        private void OnDisable()
        {
            StopLoop();
            // Restore plain text so it doesn't stay mid-scramble when re-enabled
            if (_label != null) _label.text = text;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Immediately restarts the decrypt loop (e.g. after changing <see cref="text"/>).</summary>
        public void Restart()
        {
            StopLoop();
            StartLoop();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void StartLoop()
        {
            StopLoop();
            _loop = StartCoroutine(DecryptLoop());
        }

        private void StopLoop()
        {
            if (_loop != null)
            {
                StopCoroutine(_loop);
                _loop = null;
            }
        }

        private IEnumerator DecryptLoop()
        {
            while (true)
            {
                string source = text;
                int    len    = source.Length;

                _charArray = source.ToCharArray();
                _revealed  = new bool[len];

                // Step 1 — scramble everything instantly
                ScrambleAll(source);
                yield return new WaitForSeconds(scrambleHoldSeconds);

                // Step 2 — build centre-out reveal order
                int[] order = CentreOutOrder(len);

                // Step 3 — reveal one character per tick
                foreach (int idx in order)
                {
                    _revealed[idx] = true;
                    _charArray[idx] = source[idx];
                    ScrambleUnrevealed(source);
                    yield return new WaitForSeconds(revealStepSeconds);
                }

                // Step 4 — hold fully-revealed text
                _label.text = source;
                yield return new WaitForSeconds(revealHoldSeconds);
            }
        }

        /// <summary>Randomise every non-space character in the working buffer.</summary>
        private void ScrambleAll(string source)
        {
            for (int i = 0; i < source.Length; i++)
                _charArray[i] = source[i] == ' ' ? ' ' : RandomChar();

            _label.text = new string(_charArray);
        }

        /// <summary>Randomise only the characters not yet revealed.</summary>
        private void ScrambleUnrevealed(string source)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (_revealed[i]) continue;
                _charArray[i] = source[i] == ' ' ? ' ' : RandomChar();
            }
            _label.text = new string(_charArray);
        }

        private char RandomChar()
        {
            if (characters.Length == 0) return '?';
            return characters[Random.Range(0, characters.Length)];
        }

        /// <summary>
        /// Returns indices in centre-outward order, matching React Bits revealDirection="center".
        /// e.g. for len=5: [2, 1, 3, 0, 4]
        /// </summary>
        private static int[] CentreOutOrder(int len)
        {
            int[] order  = new int[len];
            int   middle = len / 2;
            int   pos    = 0;
            int   offset = 0;

            while (pos < len)
            {
                if (offset % 2 == 0)
                {
                    int idx = middle + offset / 2;
                    if (idx >= 0 && idx < len) order[pos++] = idx;
                }
                else
                {
                    int idx = middle - (offset + 1) / 2;
                    if (idx >= 0 && idx < len) order[pos++] = idx;
                }
                offset++;
            }
            return order;
        }
    }
}
