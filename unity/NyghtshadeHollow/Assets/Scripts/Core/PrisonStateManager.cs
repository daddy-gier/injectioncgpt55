using UnityEngine;
using System.Collections.Generic;

namespace NyghtshadeHollow.Core
{
    // Tracks global prison state flags, alert level, and discipline records
    public class PrisonStateManager : MonoBehaviour
    {
        public enum AlertLevel { Green, Yellow, Orange, Red, Lockdown }

        public AlertLevel CurrentAlert { get; private set; } = AlertLevel.Green;
        public int PlayerReputation { get; private set; } = 0;
        public bool IsLockdown => CurrentAlert == AlertLevel.Lockdown;

        private readonly Dictionary<string, bool> _worldFlags = new();
        private readonly Dictionary<string, int> _incidentLog = new();

        public static event System.Action<AlertLevel> OnAlertChanged;
        public static event System.Action<string, bool> OnFlagChanged;

        public void SetFlag(string key, bool value)
        {
            _worldFlags[key] = value;
            OnFlagChanged?.Invoke(key, value);
        }

        public bool GetFlag(string key) =>
            _worldFlags.TryGetValue(key, out bool v) && v;

        public void EscalateAlert()
        {
            if (CurrentAlert < AlertLevel.Lockdown)
            {
                CurrentAlert++;
                OnAlertChanged?.Invoke(CurrentAlert);
            }
        }

        public void DeescalateAlert()
        {
            if (CurrentAlert > AlertLevel.Green)
            {
                CurrentAlert--;
                OnAlertChanged?.Invoke(CurrentAlert);
            }
        }

        public void LogIncident(string npcId, int severity)
        {
            _incidentLog[npcId] = (_incidentLog.TryGetValue(npcId, out int s) ? s : 0) + severity;
            if (_incidentLog[npcId] >= 10) EscalateAlert();
        }

        public void ModifyReputation(int delta)
        {
            PlayerReputation = Mathf.Clamp(PlayerReputation + delta, -100, 100);
        }

        public Dictionary<string, bool> GetAllFlags() => new(_worldFlags);
        public Dictionary<string, int> GetAllIncidents() => new(_incidentLog);
    }
}
