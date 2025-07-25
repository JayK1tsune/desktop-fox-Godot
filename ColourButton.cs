using Godot;
using System;

public partial class ColourButton : ColorPickerButton
{

    [Signal]
    public delegate void KeepClickThroughEventHandler();
    private bool _isDragging = false;
    private Vector2 _dragOffset;

    public override void _Ready()
    {
        base._Ready();
        // Connect the signal to notify when a color picker is created
        Connect("picker_created", new Callable(this, nameof(_on_color_picker_button_picker_created)));
        GuiInput += OnButtonGuiInput;
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
    
    private void _on_color_picker_button_picker_created()
    {
        GD.Print("Color Picker Button Created");
        // Emit the signal to notify that a color picker button has been created
        EmitSignal(SignalName.KeepClickThrough);
    }
}
