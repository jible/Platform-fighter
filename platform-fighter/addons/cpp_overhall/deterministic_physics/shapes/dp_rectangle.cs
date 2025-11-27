using Godot;
using System;

[Tool]
[GlobalClass]
public partial class dp_rectangle : dp_shape
{
     public DM_Vector2 size = new DM_Vector2(50,50);
    [Export]public dm_vector_editor_wrapper editor_size;
}
