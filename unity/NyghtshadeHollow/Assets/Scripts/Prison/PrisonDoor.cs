using UnityEngine;
using NyghtshadeHollow.Characters;
using NyghtshadeHollow.Core;
using NyghtshadeHollow.Faction;

namespace NyghtshadeHollow.Prison
{
    public class PrisonDoor : MonoBehaviour, IInteractable
    {
        [Header("Door Settings")]
        public string DoorID;
        public bool IsLocked = true;
        public DoorAccessLevel RequiredAccess;
        public string RequiredFlag;

        [Header("Animation")]
        public Animator DoorAnimator;
        public AudioSource DoorAudio;
        public AudioClip OpenClip;
        public AudioClip LockedClip;

        private bool _isOpen;

        public enum DoorAccessLevel { Any, Inmate, Guard, Admin, KeyCard }

        public bool CanInteract(GameObject initiator)
        {
            if (!IsLocked) return true;
            var state = GameManager.Instance?.PrisonState;
            if (state == null) return false;

            if (!string.IsNullOrEmpty(RequiredFlag) && !state.GetFlag(RequiredFlag)) return false;

            return RequiredAccess switch
            {
                DoorAccessLevel.Any     => true,
                DoorAccessLevel.KeyCard => GameManager.Instance?.Inventory.HasItem("KEY_" + DoorID) ?? false,
                DoorAccessLevel.Guard   => GameManager.Instance?.Factions.GetStanding(ENHFaction.AdminCore) >= 30,
                DoorAccessLevel.Admin   => GameManager.Instance?.Factions.GetStanding(ENHFaction.AdminCore) >= 60,
                _                       => false
            };
        }

        public string GetInteractionLabel()
        {
            if (_isOpen) return "Close door";
            if (!CanInteract(null)) return "Locked";
            return "Open door";
        }

        public void Interact(GameObject initiator)
        {
            if (!CanInteract(initiator))
            {
                DoorAudio?.PlayOneShot(LockedClip);
                return;
            }
            _isOpen = !_isOpen;
            DoorAnimator?.SetBool("IsOpen", _isOpen);
            DoorAudio?.PlayOneShot(OpenClip);
        }

        public void ForceOpen()
        {
            _isOpen = true;
            DoorAnimator?.SetBool("IsOpen", true);
        }

        public void ForceLock()
        {
            _isOpen = false;
            IsLocked = true;
            DoorAnimator?.SetBool("IsOpen", false);
        }
    }
}
