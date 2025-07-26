using Godot;
using System;

public partial class SlimeManager : Node2D
{
    [Export]
    ClickThrough clickThrough;

    // get set for children 

    public ClickThrough ClickThrough
    {
        get => clickThrough;
        set => clickThrough = value;
    }

    public override void _Ready()
    {
        // Ensure ClickThrough is initialized
        if (clickThrough == null)
        {
            clickThrough = GetNode<ClickThrough>("/root/Base/ClickThrough");
        }
        
        // Additional initialization code can go here
    }
}
