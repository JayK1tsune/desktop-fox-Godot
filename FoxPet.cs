using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Godot;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

public partial class FoxPet : AnimatedSprite2D
{
    // ────── Configurable ──────
    [Export] public float Speed = 30f;
    [Export] public float TopOffset = 50f;
    [Export] public float BottomOffset = 15f;
    [Export] private float sideOffset = 0f;
    [Export] float _fallVelocity = 0f;
    [Export] float _gravity = 2000f;
    [Export] float _maxFallSpeed = 1500f;

    [Export] private ClickThrough clickThrough;

    // ────── Internal ──────
    private enum FoxState { Moving, Idle, Sleeping, Mad, BeingDragged }
    private FoxState _state = FoxState.Moving;

    private bool _isFalling;

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
    private bool _isClickCandidate = false;
    private Vector2 _clickStartPosition;
    private const float DragThreshold = 4f;
    private Vector2 _dragOffset;
    private bool _uiActive = false;

    private float _clickTimer = 0f;
    private bool _waitingForClickRelease = false;

    // Ui interaction
    [Export] private Control _ui;
    [Export] private Ui UiScript;







    public override void _Ready()
    {
        UiScript.UiActive += _on_keep_clickthrough;
        // Connect the signal to keep UI click active
        var tex = SpriteFrames.GetFrameTexture(GetAnimation(), GetFrame());
        _spriteSize = tex.GetSize();
        GD.PrintErr(_spriteSize);
        clickThrough.SetClickThrough(false);
        _targetX = GetNewTargetX();
        Play("Idle");
        _sprite = this;
        var body = GetNode<Area2D>("ClickLogic");
        body.InputPickable = true;
        body.Connect("input_event", new Callable(this, nameof(OnInputEvent)));

    }

    public override void _Process(double delta)
    {
        IntPtr currentForeground = GetForegroundWindow();
        IntPtr foxWindow = GetFoxWindowHandle();

        if (currentForeground != IntPtr.Zero && currentForeground != foxWindow)
        {
            _previousWindowHandle = currentForeground;
        }

        UpdateWorkArea();
        HandleFoxBehavior((float)delta);

        _mousePosition = GetViewport().GetMousePosition();




        // Check if mouse is over opaque pixel of the fox sprite
        bool hoveringSprite = IsMouseOverOpaquePixelOnly(
            SpriteFrames.GetFrameTexture(GetAnimation(), GetFrame()),
            _mousePosition,
            Position,
            _spriteSize,
            0.5f);

        bool hoveringUI = IsMouseOverUI(_ui);
        bool hoveringOpaque = hoveringSprite || hoveringUI;

        // Toggle click-through purely on pixel opacity under mouse
        clickThrough.SetClickThrough(!hoveringOpaque);

        if (_waitingForClickRelease)
            _clickTimer += (float)delta;
        if (_isDragging)
        {
            Vector2 mousePos = GetGlobalMousePosition();
            GlobalPosition = mousePos - _dragOffset;
            GD.Print("Dragging.");
            _state = FoxState.BeingDragged;
        }

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
            case FoxState.BeingDragged:
                UpdateFoxLocation(delta);
                BeginDrag();
                break;
        }
    }

    private void MoveTowardTarget(float delta)
    {
        Vector2 pos = Position;
        float direction = _targetX > pos.X ? 1f : -1f;
        pos.X += direction * Speed * delta;
        pos.X = Mathf.Clamp(pos.X, _workArea.Left, _workArea.Right - _spriteSize.X);

        Position = new Vector2(pos.X, Position.Y);
        FlipH = direction < 0;
        Play("Running");
        UpdateFoxLocation(delta);
        if (!_isFalling && Mathf.Abs(_targetX - pos.X) < 5f)
            BeginIdle();

    }

    private void BeginIdle()
    {
        _idleDuration = (float)(_rng.NextDouble() * 3 + 2);
        _stateTimer = 0f;
        _state = FoxState.Idle;
        Play("Idle");
    }

    private void BeginDrag()
    {
        Play("GettingDragged");

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

    private void UpdateFoxLocation(float delta)
    {
        Vector2 pos = Position;
        float groundY = _workArea.Bottom - _spriteSize.Y - BottomOffset;

        // Check if fox is off the taskbar
        if (Position.Y < groundY - 5f || _isFalling)
        {
            _isFalling = true;
            _fallVelocity = Mathf.Min(_fallVelocity + _gravity * delta, _maxFallSpeed);
            pos.Y += _fallVelocity * delta;
            _state = FoxState.BeingDragged;

            if (pos.Y >= groundY)
            {
                pos.Y = groundY;
                _fallVelocity = 0f;
                _isFalling = false;
                BeginIdle();
            }
        }
        else
        {
            pos.Y = groundY;
        }

        Position = new Vector2(Position.X, pos.Y);
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

    private void OnInputEvent(Node viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.Pressed)
            {
                _clickStartPosition = mouseEvent.GlobalPosition;
                _isClickCandidate = true;
            }
            else // Released
            {
                float distance = (mouseEvent.GlobalPosition - _clickStartPosition).Length();

                if (_isClickCandidate && distance < DragThreshold)
                {
                    // Treat this as a click or double-click
                    if (mouseEvent.DoubleClick)
                    {
                        GD.Print("Double Clicked Fox");
                    }
                    else
                    {
                        GD.Print("Single Clicked Fox");
                    }

                    _state = FoxState.Mad;
                    _stateTimer = 0f;
                    Play("Mad");

                    if (_previousWindowHandle != IntPtr.Zero)
                    {
                        SetForegroundWindow(_previousWindowHandle);
                        GD.Print("Restored Window.");
                    }
                }

                _isDragging = false;
                _isClickCandidate = false;
            }
        }

        if (@event is InputEventMouseMotion mouseMotion && _isClickCandidate)
        {
            float moveDistance = (mouseMotion.GlobalPosition - _clickStartPosition).Length();
            if (moveDistance >= DragThreshold)
            {
                // Start dragging
                _isDragging = true;
                _dragOffset = mouseMotion.GlobalPosition - GlobalPosition;
                _isClickCandidate = false;
            }
        }
    }





    bool IsMouseOverOpaquePixelOnly(Texture2D texture, Vector2 mousePos, Vector2 spritePos, Vector2 spriteSize, float alphaThreshold = 0.5f)
    {
        if (_uiActive)  // Check if click-through is enabled
        {
            return false; // Ignore if click-through is enabled
        }
        Vector2 localPos = ToLocal(mousePos);

        int x = Mathf.Clamp((int)(localPos.X + spriteSize.X / 2), 0, (int)spriteSize.X - 1);
        int y = Mathf.Clamp((int)(localPos.Y + spriteSize.Y / 2), 0, (int)spriteSize.Y - 1);

        Image image = texture.GetImage();

        if (image != null)
        {
            Color pixelColor = image.GetPixel(x, y);
            return pixelColor.A >= alphaThreshold;
        }

        return false;
    }

    bool IsMouseOverUI(Control root)
    {
        if (root == null || !root.Visible)
            return false;

        Vector2 mousePos = GetViewport().GetMousePosition();
        return IsMouseOverAnyControlRecursive(root, mousePos);
    }

    bool IsMouseOverAnyControlRecursive(Control node, Vector2 mousePos)
    {
        if (node.Visible
            && node.MouseFilter != Control.MouseFilterEnum.Ignore
            && node.GetGlobalRect().HasPoint(mousePos))
        {
            GD.Print("Mouse is over UI element: " + node.Name);
            return true;
        }

        foreach (Node child in node.GetChildren())
        {
            if (child is Control control)
            {
                if (IsMouseOverAnyControlRecursive(control, mousePos))
                    return true;
            }
        }

        return false;
    }

    private void _on_keep_clickthrough()
    {
        _uiActive = true;
        clickThrough.SetClickThrough(false);
        GD.Print("Click-through state set to: false");
    }





}
