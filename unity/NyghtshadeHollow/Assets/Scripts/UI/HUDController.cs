using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NyghtshadeHollow.Core;
using NyghtshadeHollow.Faction;

namespace NyghtshadeHollow.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Alert")]
        public Image AlertBar;
        public TextMeshProUGUI AlertLabel;
        public Color[] AlertColors; // Green, Yellow, Orange, Red, Red

        [Header("Faction Bars")]
        public Slider AdminStanding;
        public Slider InmateStanding;
        public Slider SyndicateStanding;
        public Slider GRAVEStanding;

        [Header("Suspicion")]
        public Slider SuspicionMeter;
        public Image SuspicionFill;

        [Header("Journal Badge")]
        public GameObject JournalBadge;
        public TextMeshProUGUI UnreadCount;

        [Header("Interaction Prompt")]
        public GameObject InteractionPrompt;
        public TextMeshProUGUI InteractionLabel;

        private void OnEnable()
        {
            PrisonStateManager.OnAlertChanged += OnAlertChanged;
            FactionManager.OnStandingChanged  += OnStandingChanged;
        }

        private void OnDisable()
        {
            PrisonStateManager.OnAlertChanged -= OnAlertChanged;
            FactionManager.OnStandingChanged  -= OnStandingChanged;
        }

        private void Start()
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            OnAlertChanged(gm.PrisonState.CurrentAlert);

            AdminStanding.value    = Remap(gm.Factions.GetStanding(ENHFaction.AdminCore));
            InmateStanding.value   = Remap(gm.Factions.GetStanding(ENHFaction.Inmates));
            SyndicateStanding.value= Remap(gm.Factions.GetStanding(ENHFaction.Syndicates));
            GRAVEStanding.value    = Remap(gm.Factions.GetStanding(ENHFaction.GRAVE));
        }

        private void OnAlertChanged(PrisonStateManager.AlertLevel level)
        {
            int idx = (int)level;
            AlertLabel.text = level.ToString().ToUpper();
            if (AlertColors != null && idx < AlertColors.Length)
                AlertBar.color = AlertColors[idx];
        }

        private void OnStandingChanged(ENHFaction faction, int value)
        {
            float v = Remap(value);
            switch (faction)
            {
                case ENHFaction.AdminCore:   AdminStanding.value    = v; break;
                case ENHFaction.Inmates:     InmateStanding.value   = v; break;
                case ENHFaction.Syndicates:  SyndicateStanding.value= v; break;
                case ENHFaction.GRAVE:       GRAVEStanding.value    = v; break;
            }
        }

        public void ShowInteractionPrompt(string label)
        {
            InteractionPrompt.SetActive(true);
            InteractionLabel.text = label;
        }

        public void HideInteractionPrompt()
        {
            InteractionPrompt.SetActive(false);
        }

        public void UpdateSuspicion(float value)
        {
            SuspicionMeter.value = value / 100f;
            SuspicionFill.color = Color.Lerp(Color.green, Color.red, value / 100f);
        }

        public void ShowJournalBadge(int unread)
        {
            JournalBadge.SetActive(unread > 0);
            UnreadCount.text = unread.ToString();
        }

        private float Remap(int standing) => (standing + 100f) / 200f;
    }
}
