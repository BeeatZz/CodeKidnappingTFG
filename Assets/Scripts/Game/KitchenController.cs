using System.Collections;
using CodeRunner.Bridge;
using Computer;
using ComputerUI;
using Mission;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // Runs the Feed the Hellhound scene: the briefing, the animations the imp plays,
    // the dinner bell that resets the kitchen and the ending.
    public class KitchenController : MonoBehaviour
    {
        public Transform[] sacks;           // sack01 to sack05
        public Transform hound;
        public Transform imp;
        public Transform goblinCook;
        public Transform demonLord;
        public Transform bell;              // the part of the bell that swings
        public Transform doorInside;        // just inside the door
        public Transform doorOutside;
        public PlayerController player;
        public CanvasComputer computer;
        public DialogueBox dialogue;
        public GameObject resultPanel;      // the "Mission complete" box
        public Text resultText;
        public float walkSpeed = 3.5f;

        private TestSceneSetup setup;
        private Kitchen kitchen;

        // How the scene looked when it loaded, so a reset can put it all back
        private Vector3[] sackPositions;
        private Quaternion[] sackRotations;
        private Transform[] sackParents;
        private bool[] sackActive;
        private Vector3 impStart;
        private Vector3 houndStart;

        private SpriteRenderer houndSprite;
        private SpriteRenderer impSprite;
        private Vector3 houndSpriteScale;
        private Vector3 houndSpritePosition;

        private int busy;                   // animations that are playing
        private bool ringing;
        private bool skipRequested;
        private bool endingRunning;
        private bool storyDone;
        private bool resultShown;
        private float resultUntil;
        private float chompTimer;
        private float petTimer;

        // The bell only works when nothing else is going on
        public bool CanReset
        {
            get
            {
                return busy == 0 && !ringing && !endingRunning && !dialogue.IsOpen && setup.Session.CurrentLine == 0;
            }
        }

        private void Start()
        {
            setup = TestSceneSetup.Instance;
            kitchen = setup.Kitchen;

            sackPositions = new Vector3[sacks.Length];
            sackRotations = new Quaternion[sacks.Length];
            sackParents = new Transform[sacks.Length];
            sackActive = new bool[sacks.Length];
            for (int i = 0; i < sacks.Length; i++)
            {
                sackPositions[i] = sacks[i].position;
                sackRotations[i] = sacks[i].rotation;
                sackParents[i] = sacks[i].parent;
                sackActive[i] = sacks[i].gameObject.activeSelf;

                // The tag always shows the label, never the contents
                Transform tag = sacks[i].Find("Tag");
                if (tag != null)
                    tag.GetComponent<TextMesh>().text = kitchen.Sacks[i].Label;
            }
            impStart = imp.position;
            houndStart = hound.position;

            houndSprite = hound.Find("Sprite").GetComponent<SpriteRenderer>();
            impSprite = imp.Find("Sprite").GetComponent<SpriteRenderer>();
            houndSpriteScale = houndSprite.transform.localScale;
            houndSpritePosition = houndSprite.transform.localPosition;

            goblinCook.gameObject.SetActive(false);
            kitchen.Imp.GiveResolved += OnGiveResolved;

            StartCoroutine(Briefing());
        }

        private void OnDestroy()
        {
            if (kitchen != null)
                kitchen.Imp.GiveResolved -= OnGiveResolved;
        }

        private void Update()
        {
            UpdateCharacters();

            if (kitchen.IsMissionComplete && !resultShown && CanReset && !computer.IsOpen)
                StartCoroutine(Ending());

            UpdateResultPanel();
        }

        // ---------- How the characters look ----------

        private void UpdateCharacters()
        {
            bool asleep = kitchen.Hound.IsAsleep;
            houndSprite.color = asleep ? new Color(0.55f, 0.55f, 0.75f) : Color.white;
            impSprite.color = kitchen.Imp.IsCoveredInFood ? new Color(0.75f, 0.5f, 0.25f) : Color.white;

            Transform sprite = houndSprite.transform;
            Vector3 scale = houndSpriteScale;
            Vector3 position = houndSpritePosition;

            if (asleep)
            {
                scale.y *= 1f + Mathf.Sin(Time.time * 1.5f) * 0.03f;
            }
            else
            {
                // Awake: it breathes, shifts its weight and paces on the spot
                float breath = Mathf.Sin(Time.time * 3f);
                scale.y *= 1f + breath * 0.025f;
                scale.x *= 1f - breath * 0.012f;
                position.x += Mathf.Sin(Time.time * 1.3f) * 0.04f;
                position.y += (Mathf.Sin(Time.time * 6f) + 1f) * 0.012f;
            }

            if (chompTimer > 0f)
            {
                chompTimer -= Time.deltaTime;
                scale.y *= 1f + Mathf.Sin(chompTimer * 25f) * 0.08f;
            }

            if (petTimer > 0f)
            {
                petTimer -= Time.deltaTime;
                position.x += Mathf.Sin(Time.time * 18f) * 0.06f;
                position.y += Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.08f;
            }

            sprite.localScale = scale;
            sprite.localPosition = position;
        }

        // ---------- Walking ----------

        private bool SkipPressed()
        {
            return Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
        }

        private IEnumerator Walk(Transform who, Vector3 destination, float stopDistance, float speed = 0f)
        {
            if (speed <= 0f)
                speed = walkSpeed;
            destination.y = who.position.y;

            while (Vector3.Distance(who.position, destination) > stopDistance)
            {
                if (ringing && SkipPressed())
                    skipRequested = true;
                if (skipRequested)
                    yield break;

                who.position = Vector3.MoveTowards(who.position, destination, speed * Time.deltaTime);
                yield return null;
            }
        }

        private IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end && !skipRequested)
            {
                if (ringing && SkipPressed())
                    skipRequested = true;
                yield return null;
            }
        }

        // ---------- What the imp does ----------

        // Called by the action player for the imp's actions
        public IEnumerator PlayGive(WorldActionInfo action)
        {
            FoodSack sack = (FoodSack)action.Arguments[0];
            GiveOutcome outcome = kitchen.Imp.LastOutcome;

            if (outcome == GiveOutcome.HoundAsleep)
                setup.Session.Log.AddWarning(action.Line, action.Text, "the hound is asleep, so nothing happened");

            return GiveRoutine(kitchen.Sacks.IndexOf(sack), outcome);
        }

        public IEnumerator PlayPet(WorldActionInfo action)
        {
            return PetRoutine();
        }

        // A refusal stops the run, so there is no action to play. The scene reacts to the result itself.
        private void OnGiveResolved(IFood food, Hellhound target, GiveOutcome outcome)
        {
            if (outcome == GiveOutcome.Refused)
                StartCoroutine(GiveRoutine(kitchen.Sacks.IndexOf((FoodSack)food), outcome));
        }

        private IEnumerator GiveRoutine(int index, GiveOutcome outcome)
        {
            busy++;
            Transform sack = sacks[index];

            // Walk to the sack and pick it up
            yield return Walk(imp, sack.position, 0.9f);
            sack.SetParent(imp, true);
            sack.localPosition = new Vector3(0f, 0.9f, 0.3f);

            // Carry it to the hound
            yield return Walk(imp, hound.position, 1.3f);

            if (outcome == GiveOutcome.Eaten)
            {
                yield return Wait(0.5f);
                sack.SetParent(sackParents[index], true);
                sack.gameObject.SetActive(false);
                chompTimer = 0.6f;
                yield return Wait(0.7f);
            }
            else if (outcome == GiveOutcome.Refused)
            {
                // The hound spits it out: the sack lands on the floor next to the imp
                yield return Wait(0.4f);
                chompTimer = 0.4f;
                sack.SetParent(sackParents[index], true);
                Vector3 landing = imp.position + Vector3.left * 0.8f;
                landing.y = sackPositions[index].y;
                sack.position = landing;
                yield return Wait(0.8f);
            }
            else
            {
                // Asleep: the imp just holds the sack up for a moment
                sack.localPosition = new Vector3(0f, 1.3f, 0.3f);
                yield return Wait(1.4f);
                RestoreSack(index);
            }

            busy--;
        }

        private IEnumerator PetRoutine()
        {
            busy++;
            yield return Walk(imp, hound.position, 1.1f);
            petTimer = 2f;
            yield return Wait(2f);
            busy--;
        }

        // ---------- The dinner bell ----------

        public void RingBell()
        {
            if (CanReset)
                StartCoroutine(RingRoutine());
        }

        private IEnumerator RingRoutine()
        {
            ringing = true;
            skipRequested = false;

            // The bell swings
            for (float t = 0f; t < 1f && !skipRequested; t += Time.deltaTime)
            {
                if (SkipPressed())
                    skipRequested = true;
                bell.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 30f) * 20f * (1f - t));
                yield return null;
            }
            bell.localRotation = Quaternion.identity;

            // The cook comes in and puts back every sack that is not where it started
            goblinCook.position = doorInside.position;
            goblinCook.gameObject.SetActive(true);

            for (int i = 0; i < sacks.Length && !skipRequested; i++)
            {
                if (IsAtStart(i))
                    continue;

                yield return Walk(goblinCook, sackPositions[i], 1f);
                yield return Wait(0.3f);
                RestoreSack(i);
            }

            if (!skipRequested)
                yield return Walk(goblinCook, doorOutside.position, 0.1f);

            goblinCook.gameObject.SetActive(false);
            RestoreWorld();
            skipRequested = false;
            ringing = false;
        }

        private bool IsAtStart(int index)
        {
            Transform sack = sacks[index];
            return sack.gameObject.activeSelf == sackActive[index]
                && sack.parent == sackParents[index]
                && (sack.position - sackPositions[index]).sqrMagnitude < 0.0001f;
        }

        private void RestoreSack(int index)
        {
            Transform sack = sacks[index];
            sack.SetParent(sackParents[index], true);
            sack.position = sackPositions[index];
            sack.rotation = sackRotations[index];
            sack.gameObject.SetActive(sackActive[index]);
        }

        // The model goes back to its starting values and the scene follows it
        private void RestoreWorld()
        {
            kitchen.Reset();

            for (int i = 0; i < sacks.Length; i++)
                RestoreSack(i);

            imp.position = impStart;
            hound.position = houndStart;
            chompTimer = 0f;
            petTimer = 0f;
            resultShown = false;
            resultUntil = 0f;

            setup.Session.Log.Clear();
        }

        // ---------- Story ----------

        private IEnumerator Briefing()
        {
            // Wait a frame so the player has set up its input first
            yield return null;
            player.SetInputEnabled(false);

            // The player starts facing the Demon Lord
            FaceTowards(demonLord.position);

            yield return dialogue.Say("Demon Lord", "You. Programmer. My hound hasn't eaten since your summoning and he's started on the throne.");
            yield return dialogue.Say("Demon Lord", "Make him stop being hungry. He eats meat. ONLY meat.");
            yield return dialogue.Say("Demon Lord", "Don't make me ask twice.");

            yield return Walk(demonLord, doorOutside.position, 0.1f, 2f);
            demonLord.gameObject.SetActive(false);

            // Then they turn to look into the room, towards the north
            yield return TurnTo(Quaternion.identity, 1.3f);
            player.SetInputEnabled(true);
        }

        // Turns the player round over a moment, easing in and out, so the view does not jump
        private IEnumerator TurnTo(Quaternion target, float seconds)
        {
            Quaternion start = player.transform.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                player.transform.rotation = Quaternion.Slerp(start, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            player.transform.rotation = target;
        }

        private IEnumerator TurnToward(Vector3 point, float seconds)
        {
            Vector3 direction = point - player.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
                yield return TurnTo(Quaternion.LookRotation(direction), seconds);
        }

        private void FaceTowards(Vector3 point)
        {
            Vector3 direction = point - player.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
                player.transform.rotation = Quaternion.LookRotation(direction);
        }

        private IEnumerator Ending()
        {
            endingRunning = true;
            resultShown = true;

            if (!storyDone)
            {
                storyDone = true;
                player.SetInputEnabled(false);
                Quaternion startRotation = player.transform.rotation;

                // The hound goes to sleep, then the Demon Lord comes back
                yield return Wait(1f);
                demonLord.position = doorOutside.position;
                demonLord.gameObject.SetActive(true);
                yield return Walk(demonLord, doorInside.position, 0.1f, 2f);
                yield return TurnToward(demonLord.position, 0.9f);

                yield return dialogue.Say("Demon Lord", "Good. Tomorrow he hunts the villagers who fled into the woods. He'll need his strength.");

                yield return Walk(demonLord, doorOutside.position, 0.1f, 2f);
                demonLord.gameObject.SetActive(false);

                // A silent beat: the imp takes the leftover bread out of the door
                yield return BreadBeat();

                // The imp learns something new and shows it
                setup.Types.Learn("Pet");
                yield return dialogue.Say("", "The imp has learned a new trick.");
                yield return PetRoutine();

                yield return TurnTo(startRotation, 1.2f);
                player.SetInputEnabled(true);
            }

            resultUntil = Time.time + 15f;
            endingRunning = false;
        }

        private IEnumerator BreadBeat()
        {
            int bread = 4;
            if (kitchen.Sacks[bread].IsEaten)
                yield break;

            Transform sack = sacks[bread];
            yield return Walk(imp, sack.position, 0.9f);
            sack.SetParent(imp, true);
            sack.localPosition = new Vector3(0f, 0.9f, 0.3f);

            yield return Walk(imp, doorOutside.position, 0.1f);
            sack.SetParent(sackParents[bread], true);
            sack.gameObject.SetActive(false);

            yield return Wait(0.8f);
            yield return Walk(imp, impStart, 0.1f);
        }

        // ---------- Result ----------

        // The box shows for a while after the ending, but not on top of the computer
        private void UpdateResultPanel()
        {
            bool show = Time.time <= resultUntil && !computer.IsOpen;
            resultPanel.SetActive(show);
            if (show)
                resultText.text = kitchen.OptionalGoalMet ? "Nothing spat out: done" : "Nothing spat out: not done";
        }
    }
}
