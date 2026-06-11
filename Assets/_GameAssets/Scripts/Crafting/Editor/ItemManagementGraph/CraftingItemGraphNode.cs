using UnityEngine;
using Unity.GraphToolkit.Editor;
using System;

[Serializable]
public class CraftingItemGraphNode : Node
{
    private const string PrerequisitePortName = "Prerequisite";

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<CraftingItemData>(PrerequisitePortName)
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();
    }
}
