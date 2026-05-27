using System.Collections.Generic;
using InformationString.Core;
using InformationString.Input;
using UnityEngine;

namespace InformationString.Visual
{
    /// <summary>
    /// Manages idle-screen visibility based on connection state.
    ///
    /// The idle screen (FloatingLines background + DecryptedText title) is shown
    /// whenever there are zero published connections (including Sustained info-only),
    /// and hidden as soon as any connection is established.
    ///
    /// On Awake the idle screen is shown by default (startup = no connections yet).
    ///
    /// Scene setup:
    ///   Empty GameObject (anywhere in scene hierarchy)
    ///   └─ this component
    ///        idleCanvas  → drag the IdleCanvas GameObject here
    /// </summary>
    public class IdleScreenController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The root Canvas (or any root GameObject) that contains the FloatingLines " +
                 "RawImage and the DecryptedText TextMeshPro. Will be shown/hidden based on " +
                 "connection state.")]
        public GameObject idleCanvas;

        [Header("Debug")]
        [Tooltip("Log connection-count changes to the console.")]
        public bool debugLog = false;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            // Default: visible — no connections at startup
            SetIdleVisible(true);
        }

        private void OnEnable()
        {
            GameEvents.OnValidConnectionsUpdated += HandleConnectionsUpdated;
        }

        private void OnDisable()
        {
            GameEvents.OnValidConnectionsUpdated -= HandleConnectionsUpdated;
        }

        // ── Event handler ─────────────────────────────────────────────────────────

        private void HandleConnectionsUpdated(List<ValidConnection> connections)
        {
            bool idle = connections == null || connections.Count == 0;

            if (debugLog)
                Debug.Log($"[IdleScreenController] connections={connections?.Count ?? 0} → idle={idle}");

            SetIdleVisible(idle);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void SetIdleVisible(bool visible)
        {
            if (idleCanvas == null)
            {
                Debug.LogWarning("[IdleScreenController] idleCanvas reference is not set.");
                return;
            }

            idleCanvas.SetActive(visible);
        }
    }
}
