using Ascendere;
using Godot;

[MetaComponent("Energy", Description = "Tracks the energy level of an entity.")]
public struct EnergyComponent
{
    [Export]
    public float Current;
    public float Max;
    public float Percentage => Max > 0 ? Current / Max : 0f;
}
