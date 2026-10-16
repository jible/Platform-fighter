using Godot;
using System;

// An ItemList whose items can be dragged up and down. It only reports the move, so the owner edits the data
// and refills the list. Editor UI only
[GlobalClass]
public partial class SequenceList : ItemList
{
    // From and to indexes. The item should end up at to, with the ones between shifted over
    public event Action<int, int> ItemMoved;

    // Item being dragged, or -1, so drops from anywhere else are ignored
    private int dragging = -1;

    public override Variant _GetDragData(Vector2 atPosition)
    {
        int index = GetItemAtPosition(atPosition, true);
        if (index < 0) return default;
        dragging = index;
        SetDragPreview(new Label { Text = GetItemText(index) });
        return index;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) => dragging >= 0;

    // Dropped below the last item moves to the end
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        int from = data.AsInt32();
        dragging = -1;
        int to = GetItemAtPosition(atPosition, true);
        if (to < 0) to = ItemCount - 1;
        if (to != from) ItemMoved?.Invoke(from, to);
    }

    // A drag that ends anywhere else is cancelled
    public override void _Notification(int what)
    {
        if (what == NotificationDragEnd) dragging = -1;
    }
}
