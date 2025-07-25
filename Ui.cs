using Godot;
using System;

public partial class Ui : Control
{
	[Export] ClickThrough clickThrough;
	[Export] FoxButtons_UI button;
	[Export] private ColourButton colorPickerButton;

	[Signal] public delegate void UiActiveEventHandler();


	public override void _Ready()
	{
		clickThrough.SetClickThrough(false);
		colorPickerButton.KeepClickThrough += keepUiClickActive;   // Connect button signals
		button.CloseUi += DisableUi; // Connect button signals

	}






	public void keepUiClickActive()
	{
		KeepUiActive();
		GD.Print("Click-through state set to: false");
	}

	public void KeepUiActive()
	{
		clickThrough.SetClickThrough(false);
		GD.Print("UI is active, click-through disabled");
		EmitSignal(SignalName.UiActive);
	}

	public void DisableUi()
	{
		this.Visible = false;
	}
}
