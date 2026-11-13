using UnityEngine;

namespace Game
{
    // Looks at what is in front of the camera and uses it when the player presses E
    public class PlayerInteractor : MonoBehaviour
    {
        public PlayerController player;
        public Camera viewCamera;
        public float reach = 4f;

        // What the player is looking at right now, for the HUD
        public Interactable Current { get; private set; }

        private void Update()
        {
            Current = null;
            if (!player.InputEnabled)
                return;

            Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, reach))
                Current = hit.collider.GetComponentInParent<Interactable>();

            if (Current != null && Input.GetKeyDown(KeyCode.E))
                Current.Interact();
        }
    }
}
