using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class LevelBlueprintNode : Node
{
    public TextField nodeNameField;
    public EnumField roomTypeField;
    public Slider spawnChanceField;
    public Toggle forceDirectionField;
    public EnumField requiredDirField;

    public Port inputPort;
    public Port outputPort;

    public LevelBlueprintNode()
    {
        title = "Room Node";

        // Create Input Port
        inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Create Output Port
        outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
        outputPort.portName = "Output";
        outputContainer.Add(outputPort);

        // Add fields to extension container
        var extension = extensionContainer;

        nodeNameField = new TextField("Name") { value = "New Room" };
        nodeNameField.RegisterValueChangedCallback(evt => { title = evt.newValue; });
        extension.Add(nodeNameField);

        roomTypeField = new EnumField("Type", RoomNodeType.Normal);
        extension.Add(roomTypeField);

        spawnChanceField = new Slider("Spawn Chance", 0f, 1f) { value = 1f };
        spawnChanceField.showInputField = true;
        extension.Add(spawnChanceField);

        forceDirectionField = new Toggle("Force Direction") { value = false };
        extension.Add(forceDirectionField);

        requiredDirField = new EnumField("Required Dir", ExitDirection.Up);
        extension.Add(requiredDirField);

        RefreshExpandedState();
        RefreshPorts();
    }
}
