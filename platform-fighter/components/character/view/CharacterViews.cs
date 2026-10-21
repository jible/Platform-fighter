using Godot;

// Makes the view a character is drawn with, picked by its VisualKind. The game and the hitbox editor both
// make views here, so the editor always shows exactly what plays. A new VisualKind only needs a case here
public static class CharacterViews
{
    // Null when the character's VisualKind isn't supported yet. The view is a node the caller adds to the tree.
    // data is the character's loaded data, see CharacterFiles.LoadData. The view keeps reading its visuals,
    // so the editor's changes to them show on the next Show
    public static ICharacterView Create(CharacterId characterId, CharacterData data)
    {
        switch (data.Visuals.Kind)
        {
            case VisualKind.Sprite:
                var view = new SpriteCharacterView { Name = "View" };
                view.Configure(CharacterFiles.ResourceFolder(characterId), data.Visuals);
                return view;
            default:
                GD.PushError($"{characterId} uses {data.Visuals.Kind} visuals, which aren't supported yet");
                return null;
        }
    }
}
