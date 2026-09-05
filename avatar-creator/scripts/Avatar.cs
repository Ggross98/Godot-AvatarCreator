using System.Collections.Generic;
using Godot;

public partial class Avatar : Control
{
	[Export]
	public AvatarData Data { get; set; }

	private ColorRect _background;
	private readonly Dictionary<string, TextureRect> _layers = new();

	public override void _Ready()
	{
		FitChildrenToParent();
		CacheLayers();
		Apply(Data ?? new AvatarData());
	}

	public void Apply(AvatarData data)
	{
		Data = data;
		if (data == null)
		{
			if (_background != null)
				_background.Visible = false;

			foreach (var layer in _layers.Values)
				HideLayer(layer);
			return;
		}

		if (_background != null)
		{
			_background.Color = data.Background;
			_background.Visible = true;
		}

		foreach (var part in AvatarCatalog.Parts)
		{
			if (!_layers.TryGetValue(part, out var layer))
				continue;

			ApplyLayer(layer, part, data.GetPartId(part));
		}
	}

	public void RandomizeAppearance(RandomNumberGenerator rng = null)
	{
		Apply(AvatarCatalog.CreateRandom(rng));
	}

	private void FitChildrenToParent()
	{
		foreach (var child in GetChildren())
		{
			if (child is not Control control)
				continue;

			control.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			if (control is TextureRect textureRect)
				textureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		}
	}

	private void CacheLayers()
	{
		_background = GetNodeOrNull<ColorRect>("background");
		_layers.Clear();

		foreach (var part in AvatarCatalog.Parts)
		{
			var layer = GetNodeOrNull<TextureRect>(part);
			if (layer == null)
			{
				GD.PushWarning($"Avatar is missing TextureRect '{part}'.");
				continue;
			}

			_layers[part] = layer;
		}
	}

	private static void ApplyLayer(TextureRect layer, string part, string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			HideLayer(layer);
			return;
		}

		var path = AvatarCatalog.GetTexturePath(part, id);
		if (!ResourceLoader.Exists(path))
		{
			GD.PushWarning($"Avatar texture missing: {path}");
			HideLayer(layer);
			return;
		}

		layer.Texture = GD.Load<Texture2D>(path);
		layer.Visible = true;
	}

	private static void HideLayer(TextureRect layer)
	{
		layer.Texture = null;
		layer.Visible = false;
	}
}
