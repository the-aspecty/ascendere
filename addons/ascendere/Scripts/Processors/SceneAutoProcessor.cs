using Godot;

namespace Ascendere;

public partial class SceneAutoProcessor : Node
{
    public override void _EnterTree()
    {
        GetTree().NodeAdded += OnNodeAdded;
    }

    public override void _ExitTree()
    {
        GetTree().NodeAdded -= OnNodeAdded;
    }

    //make it more intelligent (pick from attribute)
    private void OnNodeAdded(Node node)
    {
        SceneReferenceProcessor.Instance.ProcessSceneReferences(node);
        NodeReferenceProcessor.Instance.ProcessNodeReferences(node);
        //InputActionProcessor.Instance.ProcessInputActions(node);
    }
}
