using Ascendere;
using Godot;

[MetaComponent(
    "Target",
    Description = "Tracks the target entity and its position.",
    Category = "Combat"
)]
public struct TargetComponent
{
    public int TargetEntityId;
    public TransformComponent? TargetPosition;
    public float Distance;
    public bool HasTarget;

    public void SetTarget(int entityId, TransformComponent position)
    {
        TargetEntityId = entityId;
        TargetPosition = position;
        HasTarget = true;
    }

    public void ClearTarget()
    {
        TargetEntityId = -1;
        TargetPosition = null;
        HasTarget = false;
    }
}

public partial class TargetSystem : Node
{
    public void UpdateTargetDistance(
        ref TargetComponent targetComponent,
        TransformComponent ownerTransform
    )
    {
        if (targetComponent.HasTarget && targetComponent.TargetPosition.HasValue)
        {
            var targetPos = targetComponent.TargetPosition.Value.Position;
            var ownerPos = ownerTransform.Position;
            targetComponent.Distance = ownerPos.DistanceTo(targetPos);
        }
        else
        {
            targetComponent.Distance = float.MaxValue;
        }
    }

    public bool IsTargetInRange(ref TargetComponent targetComponent, RangeComponent range)
    {
        return targetComponent.HasTarget && targetComponent.Distance <= range.Value;
    }
}
