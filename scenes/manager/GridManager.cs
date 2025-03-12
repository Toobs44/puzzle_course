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
	public delegate void ResourceTilesUpdateEventHandler( int collectedTiles);
	[Signal]
	public delegate void GridStateUpdatedEventHandler();

	//what is a HashSet maintain a unique list of elements
	private HashSet<Vector2I> validBuildableTiles = new();
	private HashSet<Vector2I> allTilesInBuildingRadius = new();
	private HashSet<Vector2I> collectedResourseTiles = new();
	private HashSet<Vector2I> occupiedTiles = new();
	
	[Export]
	private TileMapLayer highlightTilemapLayer;
	[Export]
	private TileMapLayer baseTerrainTilemapLayer;

	private List<TileMapLayer> allTilemaplayers = new();
	private Dictionary<TileMapLayer, ElevationLayer> tileMapLayerToElevationLayer = new();

    public override void _Ready()
    {
		//to prevent errors from freed nodes during scene changes the longer signal call is being used.
		GameEvents.Instance.Connect(GameEvents.SignalName.BuildingPlaced, Callable.From<BuildingComponent>(OnBuildingPlaced));
		GameEvents.Instance.Connect(GameEvents.SignalName.BuildingDestroyed, Callable.From<BuildingComponent>(OnBuildingDestroyed));
		allTilemaplayers = GetallTilemapLayers(baseTerrainTilemapLayer);
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

	public bool IsTilePositionBuildable(Vector2I tilePosition)
	{
		//checking to see if the tile position is in the valid list.
		return validBuildableTiles.Contains(tilePosition);
	}

	public bool IsTilePostionInAnyBuildingRadius(Vector2I tilePosition)
	{
		return allTilesInBuildingRadius.Contains(tilePosition);
	}

	
	public bool IsTileAreaBuildable(Rect2I tileArea)
	{
		var	tiles = tileArea.ToTiles();

		if (tiles.Count == 0) return false;

		//check if all tiles are in the same elevation layer
		//Underscore discards the second value in the tuple.
		(TileMapLayer firstTileMaperLayer, _) = GetTileCustomData(tiles[0], IS_BUILDABLE);
		var targetElevationLayer = firstTileMaperLayer != null? tileMapLayerToElevationLayer[firstTileMaperLayer] : null;

		return tiles.All((tilePosition) =>
		{
			(TileMapLayer tileMapLayer, bool isBuildable) = GetTileCustomData(tilePosition, IS_BUILDABLE);
			var elevationLayer = tileMapLayer != null ? tileMapLayerToElevationLayer[tileMapLayer] : null;
			return isBuildable && validBuildableTiles.Contains(tilePosition) && elevationLayer == targetElevationLayer;
		});
	}

	public void HighlightBuildabletiles()
	{
		// look through the valid list of tiles
		foreach(var tilePosition in validBuildableTiles)
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
		var tilePostion = worldPosition / 64;
		tilePostion = tilePostion.Floor();
		return new Vector2I((int)tilePostion.X, (int)tilePostion.Y);
	}

	private List<TileMapLayer> GetallTilemapLayers(Node2D rootNode)
	{
		var	tiles = new List<TileMapLayer>();
		var children = rootNode.GetChildren();
		children.Reverse();
		foreach (var child in children)
		{
			if (child is Node2D childNode)
			{
				tiles.AddRange(GetallTilemapLayers(childNode));
			}
		}
		if(rootNode is TileMapLayer tileMapLayer)
		{
			tiles.Add(tileMapLayer);
		}
		return	tiles;
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

	private void UpDateValidBuildableTiles(BuildingComponent buildingComponent)
	{
		occupiedTiles.UnionWith(buildingComponent.GetOccupiedCellPositions());
		var rootCell = buildingComponent.GetGridCellPosition();
		var tileArea = new Rect2I(rootCell, buildingComponent.BuildingResource.Dimensions);

		var allTiles = GetTilesInRadius(tileArea, buildingComponent.BuildingResource.BuildableRadius, (_) => true);

		var validTiles = GetValidTilesInRadius(tileArea, buildingComponent.BuildingResource.BuildableRadius);
		allTilesInBuildingRadius.UnionWith(allTiles);
		//union let the user add a collection of tiles to the hash instead of just one cell
		validBuildableTiles.UnionWith(validTiles);

		//remove any tiles that are currently occupied from the valid list of buildable tiles.
		validBuildableTiles.ExceptWith(occupiedTiles);

		//emit signal about the change in tiles for win condition
		EmitSignal(SignalName.GridStateUpdated);
	}

	private void UpdateCollectedResourceTiles(BuildingComponent buildingComponent)
	{
		var rootCell = buildingComponent.GetGridCellPosition();
		var tileArea = new Rect2I(rootCell, buildingComponent.BuildingResource.Dimensions);
		var resourceTiles = GetResourceTilesInRadius(tileArea, buildingComponent.BuildingResource.ResourceRadius);	

		var oldResourceTileCount = collectedResourseTiles.Count;
		collectedResourseTiles.UnionWith(resourceTiles);

		if(oldResourceTileCount != collectedResourseTiles.Count)
		{
			EmitSignal(SignalName.ResourceTilesUpdate, collectedResourseTiles.Count);
		}
		//emit signal about the change in tiles for win condition
		EmitSignal(SignalName.GridStateUpdated);
	}

	private void RecalculateGrid()
	{
		occupiedTiles.Clear();//clear the grid cell that held the building.
		validBuildableTiles.Clear();//clear all buildable tiles from the grid.
		allTilesInBuildingRadius.Clear();
		collectedResourseTiles.Clear();//clear resources that have been gained
		
		
		var buildingComponents = BuildingComponent.GetValidBuildingComponents(this);

		foreach(var buildingComponent in buildingComponents)
		{
			UpDateValidBuildableTiles(buildingComponent);
			UpdateCollectedResourceTiles(buildingComponent);
		}

		//tells game to check resource count.
		EmitSignal(SignalName.ResourceTilesUpdate, collectedResourseTiles.Count);
		//emit signal about the change in tiles for win condition
		EmitSignal(SignalName.GridStateUpdated);
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

	private void OnBuildingPlaced(BuildingComponent buildingComponent)
	{
		UpDateValidBuildableTiles(buildingComponent);
		UpdateCollectedResourceTiles(buildingComponent);
	}

	private void OnBuildingDestroyed(BuildingComponent buildingComponent)
	{
		RecalculateGrid();
	}

}
