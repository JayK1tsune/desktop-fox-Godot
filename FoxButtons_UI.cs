using Godot;
using System;

public partial class FoxButtons_UI : Button
{

    [Signal]
    public delegate void CloseUiEventHandler();
    private bool _isDragging = false;
    private Vector2 _dragOffset;


    public override void _Ready()
    {
        base._Ready();
        GuiInput += OnButtonGuiInput;
        Connect("pressed", new Callable(this, nameof(_on_button_pressed)));
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
		EmitSignal(SignalName.CloseUi);
	}
}
