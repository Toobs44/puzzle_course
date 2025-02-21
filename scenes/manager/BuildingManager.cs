using Godot;
using Game.UI;
using Game.Resources.Building;
using Game.Building;
using Game.Component;
using System.Linq;
using System.Collections.Generic;

namespace Game.Manager;

public partial class BuildingManager : Node
{
	private readonly StringName ACTION_LEFT_CLICK = "left_click";
	private readonly StringName ACTION_CANCEL = "cancel";
	private readonly StringName ACTION_RIGHT_CLICK = "right_click";

	[Signal]
	public delegate void AvailableResourceCountChangedEventHandler(int AvailableResourceCount);
	
	[Export]
	private GridManager gridManager;// connect the grid manager to the building manager through the UI.
	[Export]
	private GameUI gameUI;// connect the this through the editor as well.
	[Export]
	private Node2D ySortRoot;
	[Export]
	private PackedScene buildingGhostScene;
	
	private enum State
	{
		Normal,
		PlaceingBuilding
	}

	private int currentResourceCount;
	private int currentlyUsedResourceCount;
	private BuildingResource toPlaceBuildingResource;
	private Rect2I hoveredGridArea = new(Vector2I.Zero, Vector2I.One);
	private BuildingGhost buildingGhost;
	private State currentState;
	private int startingResourceCount;

	private int AvailableResourceCount => startingResourceCount + currentResourceCount - currentlyUsedResourceCount;


	public override void _Ready()
	{
		gridManager.ResourceTilesUpdate += OnResourceTilesUpdated;
		gameUI.BuildingResourceSelected += OnBuildingResourceSelected;

		//this signal will wait to be called after the last ready so it can be used by the GameUI node.
		Callable.From(() => EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount)).CallDeferred();
		
	}


	public override void _UnhandledInput(InputEvent evt)
    {
		switch (currentState)
		{
			case State.Normal:
				if(evt.IsActionPressed(ACTION_RIGHT_CLICK))
				{
					DestroyBuildingAtHoveredCellPosition();
				}
				break;
			case State.PlaceingBuilding:
				if (evt.IsActionPressed(ACTION_CANCEL))
				{
					ChangeState(State.Normal);
				}
				else if (
					toPlaceBuildingResource != null &&
					evt.IsActionPressed(ACTION_LEFT_CLICK) &&
					IsBuildingPlaceableAtArea(hoveredGridArea)
					)
				{
					PlaceBuildingAtHoveredCellPosition();
					
				}
				break;
			default:
				break;
		}
		
    }


    public override void _Process(double delta)
	{
		var mouseGridPostion = gridManager.GetMouseGridCellPostion();
		var rootCell = hoveredGridArea.Position;

		if(rootCell != mouseGridPostion)
		{
			hoveredGridArea.Position = mouseGridPostion;
			UpdateHoveredGridArea();
		}

		switch (currentState)
		{
			case State.Normal:
				break;
			case State.PlaceingBuilding:
				buildingGhost.GlobalPosition = mouseGridPostion * 64;
				break;
		}
	}

	public void SetStartingResourceCount(int count)
	{
		startingResourceCount = count;
	}

	private void UpdateGridDisplay()
	{
		gridManager.ClearHighlightedTiles();//clears old highlighted tiles 
		gridManager.HighlightBuildabletiles();
		if(IsBuildingPlaceableAtArea(hoveredGridArea))
		{
			//show the highlighted area for new buildings with a radius chosen from the custom resource.
			gridManager.HighlightExpandedBuildableTiles(hoveredGridArea, toPlaceBuildingResource.BuildableRadius);
			// show resources on the grid highlighted within the given radius.
			gridManager.HighlightResourcetiles(hoveredGridArea, toPlaceBuildingResource.ResourceRadius);
			buildingGhost.SetValid();
		}
		else
		{
			buildingGhost.SetInvalid();
		}
	}

	private void PlaceBuildingAtHoveredCellPosition()
	{
		var building = toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
		ySortRoot.AddChild(building);

		building.GlobalPosition = hoveredGridArea.Position * 64;
		currentlyUsedResourceCount += toPlaceBuildingResource.ResourceCost;
		ChangeState(State.Normal);
		EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
	}

	private void DestroyBuildingAtHoveredCellPosition()
	{
		var rootCell = hoveredGridArea.Position;// Position is the upper left tile in the rect.
		//use node group to get array of nodes and cast them as building components.
		var buildingComponent = GetTree().GetNodesInGroup(nameof(BuildingComponent)).Cast<BuildingComponent>()
			//Linq filter function takes current arg and checks the list if its true.
			//then returns the first element of that list or a null.
			.FirstOrDefault((buildingComponent) => 
			{
				return buildingComponent.BuildingResource.IsDeletable && buildingComponent.IsTileInBuildngArea(rootCell);
			});
		//if there is no building component(null) then leave method.
		if(buildingComponent == null) return;
		
		//refund the resources of the selected building.
		currentlyUsedResourceCount -= buildingComponent.BuildingResource.ResourceCost;
		//call custom method to destroy building.
		buildingComponent.Destroy();
		EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
	}

	private void ClearBuildingGhost()
	{
		gridManager.ClearHighlightedTiles();
		//to avoid error check if there is a building ghost before removing.
		if(IsInstanceValid(buildingGhost))
		{
			buildingGhost.QueueFree();
		}
		//Ensure the building ghost is null to prevent a bug
		buildingGhost = null;
	}

	private bool IsBuildingPlaceableAtArea(Rect2I tileArea)
	{
		//go through tilePostions and get true or false for position buildable.
		var allTilesBuildable = gridManager.IsTileAreaBuildable(tileArea);
		return allTilesBuildable && AvailableResourceCount >= toPlaceBuildingResource.ResourceCost;
	}


	private void OnResourceTilesUpdated(int resourceCount)
	{
		currentResourceCount = resourceCount;
		EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
	}

	private void UpdateHoveredGridArea()
	{
		switch(currentState)
		{
			case State.Normal:
				break;
			case State.PlaceingBuilding:
				UpdateGridDisplay();
				break;
		}
	}

	private void ChangeState(State toState)
	{
		//this is the clean up and is called first
		switch(currentState)
		{
			case State.Normal :
				break;
			case State.PlaceingBuilding:
				ClearBuildingGhost();
				toPlaceBuildingResource = null;
				break;
		}

		currentState = toState;

		
		switch(currentState)
		{
			case State.Normal:
				break;
			case State.PlaceingBuilding:
				//Instance the building ghost scene and save it as a buildingGhost type.
				buildingGhost = buildingGhostScene.Instantiate<BuildingGhost>();
				ySortRoot.AddChild(buildingGhost);
				break;
		}
	}

	private void OnBuildingResourceSelected(BuildingResource buildingResource)
	{
		ChangeState(State.PlaceingBuilding);
		hoveredGridArea.Size = buildingResource.Dimensions;//is this working?
		var buildingSprite = buildingResource.SpriteScene.Instantiate<Sprite2D>();
		buildingGhost.AddChild(buildingSprite);
		toPlaceBuildingResource = buildingResource;
		UpdateGridDisplay();

	}
}
