using System;
using System.Collections.Generic;
using Godot;

public partial class AvatarCreator : Node2D
{
	private static readonly (string NodeName, string Part)[] PartRows =
	{
		("Face", AvatarCatalog.Face),
		("Eyes", AvatarCatalog.Eyes),
		("Eyebrows", AvatarCatalog.Eyebrows),
		("Nose", AvatarCatalog.Nose),
		("Mouth", AvatarCatalog.Mouth),
		("FrontHair", AvatarCatalog.FrontHair),
		("BackHair", AvatarCatalog.BackHair),
		("Clothes", AvatarCatalog.Clothes)
	};

	private Avatar _avatar;
	private CheckButton _male;
	private CheckButton _female;
	private ColorPaletteSelector _skinPalette;
	private ColorPaletteSelector _hairPalette;
	private readonly AvatarData _data = new();
	private readonly List<PartSelector> _selectors = new();
	private string _gender = AvatarCatalog.MaleGender;

	public override void _Ready()
	{
		_avatar = GetNode<Avatar>("Control/Avatar");
		_male = GetNode<CheckButton>("Control/AvatarSelection/Gender/Male");
		_female = GetNode<CheckButton>("Control/AvatarSelection/Gender/Female");

		_male.Disabled = !AvatarCatalog.HasGenderAssets(AvatarCatalog.MaleGender);
		_female.Disabled = !AvatarCatalog.HasGenderAssets(AvatarCatalog.FemaleGender);

		if (!_male.Disabled)
		{
			_gender = AvatarCatalog.MaleGender;
			_male.ButtonPressed = true;
		}
		else if (!_female.Disabled)
		{
			_gender = AvatarCatalog.FemaleGender;
			_female.ButtonPressed = true;
		}

		_male.Toggled += OnMaleToggled;
		_female.Toggled += OnFemaleToggled;

		foreach (var (nodeName, part) in PartRows)
		{
			var row = GetNode<HBoxContainer>($"Control/AvatarSelection/{nodeName}");
			_selectors.Add(new PartSelector(part, row, OnPartStep));
		}

		_skinPalette = new ColorPaletteSelector(
			GetNode<HBoxContainer>("Control/AvatarSelection/SkinColor"),
			AvatarCatalog.SkinTones,
			_data.SkinColor,
			AvatarCatalog.SkinTones.Length - 1,
			OnSkinColorSelected);
		_hairPalette = new ColorPaletteSelector(
			GetNode<HBoxContainer>("Control/AvatarSelection/HairColor"),
			AvatarCatalog.HairColors,
			_data.HairColor,
			4,
			OnHairColorSelected);

		RefreshSelectors();
		ApplyToAvatar();
	}

	private void OnMaleToggled(bool pressed)
	{
		if (pressed)
			SetGender(AvatarCatalog.MaleGender);
	}

	private void OnFemaleToggled(bool pressed)
	{
		if (pressed)
			SetGender(AvatarCatalog.FemaleGender);
	}

	private void SetGender(string gender)
	{
		if (_gender == gender)
			return;

		_gender = gender;
		RefreshSelectors();
		ApplyToAvatar();
	}

	private void OnPartStep(PartSelector selector, int delta)
	{
		selector.Step(delta);
		ApplyToAvatar();
	}

	private void RefreshSelectors()
	{
		foreach (var selector in _selectors)
			selector.Load(_gender);
	}

	private void OnSkinColorSelected(Color color)
	{
		_data.SkinColor = color;
		ApplyToAvatar();
	}

	private void OnHairColorSelected(Color color)
	{
		_data.HairColor = color;
		ApplyToAvatar();
	}

	private void ApplyToAvatar()
	{
		foreach (var selector in _selectors)
			_data.SetPartId(selector.Part, selector.CurrentId);

		_data.SkinColor = _skinPalette.Selected;
		_data.HairColor = _hairPalette.Selected;
		_avatar.Apply(_data);
	}

	private sealed class PartSelector
	{
		public string Part { get; }
		public string CurrentId => _ids.Length == 0 ? "" : _ids[_index];

		private readonly Label _number;
		private readonly Button _previous;
		private readonly Button _next;
		private string[] _ids = Array.Empty<string>();
		private int _index;

		public PartSelector(string part, Node row, Action<PartSelector, int> onStep)
		{
			Part = part;
			_number = row.GetNode<Label>("Number");
			_previous = row.GetNode<Button>("PreviousButton");
			_next = row.GetNode<Button>("NextButton");
			_previous.Pressed += () => onStep(this, -1);
			_next.Pressed += () => onStep(this, 1);
		}

		public void Load(string gender)
		{
			var ids = AvatarCatalog.GetIds(Part, gender);
			if (AvatarCatalog.AllowsNone(Part))
			{
				var withNone = new string[ids.Length + 1];
				withNone[0] = "";
				ids.CopyTo(withNone, 1);
				_ids = withNone;
				_index = ids.Length > 0 ? 1 : 0;
			}
			else
			{
				_ids = ids;
				_index = 0;
			}

			RefreshView();
		}

		public void Step(int delta)
		{
			if (_ids.Length == 0)
				return;

			_index = (_index + delta) % _ids.Length;
			if (_index < 0)
				_index += _ids.Length;

			RefreshView();
		}

		private void RefreshView()
		{
			var count = _ids.Length;
			if (count == 0)
			{
				_number.Text = "0/0";
			}
			else if (string.IsNullOrEmpty(CurrentId))
			{
				_number.Text = "无";
			}
			else
			{
				var noneOffset = AvatarCatalog.AllowsNone(Part) ? 1 : 0;
				_number.Text = $"{_index - noneOffset + 1}/{count - noneOffset}";
			}

			_previous.Disabled = count <= 1;
			_next.Disabled = count <= 1;
		}
	}

	private sealed class ColorPaletteSelector
	{
		public Color Selected { get; private set; }

		private readonly Color[] _colors;
		private readonly Button[] _buttons;
		private readonly Action<Color> _onSelected;
		private int _index;

		public ColorPaletteSelector(Node row, Color[] colors, Color initial, int fallbackIndex, Action<Color> onSelected)
		{
			_colors = colors;
			_onSelected = onSelected;
			_buttons = new Button[colors.Length];
			_index = IndexOf(colors, initial, fallbackIndex);
			Selected = colors[_index];

			var panel = row.GetNode<PanelContainer>("Panel");
			ApplyPanelStyle(panel);

			var swatches = panel.GetNode<HBoxContainer>("Swatches");
			swatches.AddThemeConstantOverride("separation", 4);

			for (var i = 0; i < colors.Length; i++)
			{
				var index = i;
				var button = CreateSwatch(colors[i]);
				button.Pressed += () => Select(index);
				swatches.AddChild(button);
				_buttons[i] = button;
			}

			RefreshSelection();
		}

		private void Select(int index)
		{
			_index = index;
			Selected = _colors[index];
			RefreshSelection();
			_onSelected(Selected);
		}

		private void RefreshSelection()
		{
			for (var i = 0; i < _buttons.Length; i++)
			{
				var style = CreateSwatchStyle(_colors[i], i == _index);
				_buttons[i].AddThemeStyleboxOverride("normal", style);
				_buttons[i].AddThemeStyleboxOverride("hover", style);
				_buttons[i].AddThemeStyleboxOverride("pressed", style);
			}
		}

		private static Button CreateSwatch(Color color)
		{
			var style = CreateSwatchStyle(color, false);
			var button = new Button
			{
				CustomMinimumSize = new Vector2(28, 28),
				SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
				SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
				FocusMode = Control.FocusModeEnum.None,
				MouseDefaultCursorShape = Control.CursorShape.PointingHand
			};
			button.AddThemeStyleboxOverride("normal", style);
			button.AddThemeStyleboxOverride("hover", style);
			button.AddThemeStyleboxOverride("pressed", style);
			button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
			return button;
		}

		private static StyleBoxFlat CreateSwatchStyle(Color color, bool selected)
		{
			var style = new StyleBoxFlat
			{
				BgColor = color
			};
			style.SetCornerRadiusAll(4);
			style.SetContentMarginAll(0);
			if (selected)
			{
				style.SetBorderWidthAll(2);
				style.BorderColor = Colors.White;
			}

			return style;
		}

		private static void ApplyPanelStyle(PanelContainer panel)
		{
			var style = new StyleBoxFlat
			{
				BgColor = new Color(0.08f, 0.08f, 0.08f),
				BorderColor = new Color(0.55f, 0.55f, 0.55f)
			};
			style.SetCornerRadiusAll(6);
			style.SetBorderWidthAll(1);
			style.SetContentMarginAll(6);
			panel.AddThemeStyleboxOverride("panel", style);
		}

		private static int IndexOf(Color[] colors, Color target, int fallback)
		{
			for (var i = 0; i < colors.Length; i++)
			{
				if (colors[i].IsEqualApprox(target))
					return i;
			}

			return fallback;
		}
	}
}
