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
        }
    }
}