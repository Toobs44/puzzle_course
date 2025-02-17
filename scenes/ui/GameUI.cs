using Game.Manager;
using Game.Resources.Building;
using Godot;

namespace Game.UI;

public partial class GameUI : CanvasLayer
{
	[Signal]
	public delegate void BuildingResourceSelectedEventHandler(BuildingResource buildingResource);
	

	private VBoxContainer buildingSectionContainer;
	private Label resourceLabel;

	[Export]
	private BuildingManager buildingManager;
	[Export]
	private BuildingResource[] buildingResources;

	[Export]
	private PackedScene buildingSectionScene;

	public override void _Ready()
	{
		buildingSectionContainer = GetNode<VBoxContainer>("%BuildingSectionContainer");
		resourceLabel  = GetNode<Label>("%ResourceLabel");
		CreateBuildingSections();
	
		buildingManager.AvailableResourceCountChanged += OnAvailableResourceCountChanged;
	}

	public void HideUI()
	{
		Visible = false;
	}

	private void CreateBuildingSections()
	{
		foreach (var buildingResource in buildingResources)
		{
			//instantiate the prebuilt label and button for the UI
			var buildingSection = buildingSectionScene.Instantiate<BuildingSection>();
			// the new scene is still a orphan node, add it to the node tree to change this.
			buildingSectionContainer.AddChild(buildingSection);
			// add the custom text to the button.
			buildingSection.SetBuildingResource(buildingResource);

			// use an anonymous function to emit a signal for the specific button
			buildingSection.SelectButtonPressed += () =>
			{
				EmitSignal(SignalName.BuildingResourceSelected, buildingResource);	
			};
		}
	}

	private void OnAvailableResourceCountChanged(int AvailableResourceCount)
	{
		resourceLabel.Text = $"{AvailableResourceCount}";
	}
}
