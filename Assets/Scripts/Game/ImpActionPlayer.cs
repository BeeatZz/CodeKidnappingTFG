using System.Collections;
using CodeRunner.Bridge;
using Mission;
using UnityEngine;

namespace Game
{
    // Plays the imp's animation for each world action. The interpreter waits for it to finish.
    public class ImpActionPlayer : MonoBehaviour, IActionPlayer
    {
        public Transform imp;
        public float speed = 8f;
        public float wagonTopHeight = 1.35f;    // height of the first crate on a wagon
        public KitchenController kitchen;       // only set in the kitchen mission

        // What the imp is saying right now, drawn by the HUD
        public string CurrentSpeech { get; private set; }

        public IEnumerator Play(WorldActionInfo action)
        {
            switch (action.Method)
            {
                case "MoveTo": return MoveTo(action.Arguments[0]);
                case "Load": return Load((ICargo)action.Arguments[0], (Wagon)action.Arguments[1]);
                case "Say": return Say((string)action.Arguments[0]);
                case "Dispatch": return Dispatch((Wagon)action.Target);
                case "Give": return kitchen.PlayGive(action);
                case "Pet": return kitchen.PlayPet(action);
            }
            return Pause();
        }

        private Transform TransformOf(object target)
        {
            ScannableObject sceneObject;
            if (target != null && TestSceneSetup.Instance.Objects.TryGetValue(target, out sceneObject))
                return sceneObject.transform;
            return null;
        }

        private IEnumerator Pause()
        {
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator WalkTo(Vector3 destination, float stopDistance)
        {
            destination.y = imp.position.y;

            while (Vector3.Distance(imp.position, destination) > stopDistance)
            {
                Vector3 direction = destination - imp.position;
                if (direction.sqrMagnitude > 0.001f)
                    imp.rotation = Quaternion.LookRotation(direction);

                imp.position = Vector3.MoveTowards(imp.position, destination, speed * Time.deltaTime);
                yield return null;
            }
        }

        private IEnumerator MoveTo(object target)
        {
            Transform destination = TransformOf(target);
            if (destination != null)
                yield return WalkTo(destination.position, 1.5f);
        }

        private IEnumerator Load(ICargo cargo, Wagon wagon)
        {
            Transform crate = TransformOf(cargo);
            Transform wagonTransform = TransformOf(wagon);
            if (crate == null || wagonTransform == null)
                yield break;

            // Pick the crate up
            yield return WalkTo(crate.position, 1.2f);
            crate.SetParent(imp, false);
            crate.localPosition = new Vector3(0f, 1f, 0f);
            crate.localRotation = Quaternion.identity;

            // Carry it to the wagon and put it in the next free spot
            yield return WalkTo(wagonTransform.position, 3.5f);
            crate.SetParent(wagonTransform, false);
            crate.localPosition = SlotPosition(IndexOnWagon(wagon, cargo));
            crate.localRotation = Quaternion.identity;
        }

        private int IndexOnWagon(Wagon wagon, ICargo cargo)
        {
            int index = 0;
            foreach (ICargo item in wagon.Cargo)
            {
                if (item == cargo)
                    break;
                index++;
            }
            return index;
        }

        // Crates go in rows of 3, four rows to a layer, and then stack up
        private Vector3 SlotPosition(int slot)
        {
            int layer = slot / 12;
            int spot = slot % 12;
            float x = (spot % 3 - 1) * 0.75f;
            float z = (spot / 3 - 1.5f) * 0.9f;
            return new Vector3(x, wagonTopHeight + layer * 0.85f, z);
        }

        private IEnumerator Dispatch(Wagon wagon)
        {
            Transform wagonTransform = TransformOf(wagon);
            if (wagonTransform == null)
                yield break;

            // The wagon drives off towards the camp
            Vector3 goal = wagonTransform.position + Vector3.forward * 40f;
            while (Vector3.Distance(wagonTransform.position, goal) > 0.1f)
            {
                wagonTransform.position = Vector3.MoveTowards(wagonTransform.position, goal, 10f * Time.deltaTime);
                yield return null;
            }
            wagonTransform.gameObject.SetActive(false);
        }

        private IEnumerator Say(string text)
        {
            CurrentSpeech = text;
            yield return new WaitForSeconds(Mathf.Clamp(0.8f + text.Length * 0.05f, 1f, 4f));
            CurrentSpeech = null;
        }
    }
}
