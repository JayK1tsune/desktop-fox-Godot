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

	[Signal] public delegate void UiActiveEventHandler();
	[Signal] public delegate void UiDisabledEventHandler();


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

	public override void _Input(InputEvent @event)
	{
		var mousePos = GetViewport().GetMousePosition();
		var tex = _foxSprite.SpriteFrames.GetFrameTexture(_foxSprite.Animation, _foxSprite.Frame);
		var image = tex.GetImage();
		var pixel = image.GetPixel(Math.Clamp((int)mousePos.X, 0, image.GetWidth() - 1), Math.Clamp((int)mousePos.Y, 0, image.GetHeight() - 1));
		bool isOpaque = pixel.A > 0.5f;

		if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
		{
			if (isOpaque)
			{
				GD.Print("Clicked on opaque pixel");
			}

		}
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
