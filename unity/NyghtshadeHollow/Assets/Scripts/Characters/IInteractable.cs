namespace NyghtshadeHollow.Characters
{
    public interface IInteractable
    {
        bool CanInteract(UnityEngine.GameObject initiator);
        string GetInteractionLabel();
        void Interact(UnityEngine.GameObject initiator);
    }
}
