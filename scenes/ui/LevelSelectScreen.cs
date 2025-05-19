using System;
using Game.Autoload;
using Game.Resources.Level;
using Godot;

namespace Game.UI;

public partial class LevelSelectScreen : MarginContainer
{
	private const int PAGE_SIZE = 6;
	[Signal]
	public delegate void BackButtonPressedEventHandler();

	[Export]
	private PackedScene levelSelectSectionScene;

	private GridContainer gridContainer;
	private Button backButton;
	private int pageIndex;
	private int maxPageIndex;
	private LevelDefinitionResource[] levelDefinitions;

	private Button previousPageButton;
	private Button nextPageButton;

	public override void _Ready()
	{
		gridContainer = GetNode<GridContainer>("%GridContainer");
		backButton = GetNode<Button>("BackButton");
		previousPageButton = GetNode<Button>("%PreviousPageButton");
		nextPageButton = GetNode<Button>("%NextPageButton");

		levelDefinitions = LevelManager.GetLevelDefinitions();
		maxPageIndex = levelDefinitions.Length / PAGE_SIZE;

		backButton.Pressed += OnBackButtonPressed;
		previousPageButton.Pressed += () => OnPageChanged(-1);
		nextPageButton.Pressed += () => OnPageChanged(1);

		ShowPage();
	}

	private void ShowPage()
	{
		UpdateButtonVisibility();

		foreach (var child in gridContainer.GetChildren())
		{
			child.QueueFree();
		}

		var startIndex = PAGE_SIZE * pageIndex;
		var endIndex = Mathf.Min(startIndex + PAGE_SIZE, levelDefinitions.Length);
		for (var i = startIndex; i < endIndex; i++)
		{
			var levelDefinition = levelDefinitions[i];
			var levelSelectSection = levelSelectSectionScene.Instantiate<LevelSelectSection>();
			gridContainer.AddChild(levelSelectSection);

			levelSelectSection.SetLevelDefinition(levelDefinition);
			levelSelectSection.SetLevelIndex(i);
			levelSelectSection.LevelSelected += OnLevelSelected;
		}
	}

	private void UpdateButtonVisibility()
	{
		previousPageButton.Disabled = pageIndex == 0;
		previousPageButton.Modulate = pageIndex == 0 ? Colors.Transparent : Colors.White;
		nextPageButton.Disabled = pageIndex == maxPageIndex;
		nextPageButton.Modulate = pageIndex == maxPageIndex ? Colors.Transparent : Colors.White;
	}

	private void OnLevelSelected(int levelIndex)
	{
		LevelManager.Instance.ChangeToLevel(levelIndex);
	}

	private void OnBackButtonPressed()
	{
		EmitSignal(SignalName.BackButtonPressed);
	}

	private void OnPageChanged(int change)
	{
		pageIndex += change;
		ShowPage();
	}
}
