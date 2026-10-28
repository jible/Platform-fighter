using Godot;
using System;
using System.Collections.Generic;

// The strip above the frame slider showing which sprites each phase plays. Every phase has a start and an end marker
// in its own color. Dragging a marker moves the boundary it sits on, and the neighbor's marker follows it on screen,
// but nothing is reported until the mouse is released. The first start and last end are fixed.
// Clicking anywhere else picks the sprite under the mouse. It only reports, so the owner edits the data. Editor UI only
[GlobalClass]
public partial class PhaseTimeline : Control
{
    // Index of the phase left of the boundary, and the last sprite it now plays
    public event Action<int, int> BoundaryMoved;
    public event Action<int> SpriteClicked;
    // While dragging, the sprite the grabbed marker sits on: the last sprite of the phase before the boundary for an
    // end marker, the first of the phase after it for a start marker. Raised as the drag starts and whenever it moves
    public event Action<int> DragPreviewed;

    private const float MarkerWidth = 9f;
    private const float MarkerHeight = 12f;
    private const float BandHeight = 18f;
    // How far either side of a boundary a press grabs it
    private const float GrabDistance = 10f;
    private static readonly Color[] PhaseColors =
    {
        new(0.35f, 0.65f, 1f),
        new(1f, 0.35f, 0.3f),
        new(0.4f, 0.85f, 0.45f),
        new(1f, 0.75f, 0.25f),
        new(0.75f, 0.45f, 1f),
        new(0.3f, 0.85f, 0.85f),
    };
    private static readonly Color CellLineColor = new(1f, 1f, 1f, 0.15f);
    private static readonly Color PlayheadColor = Colors.White;

    private int spriteCount;
    // Inclusive sprite ranges, in play order with no gaps
    private readonly List<(int Start, int End, string Name)> phases = new();
    private int selectedPhase;
    private int currentSprite;
    private bool editable;

    // Phase left of the boundary being dragged, or -1, and where its end is on screen while dragging
    private int dragging = -1;
    private int dragEnd;
    // The right phase's start marker was grabbed, so the sprite under the mouse is where that phase starts
    private bool draggingStart;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, BandHeight + MarkerHeight + 2);
        MouseFilter = MouseFilterEnum.Stop;
    }

    public static Color ColorOf(int phase) => PhaseColors[phase % PhaseColors.Length];

    // editable lets the markers be dragged. A drag in progress is kept, so a redraw mid drag doesn't drop it
    public void Show(int spriteCount, IEnumerable<(int Start, int End, string Name)> phaseRanges, int selectedPhase, int currentSprite, bool editable)
    {
        this.spriteCount = spriteCount;
        phases.Clear();
        phases.AddRange(phaseRanges);
        this.selectedPhase = selectedPhase;
        this.currentSprite = currentSprite;
        this.editable = editable;
        if (!editable || dragging >= phases.Count - 1) dragging = -1;
        QueueRedraw();
    }

    private float CellWidth => spriteCount > 0 ? Size.X / spriteCount : Size.X;

    // Sprite under an x position, clamped to the sheet
    private int SpriteAt(float x) => Math.Clamp((int)(x / CellWidth), 0, Math.Max(0, spriteCount - 1));

    // Where phase i ends on screen, which is its own end unless a boundary next to it is being dragged
    private int ShownEnd(int i) => i == dragging ? dragEnd : phases[i].End;
    private int ShownStart(int i) => dragging >= 0 && i - 1 == dragging ? dragEnd + 1 : phases[i].Start;

    public override void _Draw()
    {
        if (spriteCount <= 0) return;
        float cell = CellWidth;
        float markerTop = BandHeight;
        Font font = GetThemeDefaultFont();
        int fontSize = GetThemeDefaultFontSize() - 4;

        for (int i = 0; i < phases.Count; i++)
        {
            int start = ShownStart(i);
            int end = ShownEnd(i);
            Color color = ColorOf(i);
            float left = start * cell;
            float right = (end + 1) * cell;

            Color band = color;
            band.A = i == selectedPhase ? 0.6f : 0.25f;
            DrawRect(new Rect2(left, 0, right - left, BandHeight), band);
            if (i == selectedPhase) DrawRect(new Rect2(left, 0, right - left, BandHeight), color, false, 2f);
            if (right - left > 12)
            {
                DrawString(font, new Vector2(left + 4, BandHeight - 5), phases[i].Name, HorizontalAlignment.Left, right - left - 8, fontSize);
            }

            // Start marker hangs right of its line and end marker left of its, so a shared boundary shows both
            DrawMarker(left, markerTop, color, true, editable && i > 0);
            DrawMarker(right, markerTop, color, false, editable && i < phases.Count - 1);
        }

        for (int sprite = 1; sprite < spriteCount; sprite++)
        {
            DrawLine(new Vector2(sprite * cell, 0), new Vector2(sprite * cell, BandHeight), CellLineColor);
        }

        float playhead = (currentSprite + 0.5f) * cell;
        DrawLine(new Vector2(playhead, 0), new Vector2(playhead, Size.Y), PlayheadColor, 2f);
    }

    // A fixed marker is drawn hollow, so it's clear it can't be dragged
    private void DrawMarker(float x, float top, Color color, bool pointsRight, bool movable)
    {
        float tip = pointsRight ? x + MarkerWidth : x - MarkerWidth;
        var points = new[] { new Vector2(x, top), new Vector2(x, top + MarkerHeight), new Vector2(tip, top) };
        if (movable) DrawColoredPolygon(points, color);
        else DrawPolyline(new[] { points[0], points[1], points[2], points[0] }, color, 1f);
        DrawLine(new Vector2(x, 0), new Vector2(x, top + MarkerHeight), color, movable ? 2f : 1f);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (spriteCount <= 0) return;
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
        {
            if (button.Pressed)
            {
                int boundary = editable ? BoundaryAt(button.Position.X) : -1;
                if (boundary >= 0)
                {
                    dragging = boundary;
                    dragEnd = phases[boundary].End;
                    draggingStart = button.Position.X >= (phases[boundary].End + 1) * CellWidth;
                    DragPreviewed?.Invoke(MarkerSprite);
                }
                else
                {
                    SpriteClicked?.Invoke(SpriteAt(button.Position.X));
                }
                AcceptEvent();
            }
            else if (dragging >= 0)
            {
                int boundary = dragging;
                int end = dragEnd;
                dragging = -1;
                QueueRedraw();
                if (end != phases[boundary].End) BoundaryMoved?.Invoke(boundary, end);
                AcceptEvent();
            }
        }
        else if (@event is InputEventMouseMotion hover && dragging < 0)
        {
            // A resize cursor over a boundary that can be dragged, so it's clear where to grab
            MouseDefaultCursorShape = editable && BoundaryAt(hover.Position.X) >= 0 ? CursorShape.Hsize : CursorShape.Arrow;
        }
        else if (@event is InputEventMouseMotion motion && dragging >= 0)
        {
            // Every phase keeps at least one sprite, so the boundary stops at the neighbors' other ends
            int sprite = SpriteAt(motion.Position.X);
            int end = draggingStart ? sprite - 1 : sprite;
            int oldEnd = dragEnd;
            dragEnd = Math.Clamp(end, phases[dragging].Start, phases[dragging + 1].End - 1);
            QueueRedraw();
            if (dragEnd != oldEnd) DragPreviewed?.Invoke(MarkerSprite);
            AcceptEvent();
        }
    }

    private int MarkerSprite => draggingStart ? dragEnd + 1 : dragEnd;

    // Index of the phase left of the boundary whose markers are under x, or -1
    private int BoundaryAt(float x)
    {
        float cell = CellWidth;
        for (int i = 0; i < phases.Count - 1; i++)
        {
            float line = (phases[i].End + 1) * cell;
            if (Math.Abs(x - line) <= GrabDistance) return i;
        }
        return -1;
    }
}
