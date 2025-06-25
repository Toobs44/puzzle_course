using System.Collections.Generic;
using System.Linq;
using Game.Autoload;
using Game.Resources.Building;
using Godot;

namespace Game.Component;

public partial class BuildingComponent : Node2D
{
	[Signal]
	public delegate void DisabledEventHandler();
	[Signal]
	public delegate void EnabledEventHandler();

	[Export(PropertyHint.File, "*.tres")]
	private string buildingResourcePath;
	[Export]
	private BuildingAnimatorComponent buildingAnimatorComponent;

	public BuildingResource BuildingResource { get; private set; }
	public bool IsDestroying { get; private set; }
	public bool IsDisabled { get; private set; }

	private HashSet<Vector2I> occupiedTiles = new();

	public static IEnumerable<BuildingComponent> GetValidBuildingComponents(Node node)
	{
		//use node group to get array of nodes and cast them as building components.
		return node.GetTree().GetNodesInGroup(nameof(BuildingComponent)).Cast<BuildingComponent>()
		//filter through the list to all elements and ignore the excluded building.
		.Where((BuildingComponent) => !BuildingComponent.IsDestroying);
	}

	public static IEnumerable<BuildingComponent> GetDangerBuildingComponents(Node node)
	{
		return GetValidBuildingComponents(node)
			.Where((buildingComponent) => buildingComponent.BuildingResource.IsDangerBuilding());
	}

	public static IEnumerable<BuildingComponent> GetNonDangerBuildingComponents(Node node)
	{
		return GetValidBuildingComponents(node)
			.Where((buildingComponent) => !buildingComponent.BuildingResource.IsDangerBuilding());
	}

	public override void _Ready()
	{
		if (buildingResourcePath != null)
		{
			BuildingResource = GD.Load<BuildingResource>(buildingResourcePath);
		}

		if (buildingAnimatorComponent != null)
		{
			buildingAnimatorComponent.DestroyAnimationFinished += OnDestroyAnimationFinished;
			buildingAnimatorComponent.DisableAnimationFinished += OnDisabledAnimationFinished;
		}


		AddToGroup(nameof(BuildingComponent));
		//wait to emit the signal after other functions are done
		Callable.From(Initialize).CallDeferred();
	}

	public Vector2I GetGridCellPosition()
	{
		var gridPosition = GlobalPosition / 64;
		gridPosition = gridPosition.Floor();
		return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
	}

	public HashSet<Vector2I> GetOccupiedCellPositions()
	{
		return occupiedTiles.ToHashSet();
	}

	public Rect2I GetTileArea()
	{
		var rootCell = GetGridCellPosition();
		var tileArea = new Rect2I(rootCell, BuildingResource.Dimensions);
		return tileArea;
	}

	public bool IsTileInBuildingArea(Vector2I tilePosition)
	{
		return occupiedTiles.Contains(tilePosition);
	}

	public void Disable()
	{
		GD.Print(IsDisabled);
		if (IsDisabled) return;// dont waste time emitting if the building is already disabled.
		IsDisabled = true;
		buildingAnimatorComponent?.PlayDisableAnimation();
	}

	public void Enable()
	{
		if (!IsDisabled) return;// dont waste time emitting if the building is already enabled.
		IsDisabled = false;
		EmitSignal(SignalName.Enabled);
		GameEvents.EmitBuildingEnabled(this);
		buildingAnimatorComponent?.PlayInAnimation();
	}

	public void Destroy()
	{
		IsDestroying = true;
		GameEvents.EmitBuildingDestroyed(this);
		buildingAnimatorComponent?.PlayDestroyAnimation();//using a "Null chain"
														  //find the root of the scene this node is a part of and destroy it along with its children nodes.
		if (buildingAnimatorComponent == null)
		{
			Owner.QueueFree();
		}
	}

	private void CalculateOccupiedCellPositions()
	{
		var gridPostion = GetGridCellPosition();
		for (int x = gridPostion.X; x < gridPostion.X + BuildingResource.Dimensions.X; x++)
		{
			for (int y = gridPostion.Y; y < gridPostion.Y + BuildingResource.Dimensions.Y; y++)
			{
				//add the calculated tiles to the hashset list.
				occupiedTiles.Add(new Vector2I(x, y));
			}
		}
	}

	private void Initialize()
	{
		CalculateOccupiedCellPositions();
		GameEvents.EmitBuildingPlaced(this);
	}

	private void OnDestroyAnimationFinished()
	{
		Owner.QueueFree();
	}

	private void OnDisabledAnimationFinished()
	{		
		GD.Print("OnDisabledAnimationFinished called");
		GameEvents.EmitBuildingDisabled(this);
		EmitSignal(SignalName.Disabled);
	}
}
