using UnityEngine;

namespace Game
{
    // Makes a light flicker with Perlin noise
    [RequireComponent(typeof(Light))]
    public class TorchFlicker : MonoBehaviour
    {
        public float amount = 0.25f;    // how much the intensity changes, as a fraction of the base intensity
        public float speed = 3f;

        private Light torchLight;
        private float baseIntensity;
        private float offset;

        private void Awake()
        {
            torchLight = GetComponent<Light>();
            baseIntensity = torchLight.intensity;

            // Every torch starts somewhere else on the noise, so they don't flicker together
            offset = Random.value * 100f;
        }

        private void Update()
        {
            float noise = Mathf.PerlinNoise(offset, Time.time * speed);
            torchLight.intensity = baseIntensity * (1f + (noise - 0.5f) * 2f * amount);
        }
    }
}
