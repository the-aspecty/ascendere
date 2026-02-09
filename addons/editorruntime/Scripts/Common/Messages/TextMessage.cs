using Godot.Collections;

namespace Ascendere.EditorRuntime
{
    public class TextMessage : RuntimeMessage
    {
        public override string Command => "text_message";
        public string Text { get; set; } = "";

        public TextMessage() { }

        public TextMessage(string text)
        {
            Text = text;
        }

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
