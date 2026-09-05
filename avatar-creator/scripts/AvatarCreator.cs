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

	private void ApplyToAvatar()
	{
		foreach (var selector in _selectors)
			_data.SetPartId(selector.Part, selector.CurrentId);

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
}
