using TMPro;
using UnityEngine;

public class GetValueFromDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    public void GetDropdownValue(string type)
    {
        int pickedEntryIndex = dropdown.value;
        if (type == "DrawMode")
        {
            FluidDisplay fluidDisplay = FindFirstObjectByType<FluidDisplay>();
            if (fluidDisplay)
            {
                fluidDisplay.drawMode = (FluidDisplay.DrawMode)pickedEntryIndex;
            }
        } else if (type == "Size")
        {
            FluidTest fluidTest = FindFirstObjectByType<FluidTest>();
            if (fluidTest)
            {
                fluidTest.width = (pickedEntryIndex + 1) * 50;
                fluidTest.height = (pickedEntryIndex + 1) * 50;
            }
        } else if (type == "Velocity")
        {
            FluidDrawer fluidDrawer = FindFirstObjectByType<FluidDrawer>();
            if (fluidDrawer)
            {
                switch (pickedEntryIndex)
                {
                    case 0:
                        fluidDrawer.velocityStrength = 10;
                        break;
                    case 1:
                        fluidDrawer.velocityStrength = 20;
                        break;
                    case 2:
                        fluidDrawer.velocityStrength = 50;
                        break;
                    case 3:
                        fluidDrawer.velocityStrength = 100;
                        break;
                    case 4:
                        fluidDrawer.velocityStrength = 200;
                        break;
                    case 5:
                        fluidDrawer.velocityStrength = 500;
                        break;
                    case 6:
                        fluidDrawer.velocityStrength = 1000;
                        break;
                    default:
                        fluidDrawer.velocityStrength = 10;
                        break;
                }
            }
        }
    }
}