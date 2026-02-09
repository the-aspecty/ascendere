using Godot.Collections;

namespace Ascendere.EditorRuntime
{
    public class AvailableCommandsMessage : RuntimeMessage
    {
        public override string Command => "announce_commands";
        public string[] Commands { get; set; } = new string[0];

        public override Array Serialize()
        {
            var arr = new Array();
            foreach (var c in Commands)
                arr.Add(c);
            return arr;
        }

        public override void Deserialize(Array data)
        {
            Commands = new string[data != null ? data.Count : 0];
            if (data == null)
                return;
            for (int i = 0; i < data.Count; i++)
            {
                Commands[i] = data[i].ToString();
            }
        }
    }
}
