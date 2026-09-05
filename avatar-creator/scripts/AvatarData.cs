using Godot;

[GlobalClass]
public partial class AvatarData : Resource
{
	[Export]
	public Color Background { get; set; } = new(0.23392546f, 0.23392546f, 0.23392543f);

	[Export]
	public string BackHair { get; set; } = "";

	[Export]
	public string Face { get; set; } = "man_00";

	[Export]
	public string Clothes { get; set; } = "man_00";

	[Export]
	public string Eyes { get; set; } = "man_00";

	[Export]
	public string Eyebrows { get; set; } = "man_00";

	[Export]
	public string Nose { get; set; } = "man_00";

	[Export]
	public string Mouth { get; set; } = "man_00";

	[Export]
	public string FrontHair { get; set; } = "man_00";

	public string GetPartId(string part)
	{
		return part switch
		{
			AvatarCatalog.BackHair => BackHair ?? "",
			AvatarCatalog.Face => Face ?? "",
			AvatarCatalog.Clothes => Clothes ?? "",
			AvatarCatalog.Eyes => Eyes ?? "",
			AvatarCatalog.Eyebrows => Eyebrows ?? "",
			AvatarCatalog.Nose => Nose ?? "",
			AvatarCatalog.Mouth => Mouth ?? "",
			AvatarCatalog.FrontHair => FrontHair ?? "",
			_ => ""
		};
	}
}
