using Godot;

namespace Game.Autoload;

public partial class OptionsHelper : Node
{
	public static OptionsHelper Instance { get; private set; }

	public override void _Notification(int what)
	{
		if (what == NotificationSceneInstantiated)
		{
			Instance = this;
			GetWindowMode();
		}
	}

	public static void SetBusVolumePercent(string busName, float volumePercent)
	{
		var busIndex = AudioServer.GetBusIndex(busName);
		GD.Print($"adjusting audio for {busName} to {volumePercent}");
		AudioServer.SetBusVolumeDb(busIndex, Mathf.LinearToDb(volumePercent));
		SaveManager.SaveAudioBusVolume(busName, volumePercent);
	}

	public static float GetBusVolumePercent(string busName)
	{
		var busIndex = AudioServer.GetBusIndex(busName);
		return Mathf.DbToLinear(AudioServer.GetBusVolumeDb(busIndex));
	}

	public static void ToggleWindowMode()
	{
		if (IsFullscreen())
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);

		}
		else
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
		}
		SaveManager.SaveWindowMode(DisplayServer.WindowGetMode().ToString());
	}

	public static bool IsFullscreen()
	{
		return DisplayServer.WindowGetMode() == DisplayServer.WindowMode.ExclusiveFullscreen;
	}

	private void GetWindowMode()
	{
		if (SaveManager.IsSavedWindowFull())
		{
			GD.Print($"IsFullScreen = {SaveManager.IsSavedWindowFull()}");
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
		}
		else
		{
			GD.Print($"IsFullScreen = {SaveManager.IsSavedWindowFull()}");
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		}
	}

}
