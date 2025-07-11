using System.Collections.Generic;


namespace Game;

public class SaveData
{
    public Dictionary<string, LevelCompletionData> LevelCompletionStatus { get; private set; } = new();
    public Dictionary<string, float> MusicBusValue { get; private set; } = new();
    public Dictionary<string, float> SFXBusValue { get; private set; } = new();
    public Dictionary<string, bool> WindowMode { get; private set; } = new(); 



    public void SaveLevelCompletion(string id, bool completed)
    {
        if (!LevelCompletionStatus.ContainsKey(id))
        {
            LevelCompletionStatus[id] = new LevelCompletionData();
        }
        LevelCompletionStatus[id].IsCompleted = completed;
    }

    public void SaveAudioBusValue(string busName, float value)
    {
        switch (busName)
        {
            case "Music":
                if (!MusicBusValue.ContainsKey(busName))
                    MusicBusValue[busName] = new();
                MusicBusValue[busName] = value;
                break;
            case "SFX":
                if (!SFXBusValue.ContainsKey(busName))
                    SFXBusValue[busName] = new();
                SFXBusValue[busName] = value;
                break;
        }
    }


    public void SaveWindowMode(bool isFullScreen)
    {
        if (!WindowMode.ContainsKey("IsFullScreen"))
            WindowMode["IsFullScreen"] = new();
        WindowMode["IsFullScreen"] = isFullScreen;
        // IsFullScreen = isFullScreen;
    }

}