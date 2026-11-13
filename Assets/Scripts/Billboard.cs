using UnityEngine;

namespace Game
{
    // Turns the object towards every camera that renders it, so the scrying glass cameras
    // see it facing them too. Seen from high up, a sprite that stays upright only leans back a little.
    public class Billboard : MonoBehaviour
    {
        public bool lockYAxis = true;       // only turn around Y, so the sprite stays upright
        public Camera targetCamera;         // leave empty to face whichever camera renders it

        [Header("Looking from above (only with lockYAxis)")]
        public float tiltStart = 15f;       // degrees of camera height that are ignored
        public float tiltAmount = 0.5f;     // how much of the rest the sprite leans
        public float maxTilt = 30f;

        private float centerHeight;         // height of the middle of the sprite, where the camera angle is measured
        private Camera.CameraCallback faceCamera;

        private void Awake()
        {
            SpriteRenderer sprite = GetComponent<SpriteRenderer>();
            if (sprite != null && sprite.sprite != null)
                centerHeight = sprite.sprite.bounds.center.y * transform.lossyScale.y;

            faceCamera = Face;
        }

        private void OnEnable()
        {
            if (faceCamera == null)
                faceCamera = Face;
            Camera.onPreCull += faceCamera;
        }

        private void OnDisable()
        {
            Camera.onPreCull -= faceCamera;
        }

        // Called right before each camera draws, so each one sees the sprite turned towards it
        private void Face(Camera camera)
        {
            if (camera.cameraType != CameraType.Game)
                return;
            if (targetCamera != null && camera != targetCamera)
                return;

            Vector3 forward = transform.position - camera.transform.position;
            if (!lockYAxis)
            {
                if (forward.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(forward);
                return;
            }

            Vector3 flat = forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f)
                return;

            // How high the camera is above the middle of the sprite, as an angle
            float height = camera.transform.position.y - (transform.position.y + centerHeight);
            float angle = Mathf.Atan2(height, flat.magnitude) * Mathf.Rad2Deg;
            float tilt = Mathf.Clamp((angle - tiltStart) * tiltAmount, 0f, maxTilt);

            transform.rotation = Quaternion.LookRotation(flat) * Quaternion.Euler(tilt, 0f, 0f);
        }
    }
}
