using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.RecyclableScroller.Scripts
{
    public enum PullRefreshState
    {
        Idle,
        Pulling,
        ReadyToRefresh,
        Loading
    }

    public class PullToRefreshUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private Image loadingSpinner;

        private Dictionary<PullRefreshState, string> stateLabels;
        private PullRefreshState currentState = PullRefreshState.Idle;
        private float spinnerRotationSpeed = 360f;
        private RectTransform cachedSpinnerRect;

        private static readonly Dictionary<PullRefreshState, string> DEFAULT_LABELS = new()
        {
            { PullRefreshState.Idle, "Release to refresh" },
            { PullRefreshState.Pulling, "Pull to refresh" },
            { PullRefreshState.ReadyToRefresh, "Release to refresh" },
            { PullRefreshState.Loading, "Loading..." }
        };

        private void Awake()
        {
            stateLabels = new Dictionary<PullRefreshState, string>(DEFAULT_LABELS);

            if (loadingSpinner is not null)
                cachedSpinnerRect = loadingSpinner.GetComponent<RectTransform>();
        }

        private void LateUpdate()
        {
            if (currentState == PullRefreshState.Loading && cachedSpinnerRect is not null)
                cachedSpinnerRect.Rotate(0, 0, -spinnerRotationSpeed * Time.deltaTime);
        }

        public void SetStateLabel(PullRefreshState state, string label)
        {
            if (string.IsNullOrEmpty(label))
                return;

            stateLabels[state] = label;
        }

        public void SetAllLabels(Dictionary<PullRefreshState, string> labels)
        {
            if (labels is null)
                return;

            stateLabels.Clear();
            foreach (KeyValuePair<PullRefreshState, string> entry in labels)
                stateLabels[entry.Key] = entry.Value;
        }

        public void SetSpinnerRotationSpeed(float degreesPerSecond)
        {
            spinnerRotationSpeed = degreesPerSecond;
        }

        public void SetPullState(PullRefreshState newState, float pullProgress = 0f)
        {
            if (currentState == newState)
                return;

            currentState = newState;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (statusLabel is not null && stateLabels.TryGetValue(currentState, out string label))
                statusLabel.text = label;

            if (loadingSpinner is not null)
                loadingSpinner.gameObject.SetActive(currentState == PullRefreshState.Loading);
        }

        public PullRefreshState GetCurrentState()
            => currentState;

        private void OnDestroy()
        {
            stateLabels?.Clear();
        }
    }
}

