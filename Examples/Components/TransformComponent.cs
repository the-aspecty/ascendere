using Ascendere;
using Godot;

[MetaComponent(
    "Transform",
    Description = "Tracks the position and rotation of an entity.",
    Category = "Core"
)]
public struct TransformComponent
{
    public Vector2 Position;
    public float Rotation;
}
