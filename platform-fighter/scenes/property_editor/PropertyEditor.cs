using Godot;
using System;
using System.Reflection;
using System.Text;

// Plays a character while a side panel edits their movement data live, and saves it to their file.
// The match is the play scene, instanced here so it can be rebuilt when the character changes.
// Edits write straight into the live characters' CharacterData, which their movement reads every tick, so nothing restarts.
// Offline only: the data isn't rollback state, and edits aren't sent to other machines
public partial class PropertyEditor : Node
{
    [Export] public PackedScene PlayScene;

    // The sections of CharacterData the panel edits, in dropdown order. Rows are made for their DM64 and int fields
    private static readonly (string Name, Func<CharacterData, object> Section)[] Categories =
    {
        ("Ground Movement", data => data.Ground),
        ("Air Movement", data => data.Air),
        ("Jump", data => data.Jump),
    };

    private const decimal DMScale = 4294967296m; // 2^32, DM64's fractional bits

    private PlayManager play;
    private OptionButton characterSelect;
    private OptionButton categorySelect;
    private VBoxContainer propertyList;

    public override void _Ready()
    {
        characterSelect = GetNode<OptionButton>("%CharacterSelect");
        categorySelect = GetNode<OptionButton>("%CategorySelect");
        propertyList = GetNode<VBoxContainer>("%PropertyList");

        foreach (CharacterId id in Enum.GetValues<CharacterId>())
        {
            characterSelect.AddItem(id.ToString(), (int)id);
        }
        foreach (var category in Categories)
        {
            categorySelect.AddItem(category.Name);
        }

        // Spawns whoever's set up in the PlayerManager, or the play scene's debug players when nobody is
        BuildPlay();
        Character edited = EditedCharacter();
        if (edited != null) characterSelect.Select(characterSelect.GetItemIndex((int)edited.CharacterId));
        BuildRows();

        characterSelect.ItemSelected += OnCharacterSelected;
        categorySelect.ItemSelected += _ => BuildRows();
        GetNode<Button>("%SaveButton").Pressed += Save;
    }

    // Every player plays the picked character. A new match is built, so unsaved edits are lost
    private void OnCharacterSelected(long index)
    {
        var id = (CharacterId)characterSelect.GetItemId((int)index);
        foreach (var profile in PlayerManager.GlobalInstance.AllPlayers)
        {
            if (profile != null) profile.Character = id;
        }
        BuildPlay();
        BuildRows();
    }

    private void BuildPlay()
    {
        if (play != null)
        {
            // Removed now rather than freed at the end of the frame, so the old world is disposed before the new one is built
            RemoveChild(play);
            play.QueueFree();
        }
        play = PlayScene.Instantiate<PlayManager>();
        AddChild(play);
        // Ahead of the UI, matching the scene's order
        MoveChild(play, 0);
    }

    // The panel shows the first character's values. Edits go to every character, since they all play the same one
    private Character EditedCharacter()
    {
        foreach (var character in play.CharacterHolder.Characters)
        {
            if (character != null) return character;
        }
        return null;
    }

    private void BuildRows()
    {
        foreach (Node row in propertyList.GetChildren())
        {
            propertyList.RemoveChild(row);
            row.QueueFree();
        }

        Character character = EditedCharacter();
        if (character == null) return;

        var getSection = Categories[categorySelect.Selected].Section;
        object section = getSection(character.Data);
        foreach (FieldInfo field in section.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.FieldType != typeof(DM64) && field.FieldType != typeof(int)) continue;
            propertyList.AddChild(MakeRow(field, field.GetValue(section), getSection));
        }
    }

    private HBoxContainer MakeRow(FieldInfo field, object value, Func<CharacterData, object> getSection)
    {
        bool isInt = field.FieldType == typeof(int);
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = DisplayName(field.Name), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        var input = new SpinBox
        {
            CustomMinimumSize = new Vector2(90, 0),
            Step = isInt ? 1 : 0.01,
            MaxValue = 1000,
            AllowGreater = true,
            Value = isInt ? (int)value : ToDouble((DM64)value),
        };
        input.ValueChanged += newValue =>
        {
            object converted = isInt ? (int)Math.Round(newValue) : ToDM64(newValue);
            foreach (var character in play.CharacterHolder.Characters)
            {
                if (character != null) field.SetValue(getSection(character.Data), converted);
            }
        };
        // The game reads the keyboard directly, so let go of it once a value is entered or typing keeps moving the character
        input.GetLineEdit().TextSubmitted += _ => input.GetLineEdit().ReleaseFocus();
        row.AddChild(input);
        return row;
    }

    // Writes every section's live values over the file's, leaving the rest of the file as it was
    private void Save()
    {
        Character character = EditedCharacter();
        if (character == null) return;

        CharacterFile file = CharacterFiles.ReadCharacter(character.CharacterId);
        file.Ground = CharacterFileJson.Copy(character.Data.Ground);
        file.Air = CharacterFileJson.Copy(character.Data.Air);
        file.Jump = CharacterFileJson.Copy(character.Data.Jump);
        CharacterFiles.WriteCharacter(character.CharacterId, file);
        GD.Print($"Saved {character.CharacterId}'s movement data");
    }

    // MaxFallVelocity reads as Max Fall Velocity
    private static string DisplayName(string fieldName)
    {
        var name = new StringBuilder();
        foreach (char c in fieldName)
        {
            if (char.IsUpper(c) && name.Length > 0) name.Append(' ');
            name.Append(c);
        }
        return name.ToString();
    }

    // Through decimal, like the file converter, so a typed 0.5 is the exact DM64 the file would read back
    private static DM64 ToDM64(double value) => DM64.FromRaw((long)Math.Round((decimal)value * DMScale, MidpointRounding.AwayFromZero));

    private static double ToDouble(DM64 value) => (double)Math.Round(value.RawValue / DMScale, 4);
}
