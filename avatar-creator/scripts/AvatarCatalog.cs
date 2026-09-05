using System;
using System.Collections.Generic;
using Godot;

public static class AvatarCatalog
{
	public const string Root = "res://assets/avatars";

	public const string BackHair = "backhair";
	public const string Face = "face";
	public const string Clothes = "clothes";
	public const string Eyes = "eyes";
	public const string Eyebrows = "eyebrows";
	public const string Nose = "nose";
	public const string Mouth = "mouth";
	public const string FrontHair = "fronthair";

	public const string MaleGender = "man";
	public const string FemaleGender = "woman";

	public static readonly string[] Parts =
	{
		BackHair, Face, Clothes, Eyes, Eyebrows, Nose, Mouth, FrontHair
	};

	public static bool AllowsNone(string part)
	{
		return part is Eyebrows or FrontHair or BackHair;
	}

	public static string GetTexturePath(string part, string id)
	{
		return $"{Root}/{part}/{id}.png";
	}

	public static string[] GetIds(string part)
	{
		using var dir = DirAccess.Open($"{Root}/{part}");
		if (dir == null)
			return Array.Empty<string>();

		var ids = new List<string>();
		dir.ListDirBegin();
		while (true)
		{
			var fileName = dir.GetNext();
			if (fileName.Length == 0)
				break;
			if (dir.CurrentIsDir())
				continue;
			if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
				continue;

			ids.Add(System.IO.Path.GetFileNameWithoutExtension(fileName));
		}

		dir.ListDirEnd();
		ids.Sort(StringComparer.Ordinal);
		return ids.ToArray();
	}

	public static string[] GetIds(string part, string gender)
	{
		var prefix = gender + "_";
		var ids = GetIds(part);
		var filtered = new List<string>();
		foreach (var id in ids)
		{
			if (id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				filtered.Add(id);
		}

		return filtered.ToArray();
	}

	public static bool HasGenderAssets(string gender)
	{
		foreach (var part in Parts)
		{
			if (GetIds(part, gender).Length > 0)
				return true;
		}

		return false;
	}

	public static AvatarData CreateRandom(RandomNumberGenerator rng = null)
	{
		if (rng == null)
		{
			rng = new RandomNumberGenerator();
			rng.Randomize();
		}

		return new AvatarData
		{
			BackHair = PickId(BackHair, rng),
			Face = PickId(Face, rng),
			Clothes = PickId(Clothes, rng),
			Eyes = PickId(Eyes, rng),
			Eyebrows = PickId(Eyebrows, rng),
			Nose = PickId(Nose, rng),
			Mouth = PickId(Mouth, rng),
			FrontHair = PickId(FrontHair, rng)
		};
	}

	private static string PickId(string part, RandomNumberGenerator rng)
	{
		var ids = GetIds(part);
		return ids.Length == 0 ? "" : ids[(int)rng.RandiRange(0, ids.Length - 1)];
	}
}
