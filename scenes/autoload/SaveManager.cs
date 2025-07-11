using System;
using Game.Resources.Level;
using Godot;
using Newtonsoft.Json;

namespace Game.Autoload;

public partial class SaveManager : Node
{
    public static SaveManager Instance { get; private set; }
    private static SaveData saveData = new();
    private static readonly string SAVE_FILE_PATH = "user://save.json";

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
        {
            Instance = this;
            LoadSaveData();
            GetAudioSettings();
        }
    }

    public static bool IsLevelCompleted(string levelId)
    {
        saveData.LevelCompletionStatus.TryGetValue(levelId, out var data);

        return data?.IsCompleted == true;
    }

    public static bool IsSavedWindowFull()
    {
        saveData.WindowMode.TryGetValue("IsFullScreen", out var data);
        return data;
    }

    public static void SaveLevelCompletion(LevelDefinitionResource levelDefinitionResource)
    {
        saveData.SaveLevelCompletion(levelDefinitionResource.Id, true);
        WriteSaveData();
    }

    public static void SaveWindowMode(string windowMode)
    {
        GD.Print($"window mode selected: {windowMode}");
        if (windowMode == "Windowed")
            saveData.SaveWindowMode(false);
        if (windowMode == "ExclusiveFullscreen")
            saveData.SaveWindowMode(true);
        WriteSaveData();
    }

    public static void SaveAudioBusVolume(string busName, float value)
    {
        saveData.SaveAudioBusValue(busName, value);
        WriteSaveData();
    }


    private void GetAudioSettings()
    {
    	var sfxValue = GetLoadedBusVolume("SFX");
    	var musicValue = GetLoadedBusVolume("Music");
    	OptionsHelper.SetBusVolumePercent("SFX", sfxValue);
    	OptionsHelper.SetBusVolumePercent("Music", musicValue);
    }
    
    public static float GetLoadedBusVolume(string busName)
    {
        GD.Print($"GetAudioBusVolume called with the bus name of {busName}");
        if (saveData.SFXBusValue.ContainsKey(busName))
        {
            saveData.SFXBusValue.TryGetValue(busName, out var value);
            return value;
        }
        else if (saveData.MusicBusValue.ContainsKey(busName))
        {
            saveData.MusicBusValue.TryGetValue(busName, out var value);
            return value;
        }
        else
        {
            GD.Print("Non found setting default");
            return 0.5f;
        }
    }

    private static void WriteSaveData()
    {
        var dataString = JsonConvert.SerializeObject(saveData);
        using var saveFile = FileAccess.Open(SAVE_FILE_PATH, FileAccess.ModeFlags.Write);
        saveFile.StoreLine(dataString);
    }

    private static void LoadSaveData()
    {
        if (!FileAccess.FileExists(SAVE_FILE_PATH))
        {
            return;
        }

        using var saveFile = FileAccess.Open(SAVE_FILE_PATH, FileAccess.ModeFlags.Read);
        var dataString = saveFile.GetLine();

        try
        {
            saveData = JsonConvert.DeserializeObject<SaveData>(dataString);
        }
        catch (Exception)
        {
            GD.PushWarning("Save file json was corrupted");
        }
    }
}
