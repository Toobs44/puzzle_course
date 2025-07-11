//This is unused saving it for later

using System.Collections.Generic;
using Godot;

namespace Game;

public partial class OptionsData
{
	public Dictionary<string, Variant> Options { get; set; }
	public float MusicBusValue { get; set; }
	public float SFXBusValue { get; set; }
	public bool IsFullScreen { get; set; }
}
