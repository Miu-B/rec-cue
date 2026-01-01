using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;

namespace RecCue.Windows;

public class RecIndicator : IDisposable
{
    private readonly RecCuePlugin plugin;
    private readonly IClientState clientState;
    private readonly ICondition condition;

    private bool _isRecordingActive;
    private bool _wasFolderInError;

    private bool _dragging;
    private Vector2 _dragOffset;

    public RecIndicator(RecCuePlugin plugin, IClientState clientState, ICondition condition)
    {
        this.plugin = plugin;
        this.clientState = clientState;
        this.condition = condition;
        plugin.RecordingLogic.RecordingStateChanged += OnRecordingStateChanged;
        _isRecordingActive = plugin.RecordingLogic.IsRecordingActive;
    }

    public void Dispose()
    {
        plugin.RecordingLogic.RecordingStateChanged -= OnRecordingStateChanged;
    }

    private bool IsInValidGameplayState()
    {
        if (!clientState.IsLoggedIn)
            return false;

        if (clientState.IsGPosing)
            return false;

        if (condition[ConditionFlag.WatchingCutscene] ||
            condition[ConditionFlag.WatchingCutscene78] ||
            condition[ConditionFlag.OccupiedInCutSceneEvent])
            return false;

        if (condition[ConditionFlag.BetweenAreas] ||
            condition[ConditionFlag.BetweenAreas51])
            return false;

        if (condition[ConditionFlag.CreatingCharacter])
            return false;

        return true;
    }

    private void OnRecordingStateChanged(bool isActive) => _isRecordingActive = isActive;

    private bool IsFolderInError()
    {
        if (string.IsNullOrEmpty(plugin.Configuration.MonitoredFolderPath))
            return true;

        return !System.IO.Directory.Exists(plugin.Configuration.MonitoredFolderPath);
    }

    private static void DrawCornerAccents(ImDrawListPtr drawList, Vector2 winPos, Vector2 winSize, float rounding, float scale)
{
    var color = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.9f));
    var thickness = 2.0f * scale;          // “thick” line
    var len = 8.0f * scale;               // how far the accent extends along edges
    var half = thickness * 0.5f;

    // Inset so stroke stays inside the window and never clips
    var p0 = winPos + new Vector2(half, half);
    var p1 = winPos + winSize - new Vector2(half, half);

    var x0 = p0.X; var y0 = p0.Y;
    var x1 = p1.X; var y1 = p1.Y;

    var r = MathF.Max(0.0f, rounding - half); // compensate for stroke thickness

    // Top-left
    drawList.PathClear();
    drawList.PathLineTo(new Vector2(x0, y0 + r + len));
    drawList.PathLineTo(new Vector2(x0, y0 + r));
    drawList.PathArcTo(new Vector2(x0 + r, y0 + r), r, MathF.PI, 1.5f * MathF.PI);
    drawList.PathLineTo(new Vector2(x0 + r + len, y0));
    drawList.PathStroke(color, ImDrawFlags.None, thickness);

    // Top-right
    drawList.PathClear();
    drawList.PathLineTo(new Vector2(x1 - r - len, y0));
    drawList.PathLineTo(new Vector2(x1 - r, y0));
    drawList.PathArcTo(new Vector2(x1 - r, y0 + r), r, 1.5f * MathF.PI, 2.0f * MathF.PI);
    drawList.PathLineTo(new Vector2(x1, y0 + r + len));
    drawList.PathStroke(color, ImDrawFlags.None, thickness);

    // Bottom-right
    drawList.PathClear();
    drawList.PathLineTo(new Vector2(x1, y1 - r - len));
    drawList.PathLineTo(new Vector2(x1, y1 - r));
    drawList.PathArcTo(new Vector2(x1 - r, y1 - r), r, 0.0f, 0.5f * MathF.PI);
    drawList.PathLineTo(new Vector2(x1 - r - len, y1));
    drawList.PathStroke(color, ImDrawFlags.None, thickness);

    // Bottom-left
    drawList.PathClear();
    drawList.PathLineTo(new Vector2(x0 + r + len, y1));
    drawList.PathLineTo(new Vector2(x0 + r, y1));
    drawList.PathArcTo(new Vector2(x0 + r, y1 - r), r, 0.5f * MathF.PI, MathF.PI);
    drawList.PathLineTo(new Vector2(x0, y1 - r - len));
    drawList.PathStroke(color, ImDrawFlags.None, thickness);
}


    public void Draw()
    {
        if (plugin.Configuration.HideIndicator)
            return;

        if (!IsInValidGameplayState())
            return;

        var localPos = plugin.Configuration.IndicatorPosition;
        var scale = plugin.Configuration.IndicatorScale;
        var size = new Vector2(100 * scale, 40 * scale);

        var unlocked = plugin.ConfigWindow.IsOpen;

        // Apply configured position/size
        ImGui.SetNextWindowPos(localPos, ImGuiCond.Always);
        ImGui.SetNextWindowSize(size, ImGuiCond.Always);

        var flags =
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoBackground;

        if (!unlocked)
            flags |= ImGuiWindowFlags.NoInputs;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10.0f * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        if (ImGui.Begin("###RecCueIndicator", flags))
        {
            var drawList = ImGui.GetWindowDrawList();

            // Use full window coords (NOT cursor/content coords)
            var winPos = ImGui.GetWindowPos();
            var winSize = ImGui.GetWindowSize();

            // Draw over whole window, not just content clip
            drawList.PushClipRect(winPos, winPos + winSize, true);

            var bgColorU32 = ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 0.9f));
            var textColorU32 = ImGui.GetColorU32(new Vector4(0.95f, 0.95f, 0.95f, 1.0f));

            var rounding = 5.0f * scale;
            drawList.AddRectFilled(winPos, winPos + winSize, bgColorU32, rounding);

            // Monitoring restart logic (fixed)
            var folderErr = IsFolderInError();
            if (_wasFolderInError && !folderErr)
                plugin.StartMonitoring();
            _wasFolderInError = folderErr;

            // Dot
            var dotSize = 22.0f * scale;
            var dotCenter = new Vector2(
                winPos.X + (10.0f * scale) + dotSize / 2.0f,
                winPos.Y + winSize.Y / 2.0f
            );

            Vector4 dotColor =
                folderErr ? new Vector4(1.0f, 0.6f, 0.0f, 1.0f) :
                _isRecordingActive ? GetPulsingRedColor() :
                new Vector4(0.5f, 0.5f, 0.5f, 1.0f);

            drawList.AddCircleFilled(dotCenter, dotSize / 2.0f, ImGui.GetColorU32(dotColor), 32);

            var borderColorU32 = ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 0.65f));
            var borderThickness = 1.5f * scale;
            
            // Inset by half thickness so the stroke stays inside the window rect
            var half = borderThickness * 0.5f;
            var p0 = winPos + new Vector2(half, half);
            var p1 = winPos + winSize - new Vector2(half, half);

            DrawCornerAccents(drawList, winPos, winSize, rounding, scale);

            // Text (crisp): use resized default font handle from plugin
            using var _font = plugin.RecIndicatorFont.Available ? plugin.RecIndicatorFont.Push() : null;

            const string text = "Rec";
            var textSize = ImGui.CalcTextSize(text);

            var textPos = new Vector2(
                winPos.X + dotSize + 20.0f * scale,
                winPos.Y + (winSize.Y - textSize.Y) / 2.0f
            );

            drawList.AddText(textPos, textColorU32, text);

            drawList.PopClipRect();

            // --- Dragging (reliable): decide hover/click using ImGui window hover after Begin() ---
            if (unlocked)
            {
                var hovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);

                if (!_dragging && hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _dragging = true;
                    _dragOffset = ImGui.GetMousePos() - winPos;
                }

                if (_dragging)
                {
                    if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                    {
                        var newPos = ImGui.GetMousePos() - _dragOffset;

                        // Move the window immediately this frame…
                        ImGui.SetWindowPos(newPos);

                        // …and persist to config so next frame SetNextWindowPos keeps it there
                        plugin.Configuration.IndicatorPosition = newPos;
                    }
                    else
                    {
                        _dragging = false;
                        plugin.Configuration.Save();
                    }
                }
            }
            else
            {
                _dragging = false;
            }
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private static Vector4 GetPulsingRedColor()
    {
        var time = (float)ImGui.GetTime();
        var pulse = (MathF.Sin(time * MathF.PI) + 1.0f) / 2.0f;
        var alpha = 0.4f + 0.6f * pulse; // 0.4..1.0
        return new Vector4(1.0f, 0.0f, 0.0f, alpha);
    }
}

