using UnityEngine;

namespace Game
{
    // Plays an idle strip and a run strip on the SpriteRenderer. It plays the run strip while
    // the character moves. A character with no strips just hops while it moves.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimator : MonoBehaviour
    {
        public Texture2D idleStrip;
        public Texture2D runStrip;
        public int idleCellWidth;
        public int runCellWidth;
        public Vector2 idlePivot = new Vector2(0.5f, 0f);
        public Vector2 runPivot = new Vector2(0.5f, 0f);
        public float framesPerSecond = 8f;
        public bool hopWhenMoving;          // for sprites with no frames
        public bool facesRight = true;

        private SpriteRenderer spriteRenderer;
        private Sprite[] idleFrames;
        private Sprite[] runFrames;
        private Transform body;
        private Vector3 lastPosition;
        private Vector3 basePosition;
        private float movingFor;
        private bool wasMoving;
        private float clock;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            body = transform.parent;
            lastPosition = body.position;
            basePosition = transform.localPosition;

            idleFrames = CutFrames(idleStrip, idleCellWidth, idlePivot);
            runFrames = CutFrames(runStrip, runCellWidth, runPivot);
        }

        private static Sprite[] CutFrames(Texture2D strip, int cellWidth, Vector2 pivot)
        {
            if (strip == null || cellWidth <= 0)
                return null;

            Sprite[] frames = new Sprite[strip.width / cellWidth];
            for (int i = 0; i < frames.Length; i++)
                // A full rectangle mesh, because the tight mesh cuts holes in very small pixel art
                frames[i] = Sprite.Create(strip, new Rect(i * cellWidth, 0f, cellWidth, strip.height), pivot, 100f, 0, SpriteMeshType.FullRect);
            return frames;
        }

        private void Update()
        {
            Vector3 step = body.position - lastPosition;
            lastPosition = body.position;

            // Count as moving for a moment after the last step, so the animation does not flicker
            if (step.sqrMagnitude > 0.00001f)
            {
                movingFor = 0.15f;
                Flip(step);
            }
            else
            {
                movingFor -= Time.deltaTime;
            }
            bool moving = movingFor > 0f;

            clock += Time.deltaTime * framesPerSecond;

            // Each strip starts from its first frame, so the loop is the same every time
            if (moving != wasMoving)
                clock = 0f;
            wasMoving = moving;

            Sprite[] frames = moving && runFrames != null ? runFrames : idleFrames;
            if (frames != null)
                spriteRenderer.sprite = frames[(int)clock % frames.Length];

            if (hopWhenMoving)
            {
                Vector3 position = basePosition;
                if (moving)
                    position.y += Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.07f;
                transform.localPosition = position;
            }
        }

        // The sprite looks left or right on the screen depending on where it walks
        private void Flip(Vector3 step)
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            // Walking straight towards or away from the camera keeps the current side
            float side = Vector3.Dot(step.normalized, camera.transform.right);
            if (Mathf.Abs(side) > 0.3f)
                spriteRenderer.flipX = facesRight ? side < 0f : side > 0f;
        }
    }
}
