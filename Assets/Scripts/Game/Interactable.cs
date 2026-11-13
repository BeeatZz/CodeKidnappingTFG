using UnityEngine;

namespace Game
{
    // Something the player can look at and press E on
    public abstract class Interactable : MonoBehaviour
    {
        public abstract string GetPrompt();

        public abstract void Interact();
    }
}
