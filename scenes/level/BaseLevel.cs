using Game.Manager;
using Game.Resources.Level;
using Game.UI;
using Godot;

namespace Game;

public partial class BaseLevel : Node
{
	[Export]
	private PackedScene levelCompleteScreenScene;
	[Export]
	private LevelDefinitionResource levelDefinitionResource;

	private GridManager gridManager;
	private GoldMine goldMine;
	private GameCamera gameCamera;
	private TileMapLayer baseTerrainTilemapLayer;
	private Node2D baseBuilding;
	private GameUI gameUI;
	private BuildingManager buildingManager;

	public override void _Ready()
	{
		gridManager = GetNode<GridManager>("GridManager");
		goldMine = GetNode<GoldMine>("%GoldMine");
		gameCamera = GetNode<GameCamera>("GameCamera");
		baseTerrainTilemapLayer = GetNode<TileMapLayer>("%BaseTerrainTileMapLayer");
		baseBuilding = GetNode<Node2D>("%Base");
		gameUI = GetNode<GameUI>("GameUI");
		buildingManager = GetNode<BuildingManager>("BuildingManager");

		buildingManager.SetStartingResourceCount(levelDefinitionResource.StartingRescourceCount);

		//set the camera limit to rect used in the base terrain layer.
		gameCamera.SetBoundingRect(baseTerrainTilemapLayer.GetUsedRect());
		//center camera on the base building.
		gameCamera.CenterOnPostion(baseBuilding.GlobalPosition);

		gridManager.GridStateUpdated += OnGridStateUpdated;
	}


	private void OnGridStateUpdated()
	{
		//checking if the grid manager is in the valid buildable area.
		var goldMineTilePostion = gridManager.ConvertWorldPositionToTilePosition(goldMine.GlobalPosition);
		if (gridManager.IsTilePostionInAnyBuildingRadius(goldMineTilePostion))
		{
			var levelCompleteScreen = levelCompleteScreenScene.Instantiate<LevelCompleteScreen>();
			AddChild(levelCompleteScreen);
			goldMine.SetActive();
			gameUI.HideUI();
		}
	}
}
