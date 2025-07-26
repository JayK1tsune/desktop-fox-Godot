using Godot;
using System;

public partial class Ui : Control
{
	[Export] ClickThrough clickThrough;
	[Export] FoxButtons_UI button;
	[Export] private ColourButton colorPickerButton;

	public FoxPet FoxPetScript;

	[Signal] public delegate void UiActiveEventHandler();
	[Signal] public delegate void UiDisabledEventHandler();


	public override void _Ready()
	{
		FoxPetScript = GetNode<FoxPet>("/root/Base/Fox");
		clickThrough.SetClickThrough(false);
		colorPickerButton.KeepClickThrough += KeepUiActive;   // Connect button signals
		button.CloseUi += DisableUi; // Connect button signals
		colorPickerButton.StopClickThrough += DisabledUi; // Connect button signals

	}








	public void KeepUiActive()
	{
		FoxPetScript._uiActive = true;
		clickThrough.SetClickThrough(false);
		GD.Print("UI is active, click-through disabled");
		EmitSignal(SignalName.UiActive);
	}
	public void DisabledUi()
	{
		FoxPetScript._uiActive = false;
		clickThrough.SetClickThrough(true);
		GD.Print("UI is disabled, click-through enabled");
		EmitSignal(SignalName.UiDisabled);
	}

	public void DisableUi()
	{
		FoxPetScript._uiActive = false;
		this.Visible = false;
	}
}
