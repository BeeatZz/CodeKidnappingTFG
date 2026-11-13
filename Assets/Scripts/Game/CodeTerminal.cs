using Computer;
using ComputerUI;

namespace Game
{
    // The terminal where the player types code. Using it opens the computer.
    public class CodeTerminal : Interactable
    {
        public CanvasComputer computer;

        public override string GetPrompt()
        {
            return "[E] Use terminal";
        }

        public override void Interact()
        {
            computer.Open();
        }
    }
}
