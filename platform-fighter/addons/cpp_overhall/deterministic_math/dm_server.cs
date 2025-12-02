using Godot;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;


public partial class dm_server : Node
{
    public List<object> DM_objects = [];
    private HashSet<object> _visited = new();
    public void CollectDM_Nodes( )
    {
        DM_objects = [];
        _visited.Clear();
        Node root = GetTree().CurrentScene;
        _RecurseCollectDM_Nodes(root);
    }

    private void _RecurseCollectDM_Nodes( Object Parent)
    {
        if (Parent == null){return;}
        if (_visited.Contains(Parent)) { return;}
        _visited.Add(Parent);


        foreach (var field in Parent.GetType().GetFields())
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


    public void ConvertEditorMath()
    {
        String Prefix = "Editor";
        int PrefixLength = Prefix.Length;

        foreach (object obj in DM_objects)
        {
            var type = obj.GetType();
            foreach (var field in type.GetFields())
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
