using Game.UI;
using Godot;

namespace Game.Manager;

public partial class FloatingTextManager : Node
{
    [Export]
    private PackedScene floatingTextScene;

    private static FloatingTextManager instance;

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
        {
            instance = this;
        }
    }


    public static void ShowMessage(string message)
    {
        //var floatingText = instance.floatingTextScene.Instantiate<FloatingTextManager>();
        var floatingText = instance.floatingTextScene.Instantiate<FloatingText>();//the tutorial uses this but it doesnt work...
        instance.AddChild(floatingText);
        floatingText.SetText(message);
        floatingText.GlobalPosition = floatingText.GetGlobalMousePosition();
    }
}
