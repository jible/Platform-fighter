using Godot;
using System;

// The hitbox editor's view of the character: the same view the game draws with, plus the selected state's boxes.
// Boxes are read from the built definition, so keys are blended and bone offsets applied exactly as in play.
// Lives in the editor's SubViewport, whose own world keeps it apart from everything else.
// Plain C#, owned by the HitboxEditor, which redraws it after every refresh and disposes it on exit
public class HitboxPreview : IDisposable
{
    // Unselected boxes are drawn fainter, so the one being edited stands out
    private const float SelectedAlpha = 0.7f;
    private const float UnselectedAlpha = 0.35f;
    // The selected box is still drawn on frames it's inactive, so a new box can be placed before it's turned on
    private const float InactiveAlpha = 0.12f;
    // In front of the sprite, which sits at z = 0
    private const float BoxZ = 0.1f;
    private const float BoxThickness = 0.01f;

    public Color HurtboxColor = new(1f, 0.85f, 0.1f);
    public Color HitboxColor = new(1f, 0.15f, 0.15f);
    public Color PushboxColor = new(0.2f, 0.5f, 1f);

    // Stands in for the character root, so the view and boxes are placed relative to it
    private readonly Node3D root;
    private readonly DebugBoxDrawer drawer;
    private ICharacterView view;
    private CharacterId? viewFor;

    public HitboxPreview(Node3D root)
    {
        this.root = root;
        drawer = new DebugBoxDrawer(root);
    }

    // 0 when there's no view, or the animation doesn't step
    public int FrameCount(string animation) => view?.FrameCount(animation) ?? 0;

    // Only rebuilds the view when the character changes, so its loaded sheets are kept between redraws
    public void SetCharacter(CharacterId characterId)
    {
        if (viewFor == characterId) return;
        if (view is Node oldView) oldView.QueueFree();
        view = CharacterViews.Create(characterId);
        if (view is Node viewNode) root.AddChild(viewNode);
        viewFor = characterId;
    }

    // stateName is any CharacterStateId name. A state with no data plays the sheet named after it, like the game does
    public void Show(CharacterDefinition definition, string stateName, int frame, int selectedBoxId)
    {
        StateDefinition state = definition.GetStateOrAttack(stateName);
        if (state == null)
        {
            view?.Show(stateName, frame, 0, null, Character.Direction.Right);
            drawer.Submit();
            return;
        }

        frame = Math.Clamp(frame, 0, state.Length - 1);
        view?.Show(state.AnimationName, frame, state.Length, state.Sprite, Character.Direction.Right);

        // The selected box last, so it draws over the rest
        BoxDefinition selected = null;
        foreach (BoxDefinition box in state.Boxes)
        {
            if (box.Id == selectedBoxId) selected = box;
            else if (box.IsActive(frame)) Draw(box, frame, UnselectedAlpha);
        }
        if (selected != null) Draw(selected, frame, selected.IsActive(frame) ? SelectedAlpha : InactiveAlpha);
        drawer.Submit();
    }

    public void Dispose()
    {
        drawer.Dispose();
    }

    private void Draw(BoxDefinition box, int frame, float alpha)
    {
        Color color = box.Type switch
        {
            BoxType.Hitbox => HitboxColor,
            BoxType.Pushbox => PushboxColor,
            _ => HurtboxColor,
        };
        color.A = alpha;
        drawer.Draw(box.CenterAt(frame).ToVector3(BoxZ), box.SizeAt(frame).ToVector3(BoxThickness), color);
    }
}
