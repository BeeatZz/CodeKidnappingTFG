using System.Collections.Generic;
using CodeRunner.Bridge;
using CodeRunner.Interpreting;
using Mission;
using UnityEngine;

namespace Game
{
    // Creates the first test mission and connects it to the objects in the scene
    public class TestSceneSetup : MonoBehaviour
    {
        public static TestSceneSetup Instance { get; private set; }

        public ImpActionPlayer impPlayer;
        public bool kitchenMission;     // true in the Feed the Hellhound scene

        public FirstMission Level { get; private set; }
        public Kitchen Kitchen { get; private set; }
        public TypeRegistry Types { get; private set; }
        public ScopeRegistry Handles { get; private set; }
        public CodeSession Session { get; private set; }

        // Model object -> the object in the scene that shows it
        public Dictionary<object, ScannableObject> Objects { get; private set; }

        // The handles the player has scanned, in order. The imp is always there.
        public List<string> ScannedNames { get; private set; }
        public string LastScanned { get; private set; }
        public float LastScanTime { get; private set; }

        private void Awake()
        {
            Instance = this;

            Imp imp;
            if (kitchenMission)
            {
                Kitchen = new Kitchen();
                imp = Kitchen.Imp;
            }
            else
            {
                Level = new FirstMission();
                imp = Level.Imp;
            }

            Types = new TypeRegistry();
            Types.RegisterAssembly(typeof(Imp).Assembly);

            Handles = new ScopeRegistry(imp);
            Session = new CodeSession(Types, Handles, impPlayer);

            if (Kitchen != null)
                Kitchen.WatchSession(Session);

            Objects = new Dictionary<object, ScannableObject>();
            ScannedNames = new List<string> { "imp" };

            BindSceneObjects();
        }

        private void BindKitchenObjects()
        {
            foreach (ScannableObject sceneObject in FindObjectsByType<ScannableObject>(FindObjectsSortMode.None))
            {
                switch (sceneObject.kind)
                {
                    case ScanKind.Sack: sceneObject.Target = Kitchen.Sacks[sceneObject.index]; break;
                    case ScanKind.Hound: sceneObject.Target = Kitchen.Hound; break;
                    case ScanKind.Bell: sceneObject.Target = Kitchen.Bell; break;
                    case ScanKind.Imp: sceneObject.Target = Kitchen.Imp; break;
                    default: continue;
                }
                Objects[sceneObject.Target] = sceneObject;
            }
        }

        private void BindSceneObjects()
        {
            if (Kitchen != null)
            {
                BindKitchenObjects();
                return;
            }

            // The first 30 things in the depot are the crates, in the order they were added
            List<IScannable> things = new List<IScannable>(Level.Depot.ScanAll());

            foreach (ScannableObject sceneObject in FindObjectsByType<ScannableObject>(FindObjectsSortMode.None))
            {
                switch (sceneObject.kind)
                {
                    case ScanKind.Crate: sceneObject.Target = things[sceneObject.index]; break;
                    case ScanKind.Wagon: sceneObject.Target = Level.Depot.Wagons[sceneObject.index]; break;
                    case ScanKind.Ledger: sceneObject.Target = Level.Ledger; break;
                    case ScanKind.Depot: sceneObject.Target = Level.Depot; break;
                    case ScanKind.Imp: sceneObject.Target = Level.Imp; break;
                }
                Objects[sceneObject.Target] = sceneObject;
            }
        }

        // Gives the player a handle for the object, so code can use its name from now on
        public void Scan(ScannableObject sceneObject)
        {
            string name = sceneObject.HandleName;

            if (!Handles.Contains(name))
            {
                Handles.Add(name, sceneObject.Target);
                ScannedNames.Add(name);
            }

            LastScanned = name;
            LastScanTime = Time.time;
        }

        public ObjectDescription Describe(string handle)
        {
            object target;
            if (!Handles.TryGet(handle, out target))
                return null;
            return Types.Describe(target);
        }

        public string GetStatus()
        {
            if (Kitchen != null)
            {
                string state = Kitchen.IsMissionComplete ? "the hound is asleep" : "the hound is still hungry";
                return "Hellhound: " + Kitchen.Hound.Mood + "  |  " + state;
            }

            Camp camp = Level.Camp;
            string order = MissionRules.IsMissionComplete(Level.Ledger, camp) ? "order complete" : "order not complete";
            return "Camp: " + camp.Accepted.Count + " accepted, " + camp.Rejected.Count + " rejected  |  " + order;
        }
    }
}
