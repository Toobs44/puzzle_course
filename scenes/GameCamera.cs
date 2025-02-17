using Godot;

namespace Game;

public partial class GameCamera : Camera2D
{
	private const int TILE_SIZE = 64;
	private const float PAN_SPEED = 500;
	private readonly StringName ACTION_PAN_LEFT = "pan_left";
	private readonly StringName ACTION_PAN_RIGHT = "pan_right";
	private readonly StringName ACTION_PAN_UP = "pan_up";
	private readonly StringName ACTION_PAN_DOWN = "pan_down";

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		GlobalPosition = GetScreenCenterPosition();
		var movementVector = Input.GetVector(ACTION_PAN_LEFT, ACTION_PAN_RIGHT, ACTION_PAN_UP, ACTION_PAN_DOWN);
		//change global position in the direction by PAN_SPEED px per second.
		GlobalPosition += movementVector * PAN_SPEED * (float)delta;
	}

	public void SetBoundingRect(Rect2I boundingrect)
	{
		LimitLeft = boundingrect.Position.X * TILE_SIZE;
		LimitRight = boundingrect.End.X * TILE_SIZE;
		LimitTop = boundingrect.Position.Y * TILE_SIZE;
		LimitBottom = boundingrect.End.Y * TILE_SIZE;
	}

	public void CenterOnPostion(Vector2 postion)
	{
		GlobalPosition = postion;
	}
}
