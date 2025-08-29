using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Godot;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

public partial class FoxPet : Node2D
{
    // ────── Configurable ──────
    [Export] public float Speed = 30f;
    [Export] public float _huntingSpeed = 50f;
    [Export] public float TopOffset = 50f;
    [Export] public float BottomOffset = 15f;
    [Export] private float sideOffset = 0f;
    [Export] float _fallVelocity = 0f;
    [Export] float _gravity = 2000f;
    [Export] float _maxFallSpeed = 1500f;
    [Export] private AnimatedSprite2D _sprite;

    ClickThrough clickThrough;

    // ────── Internal ──────
    private enum FoxState { Moving, Idle, Sleeping, Mad, BeingDragged, AttackSlime }
    private FoxState _state = FoxState.Moving;


    [Signal] public delegate void SlimeAttackedEventHandler();
    [Signal] public delegate void StopSlimeMovementEventHandler();
    private bool _isFalling;

    private float _stateTimer = 0f;
    private float _idleDuration = 0f;
    private float _sleepDuration = 5f;
    private float _madDuration = 3f;
    private Vector2 _mousePosition;
    private Vector2 _spriteSize;
    
    private float _targetX;
    private Random _rng = new Random();
    public RECT _workArea;
    private bool _isDragging = false;
    private bool _isClickCandidate = false;
    private Vector2 _clickStartPosition;
    private const float DragThreshold = 4f;
    private Vector2 _dragOffset;
    public bool _uiActive = false;

    private float _clickTimer = 0f;
    private bool _waitingForClickRelease = false;
    private bool _slimeInRange = false;


    // Ui interaction

    Ui UiScript;
    SlimeManager _slimeManager;







    public override void _Ready()
    {
        clickThrough = GetNode<ClickThrough>("/root/Window/ClickThrough");
        UiScript = GetNode<Ui>("/root/Window/Ui_Root");
        _slimeManager = GetTree().Root.GetNode<SlimeManager>("/root/Window/Slimes/Slime");


        UpdateWorkArea();
        var tex = _sprite.SpriteFrames.GetFrameTexture(_sprite.Animation, _sprite.Frame);
        _spriteSize = tex.GetSize();
        _targetX = GetNewTargetX();
        _sprite.Play("Idle");
        var body = FindChild("ClickLogic") as Area2D;
        body.Connect("input_event", new Callable(this, nameof(OnInputEvent)));
        _slimeManager.SlimeInRange += OnSlimeInRange;
        _sprite.AnimationFinished += OnAnimationFinished;
    }

    private Image _currentFrameImage;

    public override void _Process(double delta)
    {
        UpdateWorkArea();
        _ = HandleFoxBehavior((float)delta);

        Vector2 mousePos = GetViewport().GetMousePosition();

        // Cache the current frame image once per frame
        var frameTexture = _sprite.SpriteFrames.GetFrameTexture(_sprite.Animation, _sprite.Frame);
        if (_currentFrameImage == null || _currentFrameImage.GetSize() != frameTexture.GetSize())
            _currentFrameImage = frameTexture.GetImage();

        bool hoveringSprite = IsMouseOverOpaquePixel(frameTexture, _currentFrameImage, mousePos);
        bool hoveringUI = IsMouseOverUI(UiScript);

        clickThrough.SetClickThrough(!(hoveringSprite || hoveringUI || _uiActive));

        // Handle dragging
        if (_isDragging)
        {
            GlobalPosition = mousePos - _dragOffset;
            _state = FoxState.BeingDragged;
            _sprite.Play("GettingDragged");
        }

    }


    private async Task HandleFoxBehavior(float delta)
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
            case FoxState.AttackSlime:
                if (_slimeManager.foxDetectionArea != null)
                {
                    _slimeManager.foxDetectionArea.QueueFree();
                    _slimeManager.foxDetectionArea = null;
                }    
                await AttackSlime(delta);
                break;
            default:
                GD.PrintErr("Unknown FoxState: " + _state);
                break;
        }
    }

    private async void MoveTowardTarget(float delta)
    {
        if (_slimeInRange)
        {
            await AttackSlime(delta);
            return;
        }
        Vector2 pos = Position;
        float direction = _targetX > pos.X ? 1f : -1f;
        pos.X += direction * Speed * delta;
        pos.X = Mathf.Clamp(pos.X, _workArea.Left, _workArea.Right - _spriteSize.X);

        Position = new Vector2(pos.X, Position.Y);
        _sprite.FlipH = direction < 0;
        _sprite.Play("Running");
        UpdateFoxLocation(delta);
        if (!_isFalling && Mathf.Abs(_targetX - pos.X) < 5f)
            BeginIdle();

    }

    private async Task AttackSlime(float delta)
    {
        if (_slimeManager.slimePrefab == null || !IsInstanceValid(_slimeManager.slimePrefab))
        {
            _state = FoxState.Moving;
               return;
        }

        Vector2 pos = Position;
        float groundY = _workArea.Bottom - _spriteSize.Y - BottomOffset;
        pos.Y = groundY;
        // Check if the fox is close enough to the slime
        if (_slimeInRange)
        {
            Vector2 slimePosition = _slimeManager.slimePrefab.Position;
            Position = pos;
            if (Mathf.Abs(slimePosition.X - pos.X) < 40f)
            {
                //face the correct direction
                _sprite.FlipH = pos.X > slimePosition.X;
                _state = FoxState.AttackSlime;
                _sprite.Play("Attack");
                _huntingSpeed = 50f; //reset hunting speed
                EmitSignal(nameof(StopSlimeMovement));
                var tcs = new TaskCompletionSource();
                void Handler()
                {
                    _sprite.AnimationFinished -= Handler;
                    tcs.SetResult();
                }
                _sprite.AnimationFinished += Handler;
                await tcs.Task; // Wait for the attack animation to finish
                OnAnimationFinished();
            }
            else
            {
                GD.Print("Fox is moving closer to the slime to attack.");
                pos.X = Mathf.MoveToward(pos.X, slimePosition.X, _huntingSpeed * delta);

                Position = pos;
                _sprite.FlipH = pos.X > slimePosition.X;
            }
        }
        else
        {
            _state = FoxState.Moving;
            _slimeInRange = false;
            _sprite.Play("Running");
        }
        Position = new Vector2(pos.X, pos.Y);
        _slimeInRange = false; // Reset slime in range after attack
        _stateTimer = 0f; // Reset state timer after attack
    }

    private void OnAnimationFinished()
    {
        EmitSignal(nameof(SlimeAttacked));
    }


    private void BeginIdle()
    {
        _idleDuration = (float)(_rng.NextDouble() * 3 + 2);
        _stateTimer = 0f;
        _state = FoxState.Idle;
        _sprite.Play("Idle");
    }

    private void BeginDrag()
    {
        _sprite.Play("GettingDragged");
    }

    private void TransitionTo(bool randomSleep)
    {
        _targetX = GetNewTargetX();
        _state = randomSleep ? FoxState.Sleeping : FoxState.Moving;
        _stateTimer = 0f;

        if (_state == FoxState.Sleeping)
            _sprite.Play("Sleeping");
        else if (_state == FoxState.Moving)
            _sprite.Play("Running");
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

        // Adjust ground Y so sprite bottom sits on taskbar
        float groundY = _workArea.Bottom - _spriteSize.Y - BottomOffset;
        

        // Falling check
        if (Position.Y < groundY || _isFalling)
        {
            _isFalling = true;
            _fallVelocity = Mathf.Min(_fallVelocity + _gravity * delta, _maxFallSpeed);
            pos.Y += _fallVelocity * delta;
            _state = FoxState.BeingDragged;

            // Land
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
            pos.Y = groundY; // Snap to ground
        }

        Position = new Vector2(Position.X, pos.Y);
    }




    public void UpdateWorkArea()
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
    //need to check to see where i can  reduce this bloat
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
                    _sprite.Play("Mad");

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


    private bool IsMouseOverOpaquePixel(Texture2D texture, Image image, Vector2 mousePos, float alphaThreshold = 0.5f)
    {
        if (_uiActive) return false;

        // Local position relative to sprite
        Vector2 localPos = _sprite.ToLocal(mousePos);
        Vector2 scaledPos = localPos / _sprite.Scale;

        // Adjust for pivot
        Vector2 texSize = texture.GetSize();
        Vector2 pivotOffset = _sprite.Centered ? texSize / 2f : Vector2.Zero;
        Vector2 texPos = scaledPos + pivotOffset;

        int x = Mathf.Clamp((int)texPos.X, 0, (int)texSize.X - 1);
        int y = Mathf.Clamp((int)texPos.Y, 0, (int)texSize.Y - 1);

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

    private void OnSlimeInRange()
    {
        GD.Print("Slime in range detected by FoxPet");
        _slimeInRange = true;
    }





}
