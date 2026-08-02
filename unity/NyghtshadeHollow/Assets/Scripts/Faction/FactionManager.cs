using UnityEngine;
using System.Collections.Generic;

namespace NyghtshadeHollow.Faction
{
    public enum ENHFaction
    {
        AdminCore,
        Medical,
        Inmates,
        GRAVE,
        Syndicates,
        Outsiders,
        Neutral
    }

    public class FactionManager : MonoBehaviour
    {
        // Standing: -100 (hated) to +100 (ally)
        private readonly Dictionary<ENHFaction, int> _standing = new()
        {
            { ENHFaction.AdminCore,  0  },
            { ENHFaction.Medical,    10 },
            { ENHFaction.Inmates,    20 },
            { ENHFaction.GRAVE,     -50 },
            { ENHFaction.Syndicates,-10 },
            { ENHFaction.Outsiders,  0  },
            { ENHFaction.Neutral,    0  },
        };

        public static event System.Action<ENHFaction, int> OnStandingChanged;

        public int GetStanding(ENHFaction faction) =>
            _standing.TryGetValue(faction, out int v) ? v : 0;

        public void ModifyStanding(ENHFaction faction, int delta)
        {
            _standing[faction] = Mathf.Clamp(_standing.GetValueOrDefault(faction) + delta, -100, 100);
            OnStandingChanged?.Invoke(faction, _standing[faction]);

            // Opposing faction reactions
            ApplyRipple(faction, delta);
        }

        private void ApplyRipple(ENHFaction changed, int delta)
        {
            // Gaining favor with Inmates hurts AdminCore slightly
            if (changed == ENHFaction.Inmates && delta > 0)
                _standing[ENHFaction.AdminCore] = Mathf.Clamp(_standing[ENHFaction.AdminCore] - delta / 2, -100, 100);

            if (changed == ENHFaction.AdminCore && delta > 0)
                _standing[ENHFaction.Inmates] = Mathf.Clamp(_standing[ENHFaction.Inmates] - delta / 2, -100, 100);

            if (changed == ENHFaction.Syndicates && delta > 0)
                _standing[ENHFaction.GRAVE] = Mathf.Clamp(_standing[ENHFaction.GRAVE] - delta / 3, -100, 100);
        }

        public string GetStandingLabel(ENHFaction faction)
        {
            int s = GetStanding(faction);
            return s switch
            {
                >= 75  => "Allied",
                >= 40  => "Friendly",
                >= 10  => "Neutral",
                >= -10 => "Indifferent",
                >= -40 => "Hostile",
                _      => "Enemy"
            };
        }

        public Dictionary<ENHFaction, int> GetAllStandings() => new(_standing);
    }
}
