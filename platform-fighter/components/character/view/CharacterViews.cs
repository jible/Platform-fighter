using Godot;

// Makes the view a character is drawn with, picked by its VisualKind. The game and the hitbox editor both
// make views here, so the editor always shows exactly what plays. A new VisualKind only needs a case here
public static class CharacterViews
{
    // Null when the character's VisualKind isn't supported yet. The view is a node the caller adds to the tree
    public static ICharacterView Create(CharacterId characterId)
    {
        CharacterData data = Roster.Get(characterId);
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
