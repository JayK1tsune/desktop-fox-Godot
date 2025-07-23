using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Godot;

public partial class FoxPet : AnimatedSprite2D
{
    // ────── Configurable ──────
    [Export] public float Speed = 30f;
    [Export] public float TopOffset = 50f;
    [Export] public float BottomOffset = 50f;

    // ────── Internal ──────
    private enum FoxState { Moving, Idle, Sleeping }
    private FoxState _state = FoxState.Moving;

    private float _stateTimer = 0f;
    private float _idleDuration = 0f;
    private float _sleepDuration = 5f;

    private Vector2 _spriteSize;
    private float _targetX;
    private Random _rng = new Random();

    private RECT _workArea;

    public override void _Ready()
    {
        // Cache sprite size once
        _spriteSize = SpriteFrames.GetFrameTexture(GetAnimation(), GetFrame()).GetSize();

        UpdateWorkArea();
        _targetX = GetNewTargetX();
        Play("Idle");
    }

    public override void _Process(double delta)
    {
        UpdateWorkArea();
        HandleFoxBehavior((float)delta);
    }

    private void HandleFoxBehavior(float delta)
    {
        AlignYToWindowTop();
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
        }
    }

    private void MoveTowardTarget(float delta)
    {
        Vector2 pos = Position;
        float direction = _targetX > pos.X ? 1f : -1f;
        pos.X += direction * Speed * delta;

        float left = _workArea.Left;
        float right = _workArea.Right - _spriteSize.X;

        pos.X = Mathf.Clamp(pos.X, _workArea.Left, _workArea.Right - _spriteSize.X);
        pos.Y = _workArea.Top + TopOffset;

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
    }

    private float GetNewTargetX()
    {
        float left = (float)_workArea.Left;
        float right = (float)(_workArea.Right - _spriteSize.X);
        float randomX = (float)_rng.NextDouble() * (right - left) + left;
        return Mathf.Clamp(randomX, left, right);

    }

    private void UpdateWorkArea()
    {
        RECT rect = new RECT();
        IntPtr activeWindow = GetForegroundWindow();
        bool useFallback = false;

        // Get the active window's rect
        if (activeWindow == IntPtr.Zero ||
            activeWindow == GetFoxWindowHandle() ||
            !GetWindowRect(activeWindow, out rect))
        {
            useFallback = true;
        }
        else
        {
            int height = rect.Bottom - rect.Top;

            // If window is too small or too tall to host the fox
            if (height < _spriteSize.Y + TopOffset || rect.Top < 0)
                useFallback = true;
        }

        if (useFallback)
        {
            // Fall back to taskbar (bottom of primary screen)
            SystemParametersInfo(SPI_GETWORKAREA, 0, out rect, 0);
            rect.Top = rect.Bottom - (int)(_spriteSize.Y + BottomOffset);
            rect.Bottom = rect.Top + (int)_spriteSize.Y;
        }

        _workArea = rect;
    }


    private void AlignYToWindowTop()
    {
        Position = new Vector2(Position.X, _workArea.Top + TopOffset);
    }



    // ────── Windows API ──────
    private const uint SPI_GETWORKAREA = 0x0030;

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out RECT pvParam, uint fWinIni);
    [DllImport("user32.dll")]
    private static extern bool SystemParametersInfo(int uAction, int uParam, ref RECT lpvParam, int fuWinIni);
    private IntPtr GetFoxWindowHandle()
    {
        return Process.GetCurrentProcess().MainWindowHandle;
    }
}
