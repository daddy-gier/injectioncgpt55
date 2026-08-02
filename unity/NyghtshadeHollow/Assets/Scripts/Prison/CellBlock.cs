using UnityEngine;
using System.Collections.Generic;
using NyghtshadeHollow.Core;

namespace NyghtshadeHollow.Prison
{
    // Manages a cell block: doors, occupants, lockdown state
    public class CellBlock : MonoBehaviour
    {
        [Header("Block Config")]
        public string BlockID;
        public string BlockName;

        [Header("Doors")]
        public PrisonDoor[] CellDoors;
        public PrisonDoor MainGate;

        [Header("Occupants")]
        public List<GameObject> Inmates = new();

        private bool _isInLockdown;

        private void OnEnable()
        {
            PrisonStateManager.OnAlertChanged += OnAlertChanged;
        }

        private void OnDisable()
        {
            PrisonStateManager.OnAlertChanged -= OnAlertChanged;
        }

        private void OnAlertChanged(PrisonStateManager.AlertLevel level)
        {
            bool lockdown = level == PrisonStateManager.AlertLevel.Lockdown ||
                            level == PrisonStateManager.AlertLevel.Red;
            SetLockdown(lockdown);
        }

        public void SetLockdown(bool locked)
        {
            _isInLockdown = locked;
            foreach (var door in CellDoors)
            {
                if (locked) door.ForceLock();
            }

            if (locked) MainGate?.ForceLock();
        }

        public void AddInmate(GameObject inmate)
        {
            if (!Inmates.Contains(inmate)) Inmates.Add(inmate);
        }

        public void RemoveInmate(GameObject inmate)
        {
            Inmates.Remove(inmate);
        }

        public bool IsLockdown => _isInLockdown;
        public int OccupantCount => Inmates.Count;
    }
}
