using Godot;
using System;

public partial class Ui : Control
{
	[Export] ClickThrough clickThrough;
	[Export] Button spawnMoreButton;
	[Export] private ColourButton colorPickerButton;

	[Signal] public delegate void UiActiveEventHandler();

	private bool _isDragging = false;
	private Vector2 _dragOffset;

	public override void _Ready()
	{
		clickThrough.SetClickThrough(false);
		colorPickerButton.KeepClickThrough += keepUiClickActive;   // Connect button signals
		spawnMoreButton.Connect("pressed", new Callable(this, nameof(_on_button_pressed)));
		spawnMoreButton.Connect("mouse_entered", new Callable(this, nameof(_on_button_mouse_entered)));
		spawnMoreButton.Connect("mouse_exited", new Callable(this, nameof(_on_button_mouse_exited)));
		// Listen for mouse input events (needed for drag start/stop)
		spawnMoreButton.GuiInput += OnButtonGuiInput;

	}

	public override void _Process(double delta)
	{
		if (_isDragging)
		{
			Vector2 mousePos = GetViewport().GetMousePosition();
			Position = mousePos - _dragOffset;
		}
	}

	private void OnButtonGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			if (mouseEvent.Pressed)
			{
				_isDragging = true;
				_dragOffset = mouseEvent.Position;
			}
			else
			{
				_isDragging = false;
			}
		}
	}

	public void _on_button_pressed()
	{
		GD.Print("Spawn More Button Pressed");
	}

	public void _on_button_mouse_entered()
	{
		Scale = new Vector2(1.1f, 1.1f);
	}

	public void _on_button_mouse_exited()
	{
		Scale = new Vector2(1.0f, 1.0f);
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
}
