using Godot;
using System.Collections.Generic;

// Draws a sprite character. It is the sprite itself, so a character's view is one node.
// Each animation is a sheet in the character's folder. A state with a sprite sequence cuts its sheet into the
// sequence's grid and spreads the sequence evenly over the length. Otherwise the sheet is one row of
// frames, SpriteFrameWidth wide, spread evenly over the state's length
public partial class SpriteCharacterView : Sprite3D, ICharacterView
{
    // Used when the state has no authored length, so new art plays before its state is set up
    private const int UnauthoredTicksPerSprite = 6;

    private string folder;
    // Kept, so the offset is read on every Show and the editor can move it
    private VisualData visuals;
    // Stands in for any animation with no sheet
    private string defaultAnimation;
    // 0 means square frames
    private int frameWidth;
    // Loaded on first use. Null entries are animations with no sheet, so they are only looked up once
    private readonly Dictionary<string, Texture2D> sheets = new();

    public void Configure(string folder, VisualData visuals)
    {
        this.folder = folder;
        this.visuals = visuals;
        defaultAnimation = visuals.DefaultAnimation;
        frameWidth = visuals.SpriteFrameWidth;
        PixelSize = visuals.PixelSize.ToFloat();
        // Pixel art, so no blurring between pixels
        TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
        // Cut out instead of blended, so the sprite draws with the opaque pass and writes depth. Blended, it is sorted
        // against the debug box MultiMesh by bounding box centre, which flips as characters move
        AlphaCut = AlphaCutMode.Discard;
        // Sheets from a previous folder don't apply any more
        sheets.Clear();
        PlaceAtOffset(Character.Direction.Right);
    }

    // In box units, so it's scaled by the pixel size like the boxes are
    private void PlaceAtOffset(Character.Direction facing)
    {
        Vector3 offset = (visuals.Offset * visuals.BoxUnit).ToVector3();
        if (facing == Character.Direction.Left) offset.X = -offset.X;
        Position = offset;
    }

    public void Show(string animation, int frame, int length, SpriteTrack sprite, Character.Direction facing)
    {
        // Art is drawn facing right
        FlipH = facing == Character.Direction.Left;
        PlaceAtOffset(facing);

        Texture2D sheet = GetSheet(animation);
        // The sequence's grid is for its own sheet, not the default standing in for it
        if (sheet == null) sprite = null;
        sheet ??= GetSheet(defaultAnimation);
        // No sheet and no default keeps whatever was showing, so a missing animation is visible but not blank
        if (sheet == null) return;

        int columns = sprite?.Columns ?? FrameCount(sheet);
        int rows = sprite?.Rows ?? 1;
        if (Texture != sheet || Hframes != columns || Vframes != rows)
        {
            // Reset first, since the old frame may be past the end of the new grid
            Frame = 0;
            Texture = sheet;
            Hframes = columns;
            Vframes = rows;
        }

        int index;
        if (sprite != null) index = length > 0 ? sprite.SpriteAt(frame, length) : 0;
        else if (length > 0) index = frame * columns / length;
        else index = frame / UnauthoredTicksPerSprite % columns;
        Frame = Mathf.Clamp(index, 0, columns * rows - 1);
    }

    // Frames Show spreads over the state for this animation, or 0 when there is no sheet and no default
    public int FrameCount(string animation)
    {
        Texture2D sheet = ResolveSheet(animation);
        return sheet != null ? FrameCount(sheet) : 0;
    }

    private int FrameCount(Texture2D sheet)
    {
        int width = frameWidth > 0 ? frameWidth : sheet.GetHeight();
        return Mathf.Max(1, sheet.GetWidth() / width);
    }

    private Texture2D ResolveSheet(string animation) => GetSheet(animation) ?? GetSheet(defaultAnimation);

    // Where an animation's sheet lives. Animations can be named with or without the extension
    public static string SheetPath(string folder, string animation)
    {
        string path = $"{folder}/{animation}";
        return path.GetExtension() == "" ? path + ".png" : path;
    }

    private Texture2D GetSheet(string animation)
    {
        if (string.IsNullOrEmpty(animation)) return null;
        if (sheets.TryGetValue(animation, out Texture2D sheet)) return sheet;

        string path = SheetPath(folder, animation);
        sheet = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
        if (sheet == null)
        {
            GD.PushWarning($"No sprite sheet at {path}");
        }
        sheets[animation] = sheet;
        return sheet;
    }
}
