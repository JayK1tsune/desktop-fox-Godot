using Godot;
using System;

public partial class Ui : Control
{
	ClickThrough clickThrough;
	[Export] FoxButtons_UI button;
	[Export] private ColourButton colorPickerButton;

	public FoxPet FoxPetScript;
	private AnimatedSprite2D _foxSprite;
	private Sprite2D _foxSprite2D;


	public override void _Ready()
	{
		clickThrough = GetNode<ClickThrough>("/root/Window/ClickThrough");
		FoxPetScript = GetNode<FoxPet>("/root/Window/Fox");
		clickThrough.SetClickThrough(false);
		colorPickerButton.KeepClickThrough += KeepUiActive;   // Connect button signals
		button.CloseUi += DisableUi; // Connect button signals
		colorPickerButton.StopClickThrough += DisabledUi; // Connect button signals
		_foxSprite = GetNode<AnimatedSprite2D>("/root/Window/Fox/Sprite");
	}



	public void KeepUiActive()
	{
		FoxPetScript._uiActive = true;
	}
	public void DisabledUi()
	{
		FoxPetScript._uiActive = false;
	}

	public void DisableUi()
	{
		FoxPetScript._uiActive = false;
		this.Visible = false;
	}
}
