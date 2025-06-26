using Game.Autoload;
using Godot;

namespace Game.UI;

public partial class MainMenu : Node
{

	private Button playButton;
	private Control mainMenuContainer;
	private LevelSelectScreen levelSelectScreen;
	private Button quitButton;

	public override void _Ready()
	{
		playButton = GetNode<Button>("%PlayButton");
		quitButton = GetNode<Button>("%QuitButton");

		AudioHelpers.RegisterButtons(new Button[] { playButton, quitButton });

		mainMenuContainer = GetNode<Control>("%MainMenuContainer");
		levelSelectScreen = GetNode<LevelSelectScreen>("%LevelSelectScreen");

		//ensures proper default setting
		levelSelectScreen.Visible = false;
		mainMenuContainer.Visible = true;

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
		mainMenuContainer.Visible = false;
		levelSelectScreen.Visible = true;
	}

	private void OnLevelSelectButtonPressed()
	{
		mainMenuContainer.Visible = true;
		levelSelectScreen.Visible = false;
	}
}
