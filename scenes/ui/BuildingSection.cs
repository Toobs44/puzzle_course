using Godot;
using Game.Resources.Building;

namespace Game.UI;

public partial class BuildingSection : PanelContainer
{
	[Signal]
	public delegate void SelectButtonPressedEventHandler();

	private Label titleLabel;
	private Label descriptionLabel;
	private Label costLabel;
	private Button selectButton;

	public override void _Ready()
	{
		titleLabel = GetNode<Label>("%TitleLabel");
		descriptionLabel = GetNode<Label>("%DescriptionLabel");
		costLabel = GetNode<Label>("%CostLabel");
		selectButton = GetNode<Button>("%Button");

		selectButton.Pressed += OnSelectButtonPressed;
	}

	public void SetBuildingResource(BuildingResource buildingResource)
	{
		//make the label dynamic to change what it says
		titleLabel.Text = buildingResource.DisplayName;
		//Allows the cost text to change with the selected buildingResouce.
		costLabel.Text = $"{buildingResource.ResourceCost}";
		descriptionLabel.Text = buildingResource.Description;
	}

	// For safety emit a signal for the button press instead of making the selectButton public.
	public void OnSelectButtonPressed()
	{
		EmitSignal(SignalName.SelectButtonPressed);
	}
}
