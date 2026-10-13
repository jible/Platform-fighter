using Godot;
using System.Collections.Generic;

// Draws a sprite character. It is the sprite itself, so a character's view is one node.
// Each animation is its own sheet in the character's folder: one row of frames in order, SpriteFrameWidth wide.
// The sheet's frames are spread evenly over the state's length
public partial class SpriteCharacterView : Sprite3D, ICharacterView
{
    // Used when the state has no authored length, so new art plays before its state is set up
    private const int UnauthoredTicksPerSprite = 6;

    private string folder;
    // Stands in for any animation with no sheet
    private string defaultAnimation;
    // 0 means square frames
    private int frameWidth;
    // Loaded on first use. Null entries are animations with no sheet, so they are only looked up once
    private readonly Dictionary<string, Texture2D> sheets = new();

    public void Configure(string folder, VisualData visuals)
    {
        this.folder = folder;
        defaultAnimation = visuals.DefaultAnimation;
        frameWidth = visuals.SpriteFrameWidth;
        PixelSize = visuals.PixelSize.ToFloat();
        // Pixel art, so no blurring between pixels
        TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
        // Sheets from a previous folder don't apply any more
        sheets.Clear();
    }

    public void Show(string animation, int frame, int length, Character.Direction facing)
    {
        // Art is drawn facing right
        FlipH = facing == Character.Direction.Left;

        Texture2D sheet = ResolveSheet(animation);
        // No sheet and no default keeps whatever was showing, so a missing animation is visible but not blank
        if (sheet == null) return;

        if (Texture != sheet)
        {
            // Reset first, since the old frame may be past the end of the new sheet
            Frame = 0;
            Texture = sheet;
            Hframes = FrameCount(sheet);
        }

        int index = length > 0
            ? frame * Hframes / length
            : frame / UnauthoredTicksPerSprite % Hframes;
        Frame = Mathf.Clamp(index, 0, Hframes - 1);
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

    private Texture2D GetSheet(string animation)
    {
        if (string.IsNullOrEmpty(animation)) return null;
        if (sheets.TryGetValue(animation, out Texture2D sheet)) return sheet;

        // Animations can be named with or without the extension
        string path = $"{folder}/{animation}";
        if (path.GetExtension() == "") path += ".png";
        sheet = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
        if (sheet == null)
        {
            GD.PushWarning($"No sprite sheet at {path}");
        }
        sheets[animation] = sheet;
        return sheet;
    }
}
