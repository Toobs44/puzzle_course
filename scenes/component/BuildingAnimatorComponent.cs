using System.Linq;
using Game.Autoload;
using Godot;

namespace Game.Component;

public partial class BuildingAnimatorComponent : Node2D
{
    [Signal]
    public delegate void DestroyAnimationFinishedEventHandler();
    [Signal]
    public delegate void DisableAnimationFinishedEventHandler();

    [Export]
    private PackedScene impactParticlesScene;

    [Export]
    private PackedScene destroyParticlesScene;

    [Export]
    private Texture2D maskTexture;

    private Tween activeTween;
    private Node2D animationRootNode;
    private Sprite2D maskNode;
    private AudioStreamPlayer impactAudioStreamPlayer;

    public override void _Ready()
    {
        YSortEnabled = false;//YSort is needed in the editor but will mess things up after the game starts.
        impactAudioStreamPlayer = GetNode<AudioStreamPlayer>("ImpactAudioStreamPlayer");
        SetUpNodes();
    }

    public void PlayInAnimation()
    {
        if (animationRootNode == null) return;

        // ensures there isnt more than one of the same tween active.
        if (activeTween != null && activeTween.IsValid())
        {
            activeTween.Kill();
        }
        
        activeTween = CreateTween();
        //This animates the "fall" of the building starting from 128 pixels up
        activeTween
            .TweenProperty(animationRootNode, "position", Vector2.Zero, .3)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In)
            .From(Vector2.Up * 128);
        activeTween.TweenCallback(Callable.From(() =>
        {
            var impactParticles = impactParticlesScene.Instantiate<Node2D>();
            Owner.GetParent().AddChild(impactParticles);
            impactParticles.GlobalPosition = GlobalPosition;
            impactAudioStreamPlayer.Play();
            GameCamera.Shake();
        }));
        activeTween
            .TweenProperty(animationRootNode, "position", Vector2.Up * 16, .1)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        activeTween
            .TweenProperty(animationRootNode, "position", Vector2.Zero, .1)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
    }

    public void PlayDestroyAnimation()
    {
        if (animationRootNode == null) return;

        // ensures there isnt more than one of the same tween active.
        if (activeTween != null && activeTween.IsValid())
        {
            activeTween.Kill();
        }

        animationRootNode.Position = Vector2.Zero;

        maskNode.ClipChildren = ClipChildrenMode.Only;
        maskNode.Texture = maskTexture;

        var destroyParticles = destroyParticlesScene.Instantiate<Node2D>();
        Owner.GetParent().AddChild(destroyParticles);
        destroyParticles.GlobalPosition = GlobalPosition;

        AudioHelpers.PlayBuildingDestruction();
        GameCamera.Shake();

        activeTween = CreateTween();
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", -5, .1);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", 5, .1);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", -2, .1);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", 2, .1);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", 0, .1);

        activeTween.TweenProperty(animationRootNode, "position", Vector2.Down * 300, .4)
            .SetTrans(Tween.TransitionType.Quart)
            .SetEase(Tween.EaseType.In);

        activeTween.Finished += () =>
        {
            EmitSignal(SignalName.DestroyAnimationFinished);
        };
    }

    public void PlayDisableAnimation()
    {
        
        if (animationRootNode == null) return;

        // ensures there isnt more than one of the same tween active.
        if (activeTween != null && activeTween.IsValid())
        {
            activeTween.Kill();
        }

        animationRootNode.Position = Vector2.Zero;

        maskNode.ClipChildren = ClipChildrenMode.Only;
        maskNode.Texture = maskTexture;

        var destroyParticles = destroyParticlesScene.Instantiate<Node2D>();
        Owner.GetParent().AddChild(destroyParticles);
        destroyParticles.GlobalPosition = GlobalPosition;

        AudioHelpers.PlayBuildingDestruction();
        GameCamera.Shake();

        activeTween = CreateTween();
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", -5, .1);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", 5, .1);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", -2, .1);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", 2, .1);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", 0, .1);

        
        activeTween.Finished += () =>
        {
            EmitSignal(SignalName.DisableAnimationFinished);
        };

    }

    //This creates a new node so the Y sort is anchored and the building "fall" animation plays without flickering.
    private void SetUpNodes()
    {
        var spriteNode = this.GetFirstNodeOfType<Node2D>();
        if (spriteNode == null)
        {
            return;
        }
        // Removes and adds sprites to be in the proper order and position within the tree.
        RemoveChild(spriteNode);
        Position = new Vector2(spriteNode.Position.X, spriteNode.Position.Y);

        maskNode = new Sprite2D
        {
            Centered = false,
            Offset = new Vector2(-160, -256)
        };
        AddChild(maskNode);

        animationRootNode = new Node2D();
        maskNode.AddChild(animationRootNode);

        animationRootNode.AddChild(spriteNode);
        spriteNode.Position = new Vector2(0, 0);
    }
}
