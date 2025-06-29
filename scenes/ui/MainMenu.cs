using Game.Autoload;
using Godot;

namespace Game.UI;

public partial class MainMenu : Node
{
	[Export]
	private PackedScene optionsMenuScene;

	private Button playButton;
	private Control mainMenuContainer;
	private LevelSelectScreen levelSelectScreen;
	private Button quitButton;
	private Button optionsButton;

	public override void _Ready()
	{
		playButton = GetNode<Button>("%PlayButton");
		quitButton = GetNode<Button>("%QuitButton");
		optionsButton = GetNode<Button>("%OptionsButton");

		AudioHelpers.RegisterButtons(new Button[] { playButton, quitButton, optionsButton });

		mainMenuContainer = GetNode<Control>("%MainMenuContainer");
		levelSelectScreen = GetNode<LevelSelectScreen>("%LevelSelectScreen");

		//ensures proper default setting
		levelSelectScreen.Visible = false;
		mainMenuContainer.Visible = true;
		optionsButton.Pressed += OnOptionsButtonPressed;
		playButton.Pressed += OnPlayButtonPressed;
		quitButton.Pressed += OnQuitButtonPressed;

		levelSelectScreen.BackButtonPressed += OnLevelSelectButtonPressed;
	}

	private void OnQuitButtonPressed()
	{
		GetTree().Quit();
	}

	private void OnPlayButtonPressed()
	{
		levelSelectScreen.Visible = true;
		mainMenuContainer.Visible = false;
	}

	private void OnLevelSelectButtonPressed()
	{
		mainMenuContainer.Visible = true;
		levelSelectScreen.Visible = false;
	}

	private void OnOptionsButtonPressed()
	{
		mainMenuContainer.Visible = false;
		var optionsMenu = optionsMenuScene.Instantiate<OptionsMenu>();
		AddChild(optionsMenu);
		optionsMenu.DonePressed += () =>
		{
			OnOptionsDonePressed(optionsMenu);
		};
	}

	private void OnOptionsDonePressed(OptionsMenu optionsMenu)
	{
		optionsMenu.QueueFree();
		mainMenuContainer.Visible = true;
	}

}
