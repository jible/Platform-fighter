using Godot;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


public partial class dm_server : Node
{
    // Singleton: Every time a scene starts, it accesses every node and resource that has a DM64 and dmvector
    // It extracts the value from any exported property with the "Editor" prefix and sends its value to the true run time value.
    // Though oddly complicated and annoying this is the only way to export deterministic math to the editor.
    public List<object> DM_objects = [];
    private HashSet<object> _visited = new();
    public override void _Ready()
    {
        ConvertEditorMath();

        GetTree().SceneChanged += () =>
        {
            ConvertEditorMath();
        };

        GetTree().NodeAdded += ConvertEditorMath;
    }

    public void CollectDM_Nodes(Node root = null )
    {
        DM_objects = [];
        _visited.Clear();
        if (root == null)
        {
            root = GetTree().CurrentScene;
        }
        _RecurseCollectDM_Nodes(root);
    }

    private void _RecurseCollectDM_Nodes( Object Parent)
    {
        if (Parent == null){return;}
        if (_visited.Contains(Parent)) { return;}
        _visited.Add(Parent);


        foreach (var field in Parent.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if ((field.FieldType == typeof(DM64) || (field.FieldType == typeof(DM_Vector2))) && !DM_objects.Contains(Parent))
            {DM_objects.Add(Parent);}
            if (typeof(Resource).IsAssignableFrom(field.FieldType))
            {
                var r = field.GetValue(Parent);
                if (r!= null) {_RecurseCollectDM_Nodes(r);}
            }
        }
        if ( Parent is not Node node) { return;}
        foreach (var child in node.GetChildren())
        {
            _RecurseCollectDM_Nodes(child);
        }
    }


    public void ConvertEditorMath(Node RootNode = null)
    {

        CollectDM_Nodes(RootNode);

        string Prefix = "Editor";
        int PrefixLength = Prefix.Length;

        foreach (object obj in DM_objects)
        {
            var type = obj.GetType();
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                string FieldName = (string)field.Name;

                Type FieldType = field.FieldType;
                object value = field.GetValue(obj);

                if (!FieldName.StartsWith(Prefix)){continue;}
                string ToBeUpdatedFieldName = FieldName[PrefixLength..];

                System.Reflection.FieldInfo ToBeUpdatedField = type.GetField(ToBeUpdatedFieldName);

                if (ToBeUpdatedField == null){ continue;}
                Type ToBeUpdateFieldType = ToBeUpdatedField.FieldType;
                if (ToBeUpdatedField == null){ continue;}
                if ( FieldType == typeof(float) && ToBeUpdateFieldType == typeof(DM64) )
                {
                    ToBeUpdatedField.SetValue(obj, new DM64((float)value));
                }
                else if (FieldType == typeof(Vector2) && ToBeUpdateFieldType == typeof(DM_Vector2)){
                    ToBeUpdatedField.SetValue(obj, new DM_Vector2((Vector2)value));
                    
                }
            }
        }
    }
}
