using Godot;
using System;

public partial class ColourButton : ColorPickerButton
{

    [Signal]
    public delegate void KeepClickThroughEventHandler();
    [Signal]
    public delegate void StopClickThroughEventHandler();
    private bool _isDragging = false;
    private Vector2 _dragOffset;

    public override void _Ready()
    {
        base._Ready();
        Pressed += _on_color_picker_button_pressed;
        PopupClosed += _on_color_picker_button_popup_closed;
    }



    private void _on_color_picker_button_pressed()
    {
        GD.Print("Color Picker Button Pressed");
        // Emit the signal to notify that the color picker button has been pressed
        EmitSignal(SignalName.KeepClickThrough);
    }

    private void _on_color_picker_button_popup_closed()
    {
        GD.Print("Color Picker Button Popup Closed");
        // Emit the signal to notify that the color picker button popup has been closed
        EmitSignal(SignalName.StopClickThrough);
    }
}
