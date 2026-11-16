using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIScroller.Samples.Extras
{
    public enum PullIndicatorState
    {
        Idle,
        Pulling,
        ReadyToRefresh,
        Loading
    }

    /// <summary>A status label and spinner for pull to refresh. Your scroller sets the state.</summary>
    public class PullIndicator : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private Image loadingSpinner;

        private Dictionary<PullIndicatorState, string> stateLabels;
        private PullIndicatorState currentState = PullIndicatorState.Idle;
        private float spinnerRotationSpeed = 360f;
        private RectTransform cachedSpinnerRect;

        private static readonly Dictionary<PullIndicatorState, string> DEFAULT_LABELS = new()
        {
            { PullIndicatorState.Idle, "Release to refresh" },
            { PullIndicatorState.Pulling, "Pull to refresh" },
            { PullIndicatorState.ReadyToRefresh, "Release to refresh" },
            { PullIndicatorState.Loading, "Loading..." }
        };

        private void Awake()
        {
            stateLabels = new Dictionary<PullIndicatorState, string>(DEFAULT_LABELS);

            if (loadingSpinner is not null)
                cachedSpinnerRect = loadingSpinner.GetComponent<RectTransform>();
        }

        private void LateUpdate()
        {
            if (currentState == PullIndicatorState.Loading && cachedSpinnerRect is not null)
                cachedSpinnerRect.Rotate(0, 0, -spinnerRotationSpeed * Time.deltaTime);
        }

        public void SetStateLabel(PullIndicatorState state, string label)
        {
            if (string.IsNullOrEmpty(label))
                return;

            stateLabels[state] = label;
        }

        public void SetAllLabels(Dictionary<PullIndicatorState, string> labels)
        {
            if (labels is null)
                return;

            stateLabels.Clear();
            foreach (KeyValuePair<PullIndicatorState, string> entry in labels)
                stateLabels[entry.Key] = entry.Value;
        }

        public void SetSpinnerRotationSpeed(float degreesPerSecond)
        {
            spinnerRotationSpeed = degreesPerSecond;
        }

        public void SetPullState(PullIndicatorState newState, float pullProgress = 0f)
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
                loadingSpinner.gameObject.SetActive(currentState == PullIndicatorState.Loading);
        }

        public PullIndicatorState GetCurrentState()
            => currentState;

        private void OnDestroy()
        {
            stateLabels?.Clear();
        }
    }
}

