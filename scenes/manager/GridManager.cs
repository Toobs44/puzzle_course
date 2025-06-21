using System;
using System.Collections.Generic;
using System.Linq;
using Game.Autoload;
using Game.Component;
using Game.Level.Util;
using Godot;

namespace Game.Manager;

public partial class GridManager : Node
{
	private const string IS_BUILDABLE = "is_buildable";
	private const string IS_WOOD = "is_wood";
	private const string IS_IGNORED = "is_ignored";

	[Signal]
	public delegate void ResourceTilesUpdatedEventHandler( int collectedTiles);
	[Signal]
	public delegate void GridStateUpdatedEventHandler();

	//what is a HashSet maintain a unique list of elements
	private HashSet<Vector2I> validBuildableTiles = new();
	private HashSet<Vector2I> validBuildableAttackTiles = new();
	private HashSet<Vector2I> allTilesInBuildingRadius = new();
	private HashSet<Vector2I> collectedResourceTiles = new();
	private HashSet<Vector2I> occupiedTiles = new();
	private HashSet<Vector2I> dangerOccupiedTiles = new();
	private HashSet<Vector2I> attackTiles = new();
	
	[Export]
	private TileMapLayer highlightTilemapLayer;
	[Export]
	private TileMapLayer baseTerrainTilemapLayer;

	private List<TileMapLayer> allTilemaplayers = new();
	private Dictionary<TileMapLayer, ElevationLayer> tileMapLayerToElevationLayer = new();
	private Dictionary<BuildingComponent, HashSet<Vector2I>> buildingToBuildableTiles = new();
	private Dictionary<BuildingComponent, HashSet<Vector2I>> dangerBuildingToTiles = new();
	private Dictionary<BuildingComponent, HashSet<Vector2I>> attackBuildingToTiles = new();

    public override void _Ready()
    {
		//to prevent errors from freed nodes during scene changes the longer signal call is being used.
		GameEvents.Instance.Connect(GameEvents.SignalName.BuildingPlaced, Callable.From<BuildingComponent>(OnBuildingPlaced));
		GameEvents.Instance.Connect(GameEvents.SignalName.BuildingDestroyed, Callable.From<BuildingComponent>(OnBuildingDestroyed));
		GameEvents.Instance.Connect(GameEvents.SignalName.BuildingEnabled, Callable.From<BuildingComponent>(OnBuildingEnabled));
		GameEvents.Instance.Connect(GameEvents.SignalName.BuildingDisabled, Callable.From<BuildingComponent>(OnBuildingDisabled));
		
		allTilemaplayers = GetAllTilemapLayers(baseTerrainTilemapLayer);
		MapTileLayerToElevationLayer();
    }

	//return a bool and the layer that bool is on.
    public (TileMapLayer, bool) GetTileCustomData(Vector2I tilePosition, string dataName)
	{
		foreach (var layer in allTilemaplayers)
		{
			var customData = layer.GetCellTileData(tilePosition);
			if(customData == null || (bool)customData.GetCustomData(IS_IGNORED)) continue;	
			// if its buildable return a true bool.
			return (layer, (bool)customData.GetCustomData(dataName));
		}

		return (null, false);
	}

	public bool IsTilePostionInAnyBuildingRadius(Vector2I tilePosition)
	{
		return allTilesInBuildingRadius.Contains(tilePosition);
	}

	
	public bool IsTileAreaBuildable(Rect2I tileArea, bool isAttackTiles = false)
	{
		var	tiles = tileArea.ToTiles();
		if (tiles.Count == 0) return false;

		//check if all tiles are in the same elevation layer
		//Underscore discards the second value in the tuple.
		(TileMapLayer firstTileMaperLayer, _) = GetTileCustomData(tiles[0], IS_BUILDABLE);
		var targetElevationLayer = firstTileMaperLayer != null? tileMapLayerToElevationLayer[firstTileMaperLayer] : null;

		var tileSetToCheck = GetBuildableTileSet(isAttackTiles);
		if (isAttackTiles)
		{
			//what is this doing?...
			//By providing a new assingnment of tileSetToCheck with a new hashset 
			// this is preventing the original reference from being modified.
			//I think... 
			tileSetToCheck = tileSetToCheck.Except(occupiedTiles).ToHashSet();
		}

		return tiles.All((tilePosition) =>
		{
			(TileMapLayer tileMapLayer, bool isBuildable) = GetTileCustomData(tilePosition, IS_BUILDABLE);
			var elevationLayer = tileMapLayer != null ? tileMapLayerToElevationLayer[tileMapLayer] : null;
			return isBuildable && tileSetToCheck.Contains(tilePosition) && elevationLayer == targetElevationLayer;
		});
	}

	public void HighlightDangerOccupiedTiles()
	{
		var atlasCoords = new Vector2I(2, 0);
		foreach (var tilePosition in dangerOccupiedTiles)
		{
			highlightTilemapLayer.SetCell(tilePosition, 0, atlasCoords);
		}
	}

	public void HighlightBuildabletiles(bool isAttackTiles = false)
	{
		// look through the valid list of tiles
		foreach(var tilePosition in GetBuildableTileSet(isAttackTiles))
		{
			//highlight the tiles in the valid list
			highlightTilemapLayer.SetCell(tilePosition, 0, Vector2I.Zero);
		}
	}

	public void HighlightExpandedBuildableTiles(Rect2I tileArea, int radius)
	{

		// get a list of all valid tiles then convert it to a hashset to create exclusions.
		var validTiles = GetValidTilesInRadius(tileArea, radius).ToHashSet();
		//Creates a list of tiles to expand Green radius but excludes already valid tiles AND tiles that are occupied.
		var expandedTiles = validTiles.Except(validBuildableTiles).Except(occupiedTiles);
		// the atlas for the Green square.
		var atlasCoords = new Vector2I(1, 0);
		// look through the valid list of tiles
		foreach(var tilePosition in expandedTiles)
		{
			//highlight the tiles in the expanded list
			highlightTilemapLayer.SetCell(tilePosition, 0, atlasCoords);
		}
	}

	public void HighlightAttackTiles(Rect2I tileArea, int radius)
	{
		var buildingAreaTiles = tileArea.ToTiles();
		var validTiles = GetValidTilesInRadius(tileArea, radius).ToHashSet()
			.Except (validBuildableAttackTiles)
			.Except(buildingAreaTiles);
		
		// the atlas for the Green square.
		var atlasCoords = new Vector2I(1, 0);
		// look through the valid list of tiles
		foreach (var tilePosition in validTiles)
		{
			//highlight the tiles in the expanded list
			highlightTilemapLayer.SetCell(tilePosition, 0, atlasCoords);
		}
	}

	public void HighlightResourcetiles(Rect2I tileArea, int radius)
	{
		var resourceTiles = GetResourceTilesInRadius(tileArea, radius);
		// the atlas for the Green square.
		var atlasCoords = new Vector2I(1, 0);
		// look through the valid list of tiles
		foreach(var tilePosition in resourceTiles)
		{
			//highlight the tiles in the expanded list
			highlightTilemapLayer.SetCell(tilePosition, 0, atlasCoords);
		}
	}
	//clear existing highlighted tile to make way for new ones.
	public void ClearHighlightedTiles()
	{
		highlightTilemapLayer.Clear();
	}

	public Vector2I GetMouseGridCellPostionWithDimensionOffset(Vector2 dimensions)
	{		
		//dividing to reintroduce a nonIntager value for more accuracy.
		var mouseGridPosition = highlightTilemapLayer.GetGlobalMousePosition() / 64;
		mouseGridPosition -= dimensions / 2;// wouldnt this offset method only work if the dimensions of the building is 2x2?
		//Instead of flooring, its rounded to center it in the dimensions.
		mouseGridPosition = mouseGridPosition.Round();
		return new Vector2I((int)mouseGridPosition.X, (int)mouseGridPosition.Y);
	}

	public Vector2I GetMouseGridCellPostion()
	{
		var mousePosition = highlightTilemapLayer.GetGlobalMousePosition();
		return ConvertWorldPositionToTilePosition(mousePosition);
	}

	public Vector2I ConvertWorldPositionToTilePosition(Vector2 worldPosition)
	{
		var tilePosition = worldPosition / 64;
		tilePosition = tilePosition.Floor();
		return new Vector2I((int)tilePosition.X, (int)tilePosition.Y);
	}

	public bool CanDestroyBuilding(BuildingComponent toDestroyBuildingComponent)
	{
		if (toDestroyBuildingComponent.BuildingResource.BuildableRadius > 0)
		{
			return !WillBuildingDestructionCreateOrphanBuildings(toDestroyBuildingComponent) &&
				IsBuildingNetworkConnected(toDestroyBuildingComponent);
		}
		else if (toDestroyBuildingComponent.BuildingResource.IsAttackBuilding())
		{
			return CanDestroyBarracks(toDestroyBuildingComponent);
		}
		return true;
	}

	public HashSet<Vector2I> GetCollectedResourceTiles()
	{
		return collectedResourceTiles.ToHashSet();
	}

	private bool CanDestroyBarracks(BuildingComponent toDestroyBuildingComponent)
	{
		var disabledDangerBuilding = BuildingComponent.GetDangerBuildingComponents(this)
			.Where((buildingComponent) => buildingComponent.GetOccupiedCellPositions().Any((tilePosition) =>
			{
				return attackBuildingToTiles[toDestroyBuildingComponent].Contains(tilePosition);
			}));

		if (!disabledDangerBuilding.Any()) return true;

		var allDangerBuildingsStillDisabled = disabledDangerBuilding.All((dangerBuilding) =>
		{
			return dangerBuilding.GetOccupiedCellPositions().Any((tilePosition) =>
			{
				return attackBuildingToTiles.Keys.Where((attackBuilding) => attackBuilding != toDestroyBuildingComponent)
					.Any((attackBuilding) => attackBuildingToTiles[attackBuilding].Contains(tilePosition));
			});
		});

		if (allDangerBuildingsStillDisabled) return true;

		var nonDangerBuildings = BuildingComponent.GetNonDangerBuildingComponents(this).Where((nonDangerBuilding) =>
		{
			return nonDangerBuilding != toDestroyBuildingComponent;
		});
		var anyDangerBuildingContainsPlayerBuilding = disabledDangerBuilding.Any((dangerBuilding) =>
		{
			var dangerTiles = dangerBuildingToTiles[dangerBuilding];
			return nonDangerBuildings.Any((nonDangerBuilding) =>
			{
				return nonDangerBuilding.GetOccupiedCellPositions().Any((tilePosition) => dangerTiles.Contains(tilePosition));
			});
		});

		return !anyDangerBuildingContainsPlayerBuilding;
	}

	private bool WillBuildingDestructionCreateOrphanBuildings(BuildingComponent toDestroyBuildingComponent)
	{
		var dependentBuildings = BuildingComponent.GetNonDangerBuildingComponents(this)
			.Where((buildingComponent) =>
			{
				if (buildingComponent == toDestroyBuildingComponent) return false;
				if (buildingComponent.BuildingResource.IsBase) return false;
				var anyTilesInRadius = buildingComponent.GetOccupiedCellPositions()
					.All((tilePosition) => buildingToBuildableTiles[toDestroyBuildingComponent].Contains(tilePosition));
				return  anyTilesInRadius;
			});

		var allBuildingsStillValid = dependentBuildings.All((dependentBuilding) => 
		{
			var tilesforBuilding = dependentBuilding.GetOccupiedCellPositions();
			var buildingToCheck = buildingToBuildableTiles.Keys
					.Where((key) => key != toDestroyBuildingComponent && key != dependentBuilding);
			return tilesforBuilding.All((tilePosition) =>
			{
				var tileIsInSet = buildingToCheck
					.Any((buildingComponent) => buildingToBuildableTiles[buildingComponent].Contains(tilePosition));
					return tileIsInSet;
			});
		});

		if (!allBuildingsStillValid)
		{
			return true;
		}
		return false;
	}

	private bool IsBuildingNetworkConnected(BuildingComponent toDestroyBuildingComponent)
	{
		var baseBuilding = BuildingComponent.GetValidBuildingComponents(this)
			.First((buildingComponent) => buildingComponent.BuildingResource.IsBase);

		var visitedBuildings = new HashSet<BuildingComponent>();
		VisitAllConnectedBuildings(baseBuilding, toDestroyBuildingComponent, visitedBuildings);

		var totalBuildingsToVisit = BuildingComponent.GetValidBuildingComponents(this)
			.Count((buildingComponent) => 
			{
				return buildingComponent != toDestroyBuildingComponent && buildingComponent.BuildingResource.BuildableRadius > 0;
			});

		return totalBuildingsToVisit == visitedBuildings.Count;
	}

	private void VisitAllConnectedBuildings(
		BuildingComponent rootBuilding, 
		BuildingComponent excludeBuilding, 
		HashSet<BuildingComponent> visitedBuildings)
	{
		var dependentBuildings = BuildingComponent.GetNonDangerBuildingComponents(this)
			.Where((buildingComponent) =>
			{
				//dependentBuildings should have a Buildable radius greater than 0
				if (buildingComponent.BuildingResource.BuildableRadius == 0) return false;
				// ignore buildings already checked to avoid endless loop
				if (visitedBuildings.Contains(buildingComponent)) return false;

				var anyTilesInRadius = buildingComponent.GetOccupiedCellPositions()
					.Any((tilePosition) => buildingToBuildableTiles[rootBuilding].Contains(tilePosition));
				return buildingComponent != excludeBuilding && anyTilesInRadius;
			}).ToList();
		
		visitedBuildings.UnionWith(dependentBuildings);

		// use recursion to work through all the building with the updated arguments
		foreach (var dependentBuilding in dependentBuildings)
		{
			VisitAllConnectedBuildings(dependentBuilding, excludeBuilding, visitedBuildings);
		}
	}

	private HashSet<Vector2I>GetBuildableTileSet(bool isAttackTiles = false)
	{
		return isAttackTiles ? validBuildableAttackTiles : validBuildableTiles;
	}

	private List<TileMapLayer> GetAllTilemapLayers(Node2D rootNode)
	{
		var	tiles = new List<TileMapLayer>();
		var children = rootNode.GetChildren();
		children.Reverse();
		foreach (var child in children)
		{
			if (child is Node2D childNode)
			{
				tiles.AddRange(GetAllTilemapLayers(childNode));
			}
		}
		if(rootNode is TileMapLayer tileMapLayer)
		{
			tiles.Add(tileMapLayer);
		}
		return tiles;
	}

	//separate the distict layers to store them in the dictionary.
	private void MapTileLayerToElevationLayer()
	{
		foreach (var layer in allTilemaplayers)
		{
			ElevationLayer elevationLayer;
			Node startNode = layer;
			do{
				var parent = startNode.GetParent();
				elevationLayer = parent as ElevationLayer;
				startNode = parent;
			} while (elevationLayer == null && startNode != null);

			tileMapLayerToElevationLayer[layer] = elevationLayer;
		}
	}

	private void UpdateDangerOccupiedTiles(BuildingComponent buildingComponent)
	{
		occupiedTiles.UnionWith(buildingComponent.GetOccupiedCellPositions());
		
		if (buildingComponent.BuildingResource.IsDangerBuilding())
		{
			var tileArea = buildingComponent.GetTileArea();
			var tilesInRadius = GetValidTilesInRadius(tileArea, buildingComponent.BuildingResource.DangerRadius).ToHashSet();

			dangerBuildingToTiles[buildingComponent] = tilesInRadius.ToHashSet();

			if (!buildingComponent.IsDisabled && buildingComponent.BuildingResource.IsDangerBuilding())
			{
				tilesInRadius.ExceptWith(occupiedTiles);
				dangerOccupiedTiles.UnionWith(tilesInRadius);
			}
		}
	}

	private void UpdateValidBuildableTiles(BuildingComponent buildingComponent)
	{
		occupiedTiles.UnionWith(buildingComponent.GetOccupiedCellPositions());
		var tileArea = buildingComponent.GetTileArea();


		if (buildingComponent.BuildingResource.BuildableRadius > 0)
		{
			var allTiles = GetTilesInRadius(tileArea, buildingComponent.BuildingResource.BuildableRadius, (_) => true);
			allTilesInBuildingRadius.UnionWith(allTiles);

			var validTiles = GetValidTilesInRadius(tileArea, buildingComponent.BuildingResource.BuildableRadius);
			buildingToBuildableTiles[buildingComponent] = validTiles.ToHashSet();
			validBuildableTiles.UnionWith(validTiles);//union let the user add a collection of tiles to the hash instead of just one cell
		}

		validBuildableTiles.ExceptWith(occupiedTiles);//remove any tiles that are currently occupied from the valid list of buildable tiles.
	 	validBuildableAttackTiles.UnionWith(validBuildableTiles);

		validBuildableTiles.ExceptWith(dangerOccupiedTiles);//prevents tiles from around the goblin camp from being used
		EmitSignal(SignalName.GridStateUpdated);//emit signal about the change in tiles for win condition
	}

	private void UpdateCollectedResourceTiles(BuildingComponent buildingComponent)
	{
		var tileArea = buildingComponent.GetTileArea();
		var resourceTiles = GetResourceTilesInRadius(tileArea, buildingComponent.BuildingResource.ResourceRadius);	

		var oldResourceTileCount = collectedResourceTiles.Count;
		collectedResourceTiles.UnionWith(resourceTiles);

		if(oldResourceTileCount != collectedResourceTiles.Count)
		{
			EmitSignal(SignalName.ResourceTilesUpdated, collectedResourceTiles.Count);
		}
		//emit signal about the change in tiles for win condition
		EmitSignal(SignalName.GridStateUpdated);
	}

	private void UpdateAttackTiles(BuildingComponent buildingComponent)
	{
		if (!buildingComponent.BuildingResource.IsAttackBuilding()) return;

		var tileArea = buildingComponent.GetTileArea();
		var newAttackTiles = GetTilesInRadius(tileArea, buildingComponent.BuildingResource.AttackRadius, (_) => true).ToHashSet();
		attackBuildingToTiles[buildingComponent] = newAttackTiles;
		attackTiles.UnionWith(newAttackTiles);
	}

	private void RecalculateGrid()
	{
		occupiedTiles.Clear();//clear the grid cell that held the building.
		validBuildableTiles.Clear();//clear all buildable tiles from the grid.
	 	validBuildableAttackTiles.Clear();
		allTilesInBuildingRadius.Clear();
		collectedResourceTiles.Clear();//clear resources that have been gained
		dangerOccupiedTiles.Clear();
		attackTiles.Clear();
		buildingToBuildableTiles.Clear();//reset when ever we clear a building.
		dangerBuildingToTiles.Clear();
		attackBuildingToTiles.Clear();
		
		
		var buildingComponents = BuildingComponent.GetValidBuildingComponents(this);

		foreach(var buildingComponent in buildingComponents)
		{
			UpdateBuildComponentGridState(buildingComponent);
		}

		CheckDangerBuildingDestruction();

		//tells game to check resource count.
		EmitSignal(SignalName.ResourceTilesUpdated, collectedResourceTiles.Count);
		//emit signal about the change in tiles for win condition
		EmitSignal(SignalName.GridStateUpdated);
	}

	private void RecalculateDangerOccupiedTiles()
	{
		dangerOccupiedTiles.Clear();
		var dangerBuildings = BuildingComponent.GetDangerBuildingComponents(this);
		foreach (var building in dangerBuildings)
		{
			UpdateDangerOccupiedTiles(building);
		}
	}

	private void CheckDangerBuildingDestruction()
	{
		var dangerBuildings = BuildingComponent.GetDangerBuildingComponents(this);
		foreach (var building in dangerBuildings)
		{
			var tileArea = building.GetTileArea();
			var isInsideAttackTile = tileArea.ToTiles().Any((tilePosition) => attackTiles.Contains(tilePosition));
			if (isInsideAttackTile)
			{
				building.Disable();			
			}
			else
			{
				building.Enable();
			}
		}
	}


	private bool IsTileInsideCircle(Vector2 centerPosition, Vector2 tilePosition, float radius)
	{
		var distanceX = centerPosition.X - (tilePosition.X + .5);
		var distanceY = centerPosition.Y - (tilePosition.Y + .5);
		var distanceSquarded = (distanceX * distanceX) + (distanceY * distanceY);
		return distanceSquarded <= radius * radius;
	}

	private List<Vector2I> GetTilesInRadius(Rect2I tileArea, int radius, Func<Vector2I, bool> filterFn)
	{
		var	tiles = new List<Vector2I>();
		var tileAreaF = tileArea.ToRect2F();
		var tileAreaCenter = tileAreaF.GetCenter();
		var radiusMod = Mathf.Max(tileAreaF.Size.X, tileAreaF.Size.Y) / 2;

		for (var x = tileArea.Position.X - radius; x < tileArea.End.X + radius; x++)
		{
			for (var y = tileArea.Position.Y - radius; y < tileArea.End.Y + radius; y++)
			{
				var tilePosition = new Vector2I(x, y);
				//call the filterfn function with the custom arguments above for each cell in the radius
				if (!IsTileInsideCircle(tileAreaCenter, tilePosition, radius + radiusMod) || !filterFn(tilePosition)) continue;
				//If tile is valid add it to the list
				tiles.Add(tilePosition);
				
			} 
		}
		return	tiles;
	}

	// get the radius of a certain cell that are valid
	private List<Vector2I> GetValidTilesInRadius(Rect2I tileArea, int radius)
	{
		return GetTilesInRadius(tileArea, radius, (tilePosition) => 
		{
			return GetTileCustomData(tilePosition, IS_BUILDABLE).Item2;
		});
	}

	private List<Vector2I> GetResourceTilesInRadius(Rect2I tileArea, int radius)
	{
		return GetTilesInRadius(tileArea, radius, (tilePosition) => 
		{
			return GetTileCustomData(tilePosition, IS_WOOD).Item2;
		});
	}

	private void UpdateBuildComponentGridState(BuildingComponent buildingComponent)
	{
		UpdateDangerOccupiedTiles(buildingComponent);//update this first, Validbuildable is dependent on it.
		UpdateValidBuildableTiles(buildingComponent);
		UpdateCollectedResourceTiles(buildingComponent);
		UpdateAttackTiles(buildingComponent);
	}

	private void OnBuildingPlaced(BuildingComponent buildingComponent)
	{
		UpdateBuildComponentGridState(buildingComponent);
		CheckDangerBuildingDestruction();
	}

	private void OnBuildingDestroyed(BuildingComponent buildingComponent)
	{
		RecalculateGrid();
	}

	private void OnBuildingEnabled(BuildingComponent buildingComponent)
	{
		UpdateBuildComponentGridState(buildingComponent);
	}

	private void OnBuildingDisabled(BuildingComponent buildingComponent)
	{
		RecalculateGrid();
	}

}
