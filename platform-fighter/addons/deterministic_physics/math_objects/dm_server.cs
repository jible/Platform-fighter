using Godot;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            
            _visited.Clear();
            ConvertEditorMath();
        };

        GetTree().NodeAdded += (node) =>{
            ConvertEditorMath(node);
        };
    }

    public void CollectDM_Nodes(Node root = null )
    {
        DM_objects = [];
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


        foreach (var prop in Parent.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite)continue;
            if ((prop.PropertyType == typeof(DM64) || (prop.PropertyType == typeof(DM_Vector2))) && !DM_objects.Contains(Parent))
            {DM_objects.Add(Parent);}

            if (typeof(Resource).IsAssignableFrom(prop.PropertyType))
            {
                var r = prop.GetValue(Parent);
                if (r!= null) {_RecurseCollectDM_Nodes(r);}
            }
        }
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

        
        foreach (object obj in DM_objects)
        {
            var type = obj.GetType();
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                string FieldName = field.Name;

                Type FieldType = field.FieldType;

                bool IsDM64 = typeof(DM64) == FieldType;
                bool IsVector = typeof(DM_Vector2) == FieldType;

                string TargetFieldName = Prefix + FieldName; 


                if (IsDM64)
                {
                    System.Reflection.FieldInfo TargetField = type.GetField(TargetFieldName);

                    // If this target field is on the object, but returning as null,
                    // Make sure the export is a public var.
                    if (TargetField == null) continue;
                    
                    Type TargetFieldType = TargetField.FieldType;

                    if (TargetFieldType != typeof(string))
                    {
                        GD.PushError(TargetField , " field has wrong type to modify: ", FieldName);
                    }

                    field.SetValue(obj, new DM64((string)TargetField.GetValue(obj)));
                }

                if (IsVector)
                {
                    System.Reflection.FieldInfo TargetField = type.GetField(TargetFieldName);
                    if (TargetField == null) continue;
                    Type TargetFieldType = TargetField.FieldType;

                    if (TargetFieldType != typeof(Vector2I))
                    {
                        GD.PushError(TargetField , " field has wrong type to modify: ", FieldName);
                    }

                    
                    field.SetValue(obj, new DM_Vector2((Vector2I)TargetField.GetValue(obj)));
                    
                }
            }
        }
        
       
    }
}
