using UnityEngine;

namespace Game
{
    // The look of the picture: a soft glow around bright things, outlines, warm colours and a vignette.
    // The Built-in pipeline has no post processing of its own, so this does it on the camera.
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class KitchenPostEffect : MonoBehaviour
    {
        public Shader shader;

        [Header("Glow")]
        public float bloomThreshold = 0.8f;
        public float bloomIntensity = 0.4f;

        [Header("Colours")]
        public float saturation = 1.2f;
        public float contrast = 1.1f;
        public Color tint = new Color(1f, 1f, 1f);

        [Header("Vignette")]
        [Range(0f, 1f)] public float vignetteStart = 0.55f;
        [Range(0f, 1f)] public float vignetteStrength = 0.6f;

        [Header("Outlines")]
        public Color outlineColor = new Color(0.08f, 0.04f, 0.08f);
        [Range(0f, 1f)] public float outlineStrength = 0.85f;
        public float outlineThickness = 1f;
        public float outlineThreshold = 0.06f;      // how big a jump in distance makes a line, compared to the distance

        private Material material;

        private void OnEnable()
        {
            // The outlines read the depth of the scene
            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;

            if (shader != null)
                material = new Material(shader);
        }

        private void OnDisable()
        {
            if (material != null)
                DestroyImmediate(material);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (material == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            material.SetFloat("_Threshold", bloomThreshold);
            material.SetFloat("_BloomIntensity", bloomIntensity);
            material.SetFloat("_Saturation", saturation);
            material.SetFloat("_Contrast", contrast);
            material.SetColor("_Tint", tint);
            material.SetFloat("_VignetteStart", vignetteStart);
            material.SetFloat("_VignetteStrength", vignetteStrength);
            material.SetColor("_OutlineColor", outlineColor);
            material.SetFloat("_OutlineStrength", outlineStrength);
            material.SetFloat("_OutlineThickness", outlineThickness);
            material.SetFloat("_OutlineThreshold", outlineThreshold);

            // The glow is made at half size, which also makes it softer and cheaper
            int width = source.width / 2;
            int height = source.height / 2;
            RenderTexture bright = RenderTexture.GetTemporary(width, height, 0, source.format);
            RenderTexture blurred = RenderTexture.GetTemporary(width, height, 0, source.format);

            Graphics.Blit(source, bright, material, 0);

            material.SetVector("_BlurDirection", new Vector2(1f, 0f));
            Graphics.Blit(bright, blurred, material, 1);
            material.SetVector("_BlurDirection", new Vector2(0f, 1f));
            Graphics.Blit(blurred, bright, material, 1);

            material.SetTexture("_BloomTex", bright);
            Graphics.Blit(source, destination, material, 2);

            RenderTexture.ReleaseTemporary(bright);
            RenderTexture.ReleaseTemporary(blurred);
        }
    }
}
