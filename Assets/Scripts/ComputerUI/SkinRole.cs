namespace ComputerUI
{
    // The kinds of picture the desktop is made of. Every look has one sprite for each of them.
    public enum SkinRole
    {
        Fill,           // a plain block, for things that are only a colour
        Window,         // the frame of a window
        TitleBar,
        Panel,          // a dark box: menus, the question box, the game screens
        Page,           // the parchment of the apps
        Bar,            // a strip inside an app: toolbar, status bar, wax bar
        Taskbar,
        Button,
        ButtonSeal,     // the red button that starts the work order
        ButtonSmall,    // the buttons of the title bar
        Tab,
        TabActive,
        Field,          // where the player types
        Roller,         // the wooden bar at the top and bottom of a scroll
        Note            // the yellow memo
    }
}
