using Godot;
using System;

// The hitbox editor's view of the character: the same view the game draws with, plus one sprite's boxes.
// Boxes are read from the built definition, so they're scaled exactly as in play.
// Lives in the editor's SubViewport, whose own world keeps it apart from everything else.
// Plain C#, owned by the HitboxEditor, which redraws it after every refresh and disposes it on exit
public class HitboxPreview : IDisposable
{
    // Unselected boxes are drawn fainter, so the one being edited stands out
    private const float SelectedAlpha = 0.7f;
    private const float UnselectedAlpha = 0.35f;
    // In front of the sprite, which sits at z = 0
    private const float BoxZ = 0.1f;
    private const float BoxThickness = 0.01f;

    public Color HurtboxColor = new(1f, 0.85f, 0.1f);
    public Color HitboxColor = new(1f, 0.15f, 0.15f);
    public Color PushboxColor = new(0.2f, 0.5f, 1f);
    public Color BodyColor = new(0.2f, 0.9f, 0.4f);

    // Stands in for the character root, so the view and boxes are placed relative to it
    private readonly Node3D root;
    private readonly DebugBoxDrawer drawer;
    private ICharacterView view;
    // The view reads its visuals from this, so a reload, which loads new data, needs a new view
    private CharacterData viewFor;

    public HitboxPreview(Node3D root)
    {
        this.root = root;
        drawer = new DebugBoxDrawer(root);
    }

    // 0 when there's no view, or the animation doesn't step
    public int FrameCount(string animation) => view?.FrameCount(animation) ?? 0;

    // Only rebuilds the view when the data changes, so its loaded sheets are kept between redraws.
    // data is the editor's own copy, see HitboxEditorManager.Data
    public void SetCharacter(CharacterId characterId, CharacterData data)
    {
        if (viewFor == data) return;
        if (view is Node oldView) oldView.QueueFree();
        view = CharacterViews.Create(characterId, data);
        if (view is Node viewNode) root.AddChild(viewNode);
        viewFor = data;
    }

    // stateName is any CharacterStateId name or attack. A state with no data shows the start of the sheet named
    // after it, like the game does. body is the physics body's size to draw over everything, or null to leave it out
    public void Show(CharacterDefinition definition, string stateName, int sprite, int selectedBoxId, DMVector? body = null)
    {
        StateDefinition state = definition.GetStateOrAttack(stateName);
        if (state == null)
        {
            view?.Show(CharacterFiles.AnimationName(stateName), null, 0, Character.Direction.Right);
            DrawBody(body);
            drawer.Submit();
            return;
        }

        sprite = Math.Clamp(sprite, 0, state.Grid.Count - 1);
        view?.Show(state.AnimationName, state.Grid, sprite, Character.Direction.Right);

        // The selected box last, so it draws over the rest
        BoxDefinition selected = null;
        foreach (BoxDefinition box in state.BoxesAt(sprite))
        {
            if (box.Id == selectedBoxId) selected = box;
            else Draw(box, UnselectedAlpha);
        }
        if (selected != null) Draw(selected, SelectedAlpha);
        DrawBody(body);
        drawer.Submit();
    }

    // Centered on the root, like the body is centered on the character's position
    private void DrawBody(DMVector? body)
    {
        if (!body.HasValue) return;
        Color color = BodyColor;
        color.A = SelectedAlpha;
        drawer.Draw(new Vector3(0, 0, BoxZ), body.Value.ToVector3(BoxThickness), color);
    }

    public void Dispose()
    {
        drawer.Dispose();
    }

    private void Draw(BoxDefinition box, float alpha)
    {
        Color color = box.Type switch
        {
            BoxType.Hitbox => HitboxColor,
            BoxType.Pushbox => PushboxColor,
            _ => HurtboxColor,
        };
        color.A = alpha;
        drawer.Draw(box.Center.ToVector3(BoxZ), box.Size.ToVector3(BoxThickness), color);
    }
}
