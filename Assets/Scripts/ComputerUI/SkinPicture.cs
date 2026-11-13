using UnityEngine.UI;

namespace ComputerUI
{
    // An image that does not tell the layout groups how big its sprite is. A frame or a button picture is only a background,
    // the size of the object comes from what is inside it.
    public class SkinPicture : Image
    {
        public override float minWidth
        {
            get { return 0f; }
        }

        public override float preferredWidth
        {
            get { return 0f; }
        }

        public override float minHeight
        {
            get { return 0f; }
        }

        public override float preferredHeight
        {
            get { return 0f; }
        }
    }
}
