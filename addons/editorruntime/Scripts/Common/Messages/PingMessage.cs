using Godot.Collections;

namespace Ascendere.EditorRuntime
{
    public class PingMessage : RuntimeMessage
    {
        public override string Command => "ping";
        public string Text { get; set; } = "Ping";

        public override Array Serialize()
        {
            return new Array { Text };
        }

        public override void Deserialize(Array data)
        {
            if (data != null && data.Count > 0)
            {
                Text = data[0].AsString();
            }
        }
    }
}
