using Godot;
using System;

public partial class ColourButton : ColorPickerButton
{

    [Signal]
    public delegate void KeepClickThroughEventHandler();

    public override void _Ready()
    {
        base._Ready();
        // Connect the signal to notify when a color picker is created
        Connect("picker_created", new Callable(this, nameof(_on_color_picker_button_picker_created)));
    }
    
    private void _on_color_picker_button_picker_created()
    {
        GD.Print("Color Picker Button Created");
        // Emit the signal to notify that a color picker button has been created
        EmitSignal(SignalName.KeepClickThrough);
    }
}
