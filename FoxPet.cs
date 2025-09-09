using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Godot;
using System.Threading.Tasks;

public partial class FoxPet : Node2D
{
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

    private enum FoxState { Moving, Idle, Sleeping, Mad, BeingDragged, AttackSlime }
    private FoxState _state = FoxState.Moving;

    [Signal] public delegate void SlimeAttackedEventHandler();
    [Signal] public delegate void StopSlimeMovementEventHandler();

    private bool _isFalling;
    private float _stateTimer = 0f;
    private float _idleDuration = 0f;
    private float _sleepDuration = 5f;
    private float _madDuration = 3f;
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

    private Image _currentFrameImage;
    private Node2D _targetSlime;

    // UI and Slimes
    Ui UiScript;
    SlimeContainer slimeContainer;

    public override void _Ready()
    {
        clickThrough = GetNode<ClickThrough>("/root/Window/ClickThrough");
        UiScript = GetNode<Ui>("/root/Window/Ui_Root");
        slimeContainer = GetNode<SlimeContainer>("/root/Window/Slimes");

        UpdateWorkArea();
        var tex = _sprite.SpriteFrames.GetFrameTexture(_sprite.Animation, _sprite.Frame);
        _spriteSize = tex.GetSize();
        _targetX = GetNewTargetX();
        _sprite.Play("Idle");

        var body = FindChild("ClickLogic") as Area2D;
        if (body != null)
            body.Connect("input_event", new Callable(this, nameof(OnInputEvent)));

        _sprite.AnimationFinished += OnAnimationFinished;
        slimeContainer.SlimeRemoved += OnSlimeRemoved;

        // Connect existing slimes
        ConnectAllSlimes();

        // Listen for newly spawned slimes under the container
        slimeContainer.ChildEnteredTree += OnSlimeSpawned;
    }

    private void ConnectAllSlimes()
    {
        if (slimeContainer == null) return;
        foreach (var slime in slimeContainer.GetSlimes())
        {
            if (slime is SlimeManager slimeManager)
                slimeManager.SlimeInRange += OnSlimeInRange;
        }
    }

    private void OnSlimeSpawned(Node node)
    {
        if (node is SlimeManager slimeManager)
            slimeManager.SlimeInRange += OnSlimeInRange;
        GD.Print("Connected to new slime's SlimeInRange signal.");
        // Optionally, immediately check if the new slime is in range
        OnSlimeInRange();
    }

    private void OnSlimeRemoved(Node2D slime)
    {
        if (slime is SlimeManager slimeManager)
        {
            slimeManager.SlimeInRange -= OnSlimeInRange;
            if (_targetSlime == slime)
                _targetSlime = null;
        }
    }

    // Called when a slime's foxDetection Area2D emits SlimeInRange
    private void OnSlimeInRange()
    {
        if (slimeContainer == null) return;

        // Find the slime which has a valid detection area or first valid slime.
        foreach (var slime in slimeContainer.GetSlimes())
        {
            //break out once the first valid slime is found
            if (slime is SlimeManager slimeManager && slimeManager.foxDetectionArea != null)
            {
                _targetSlime = slime;
                _state = FoxState.AttackSlime;
                GD.Print("Fox: Slime in range, attacking!");
                break;
            }
            return;
        }
    }

    public override void _Process(double delta)
    {
        UpdateWorkArea();
        _ = HandleFoxBehavior((float)delta);

        Vector2 mousePos = GetViewport().GetMousePosition();
        var frameTexture = _sprite.SpriteFrames.GetFrameTexture(_sprite.Animation, _sprite.Frame);
        if (_currentFrameImage == null || _currentFrameImage.GetSize() != frameTexture.GetSize())
            _currentFrameImage = frameTexture.GetImage();

        bool hoveringSprite = IsMouseOverOpaquePixel(frameTexture, _currentFrameImage, mousePos);
        bool hoveringUI = IsMouseOverUI(UiScript);

        clickThrough.SetClickThrough(!(hoveringSprite || hoveringUI || _uiActive));

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
                    TransitionTo(_rng.NextDouble() < 0.3);
                break;
            case FoxState.Sleeping:
                _stateTimer += delta;
                if (_stateTimer >= _sleepDuration)
                    TransitionTo(false);
                break;
            case FoxState.Mad:
                _stateTimer += delta;
                if (_stateTimer >= _madDuration)
                    TransitionTo(false);
                break;
            case FoxState.BeingDragged:
                UpdateFoxLocation(delta);
                BeginDrag();
                break;
            case FoxState.AttackSlime:
                if (_targetSlime != null)
                    await AttackSlime(_targetSlime, delta);
                else
                    _state = FoxState.Moving;
                break;
        }
    }

    private void MoveTowardTarget(float delta)
    {
        // If target slime exists let AttackSlime handle movement/approach
        if (_targetSlime != null)
        {
            // do nothing here; AttackSlime will run in HandleFoxBehavior
            return;
        }

        Vector2 pos = GlobalPosition;
        float direction = _targetX > pos.X ? 1f : -1f;
        pos.X += direction * Speed * delta;
        pos.X = Mathf.Clamp(pos.X, _workArea.Left, _workArea.Right - _spriteSize.X);

        GlobalPosition = new Vector2(pos.X, pos.Y);
        _sprite.FlipH = direction < 0;
        _sprite.Play("Running");
        UpdateFoxLocation(delta);
        if (!_isFalling && Mathf.Abs(_targetX - pos.X) < 5f)
            BeginIdle();
    }

    // AttackSlime now queries the slime's GLOBAL position so offsets inside the manager are handled
    private async Task AttackSlime(Node2D slime, float delta)
    {
        if (slime == null || !IsInstanceValid(slime))
        {
            _state = FoxState.Moving;
            _targetSlime = null;
            return;
        }

        Vector2 foxPos = GlobalPosition;
        float groundY = _workArea.Bottom - _spriteSize.Y - BottomOffset;
        foxPos.Y = groundY;

        Vector2 slimePos = GetSlimeGlobalPosition(slime);

        // If slimePos is zero, treat as invalid
        if (slimePos == Vector2.Zero)
        {
            _state = FoxState.Moving;
            _targetSlime = null;
            return;
        }

        // If close enough on X, play attack
        if (Mathf.Abs(slimePos.X - foxPos.X) < 40f)
        {
            _sprite.FlipH = foxPos.X > slimePos.X;
            _state = FoxState.AttackSlime;
            _sprite.Play("Attack");
            EmitSignal(nameof(StopSlimeMovement));
            var tcs = new TaskCompletionSource();
            void Handler()
            {
                _sprite.AnimationFinished -= Handler;
                tcs.SetResult();
            }
            _sprite.AnimationFinished += Handler;
            await tcs.Task;
            OnAnimationFinished();
            EmitSignal(nameof(SlimeAttacked));
            // After attack clear target so we don't re-attack instantaneously
            _targetSlime = null;
        }
        else
        {
            // Move toward the slime using global coordinates
            foxPos.X = Mathf.MoveToward(foxPos.X, slimePos.X, _huntingSpeed * delta);
            foxPos.X = Mathf.Clamp(foxPos.X, _workArea.Left, _workArea.Right - _spriteSize.X);
            GlobalPosition = foxPos;
            _sprite.Play("Running");
            _sprite.FlipH = foxPos.X > slimePos.X;
        }

        _stateTimer = 0f;
    }

    // Helper: try to resolve the actual global position of the visible slime
    private Vector2 GetSlimeGlobalPosition(Node2D slimeNode)
    {
        if (slimeNode == null || !IsInstanceValid(slimeNode)) return Vector2.Zero;

        // If this is a SlimeManager, try common exported children that contain the sprite
        if (slimeNode is SlimeManager sm)
        {
            // Prefer slimeScript (likely the Slime node) if available
            if (sm.slimeScript != null && IsInstanceValid(sm.slimeScript))
                return sm.slimeScript.GlobalPosition;

            // Fall back to exported AnimatedSprite2D
            if (sm.slimePrefab != null && IsInstanceValid(sm.slimePrefab))
                return sm.slimePrefab.GlobalPosition;

            // Fallback to Area2D center if present
            if (sm.foxDetectionArea != null && IsInstanceValid(sm.foxDetectionArea))
                return sm.foxDetectionArea.GlobalPosition;
        }

        // Fallback: the node's global position
        return slimeNode.GlobalPosition;
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
        Vector2 pos = GlobalPosition;
        float groundY = _workArea.Bottom - _spriteSize.Y - BottomOffset;

        if (pos.Y < groundY || _isFalling)
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

        GlobalPosition = new Vector2(pos.X, pos.Y);
    }

    public void UpdateWorkArea()
    {
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

        SystemParametersInfo(SPI_GETWORKAREA, 0, out _workArea, 0);
    }

    private const uint SPI_GETWORKAREA = 0x0030;
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }
    private IntPtr _previousWindowHandle = IntPtr.Zero;
    private IntPtr GetFoxWindowHandle() => Process.GetCurrentProcess().MainWindowHandle;
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out RECT pvParam, uint fWinIni);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);

    private void OnInputEvent(Node viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.Pressed)
            {
                _clickStartPosition = mouseEvent.GlobalPosition;
                _isClickCandidate = true;
            }
            else
            {
                float distance = (mouseEvent.GlobalPosition - _clickStartPosition).Length();

                if (_isClickCandidate && distance < DragThreshold)
                {
                    if (mouseEvent.DoubleClick)
                        GD.Print("Double Clicked Fox");
                    else
                        GD.Print("Single Clicked Fox");

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
                _isDragging = true;
                _dragOffset = mouseMotion.GlobalPosition - GlobalPosition;
                _isClickCandidate = false;
            }
        }
    }

    private bool IsMouseOverOpaquePixel(Texture2D texture, Image image, Vector2 mousePos, float alphaThreshold = 0.5f)
    {
        if (_uiActive) return false;
        Vector2 localPos = _sprite.ToLocal(mousePos);
        Vector2 scaledPos = localPos / _sprite.Scale;
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

    bool IsMouseOverUI(CanvasGroup root)
    {
        if (root == null || !root.Visible)
            return false;

        Vector2 mousePos = GetViewport().GetMousePosition();
        if (root.GetChildCount() > 0 && root.GetChild(0) is Control controlRoot)
            return IsMouseOverAnyControlRecursive(controlRoot, mousePos);
        return false;
    }

    bool IsMouseOverAnyControlRecursive(Control node, Vector2 mousePos)
    {
        if (node.Visible && node.MouseFilter != Control.MouseFilterEnum.Ignore && node.GetGlobalRect().HasPoint(mousePos))
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
}
