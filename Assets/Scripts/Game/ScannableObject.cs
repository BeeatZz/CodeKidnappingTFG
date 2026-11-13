using UnityEngine;

namespace Game
{
    public enum ScanKind
    {
        Crate,
        Wagon,
        Ledger,
        Depot,
        Imp,
        Sack,
        Hound,
        Bell
    }

    // Links an object in the scene to its model object. Scanning it gives the player a handle.
    public class ScannableObject : Interactable
    {
        private static readonly string[] WagonNames = { "smallCart", "farmWagon", "warWagon" };

        public ScanKind kind;
        public int index;       // which crate, wagon or sack

        // The model object, set by TestSceneSetup when the scene starts
        public object Target { get; set; }

        public string HandleName
        {
            get
            {
                switch (kind)
                {
                    case ScanKind.Crate: return "crate" + index.ToString("D2");
                    case ScanKind.Wagon: return WagonNames[index];
                    case ScanKind.Ledger: return "ledger";
                    case ScanKind.Depot: return "depot";
                    case ScanKind.Sack: return "sack" + (index + 1).ToString("D2");
                    case ScanKind.Hound: return "hound";
                    case ScanKind.Bell: return "dinnerBell";
                    default: return "imp";
                }
            }
        }

        public override string GetPrompt()
        {
            if (kind == ScanKind.Imp)
                return "[E] Scan the imp";

            string prompt = "[E] Scan " + kind.ToString().ToLower();
            if (TestSceneSetup.Instance.Handles.Contains(HandleName))
                prompt += " (scanned as " + HandleName + ")";
            return prompt;
        }

        public override void Interact()
        {
            TestSceneSetup.Instance.Scan(this);
        }
    }
}
