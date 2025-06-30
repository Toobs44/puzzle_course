using Game.Autoload;
using Godot;

namespace Game.UI;

public partial class EscapeMenu : CanvasLayer
{
	private readonly StringName ESCAPE_ACTION = "escape";

	[Export(PropertyHint.File, "*.tscn")]
	private string mainMenuScenePath;
	[Export]
	private PackedScene optionsMenuScene;

	private Button quitButton;
	private Button resumeButton;
	private Button optionsButton;
	private MarginContainer marginContainer;

	public override void _Ready()
	{
		quitButton = GetNode<Button>("%QuitButton");
		resumeButton = GetNode<Button>("%ResumeButton");
		optionsButton = GetNode<Button>("%OptionsButton");
		marginContainer = GetNode<MarginContainer>("MarginContainer");

		AudioHelpers.RegisterButtons(new Button[] { quitButton, resumeButton, optionsButton });

		quitButton.Pressed += OnQuitButtonPressed;
		resumeButton.Pressed += OnResumeButtonPressed;
		optionsButton.Pressed += OnOptionsButtonPressed;
	}

	public override void _UnhandledInput(InputEvent evt)
	{
		if (evt.IsActionPressed(ESCAPE_ACTION))
		{
			QueueFree();
			GetViewport().SetInputAsHandled();
	   	}
    }


	private void OnOptionsButtonPressed()
	{
		marginContainer.Visible = false;
		var optionsMenu = optionsMenuScene.Instantiate<OptionsMenu>();
		AddChild(optionsMenu);
		optionsMenu.DonePressed += () =>
		{
			OnOptionsDonePressed(optionsMenu);
		};
	}

	private void OnResumeButtonPressed()
	{
		QueueFree();
	}

	private void OnQuitButtonPressed()
	{
		GetTree().ChangeSceneToFile(mainMenuScenePath);
	}

	private void OnOptionsDonePressed(OptionsMenu optionsMenu)
	{
		marginContainer.Visible = true;
		optionsMenu.QueueFree();
	}

}
