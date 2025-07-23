using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Godot;
using System.ComponentModel.DataAnnotations;

public partial class FoxPet : AnimatedSprite2D
{
    // ────── Configurable ──────
    [Export] public float Speed = 30f;
    [Export] public float TopOffset = 50f;
    [Export] public float BottomOffset = 50f;
    [Export] ClickThrough clickThrough;
    [Export] private Control UiNode;

    // ────── Internal ──────
    private enum FoxState { Moving, Idle, Sleeping, Mad }
    private FoxState _state = FoxState.Moving;

    private float _stateTimer = 0f;
    private float _idleDuration = 0f;
    private float _sleepDuration = 5f;
    private float _madDuration = 3f;
    private Vector2 _mousePosition;
    private Vector2 _spriteSize;
    private AnimatedSprite2D _sprite;
    private float _targetX;
    private Random _rng = new Random();
    private RECT _workArea;
    private bool _isDragging = false;
    private Vector2 _dragOffset;
    private Vector2 _uiSize;
    

    public override void _Ready()
    {
        // Cache sprite size once
        var tex = SpriteFrames.GetFrameTexture(GetAnimation(), GetFrame());
        _spriteSize = tex.GetSize();
        _uiSize = UiNode.Size;
        clickThrough.SetClickThrough(false);
        _targetX = GetNewTargetX();
        Play("Idle");
        _sprite = this;
        var body = GetNode<Area2D>("ClickLogic");
        body.InputPickable = true;
        body.Connect("input_event", new Callable(this, nameof(OnInputEvent)));
        UiNode.Connect("SpawnMorePressed", new Callable(this, nameof(OnButtonPress)));
    }

    public override void _Process(double delta)
    {
        IntPtr currentForeground = GetForegroundWindow();
        IntPtr foxWindow = GetFoxWindowHandle();

        bool hoveringUI = IsMouseOverUI(UiNode);
        clickThrough.SetClickThrough(!hoveringUI);

        if (currentForeground != IntPtr.Zero && currentForeground != foxWindow)
        {
            _previousWindowHandle = currentForeground;
        }

        UpdateWorkArea();
        HandleFoxBehavior((float)delta);

        _mousePosition = GetViewport().GetMousePosition();

        if (_isDragging)
        {
            GD.Print("Dragging.");
        }

        // Check if mouse is over opaque pixel of the fox sprite
        bool hoveringOpaque = IsMouseOverOpaquePixel(
            SpriteFrames.GetFrameTexture(GetAnimation(), GetFrame()), 
            _mousePosition, 
            Position, 
            _spriteSize, 
            0.5f);

        // Toggle click-through purely on pixel opacity under mouse
        clickThrough.SetClickThrough(!hoveringOpaque);
    }

    private void HandleFoxBehavior(float delta)
    {
        switch (_state)
        {
            case FoxState.Moving:
                MoveTowardTarget(delta);
                break;

            case FoxState.Idle:
                _stateTimer += delta;
                if (_stateTimer >= _idleDuration)
                    TransitionTo(randomSleep: _rng.NextDouble() < 0.3);
                break;

            case FoxState.Sleeping:
                _stateTimer += delta;
                if (_stateTimer >= _sleepDuration)
                    TransitionTo(randomSleep: false);
                break;

            case FoxState.Mad:
                _stateTimer += delta;
                if (_stateTimer >= _madDuration)
                    TransitionTo(randomSleep: false);
                break;
        }
    }

    private void MoveTowardTarget(float delta)
    {
        Vector2 pos = Position;
        float direction = _targetX > pos.X ? 1f : -1f;
        pos.X += direction * Speed * delta;

        pos.X = Mathf.Clamp(pos.X, _workArea.Left, _workArea.Right - _spriteSize.X);
        //Removed the top of window logic for now
        // pos.Y = _workArea.Top + TopOffset; 
        //below will snap it to the bottom
        pos.Y = _workArea.Bottom - _spriteSize.Y - BottomOffset;
        Position = pos;
        FlipH = direction < 0;
        Play("Running");

        if (Mathf.Abs(_targetX - pos.X) < 5f)
            BeginIdle();
    }

    private void BeginIdle()
    {
        _idleDuration = (float)(_rng.NextDouble() * 3 + 2);
        _stateTimer = 0f;
        _state = FoxState.Idle;
        Play("Idle");
    }

    private void TransitionTo(bool randomSleep)
    {
        _targetX = GetNewTargetX();
        _state = randomSleep ? FoxState.Sleeping : FoxState.Moving;
        _stateTimer = 0f;

        if (_state == FoxState.Sleeping)
            Play("Sleeping");
        else if (_state == FoxState.Moving)
            Play("Running");
    }

    private float GetNewTargetX()
    {
        float left = (float)_workArea.Left;
        float right = (float)(_workArea.Right - _spriteSize.X);

        if (right <= left)
            return (_workArea.Left + _workArea.Right) / 2f;

        float randomX = (float)_rng.NextDouble() * (right - left) + left;
        return Mathf.Clamp(randomX, left, right);
    }

    private void UpdateWorkArea()
    {
        // Only update work area to the foreground window if dragging
        if (_isDragging)
        {
            RECT rect;
            IntPtr activeWindow = GetForegroundWindow();
            if (activeWindow != IntPtr.Zero && GetWindowRect(activeWindow, out rect))
            {
                _workArea = rect;
                return;
            }
        }

        // Default fallback: taskbar/desktop
        SystemParametersInfo(SPI_GETWORKAREA, 0, out _workArea, 0);
    }


    // ────── Windows API ──────
    private const uint SPI_GETWORKAREA = 0x0030;

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    private IntPtr _previousWindowHandle = IntPtr.Zero;

    private IntPtr GetFoxWindowHandle()
    {
        return Process.GetCurrentProcess().MainWindowHandle;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out RECT pvParam, uint fWinIni);

    // ────── User Input ──────

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    private const  float CLICK_THRESHHOLD = 32.0f;
    private void OnInputEvent(Node viewport, InputEvent @event, int shapeIdx)
    {


        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && (mouseEvent.GlobalPosition - Position).Length() < CLICK_THRESHHOLD)
            {
                _isDragging = true;
            }
            else if (_isDragging && mouseEvent.IsReleased())
            {
                _isDragging = false;
            }
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    GD.Print("Left mouse button clicked on the fox!");
                    _state = FoxState.Mad;
                    _stateTimer = 0f;
                    Play("Mad");
                    _isDragging = false;

                    if (_previousWindowHandle != IntPtr.Zero)
                {
                    SetForegroundWindow(_previousWindowHandle);
                    GD.Print("Restored focus to previous window.");
                }
                }
        }
    }

    private void OnButtonPress() {
        GD.Print("Button Pressed");
    }

    bool IsMouseOverUI(Control control)
    {
        Vector2 localPos = control.GetLocalMousePosition();
        return control.GetRect().HasPoint(localPos);
    }


    bool IsMouseOverOpaquePixel(Texture2D texture, Vector2 mousePos, Vector2 spritePos, Vector2 spriteSize, float alphaThreshold = 0.5f)
    {
        // Convert global mouse position to local coordinates relative to the sprite center
        Vector2 localPos = ToLocal(mousePos);

        // Offset localPos to sprite texture space (top-left origin)
        int x = Mathf.Clamp((int)(localPos.X + spriteSize.X / 2f), 0, (int)spriteSize.X - 1);
        int y = Mathf.Clamp((int)(localPos.Y + spriteSize.Y / 2f), 0, (int)spriteSize.Y - 1);

        Image image = texture.GetImage();

        if (image == null)
            return false;

        Color pixelColor = image.GetPixel(x, y);
        return pixelColor.A >= alphaThreshold;
    }
}
