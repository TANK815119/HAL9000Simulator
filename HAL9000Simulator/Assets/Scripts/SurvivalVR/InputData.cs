using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class InputData : MonoBehaviour
{
    public InputDevice rightController;
    public InputDevice leftController;
    public InputDevice headset;
    // Start is called before the first frame update

    private void InitializeInputDevice()
    {
        if (!rightController.isValid)
        {
            InitializeDeviceInputs(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, ref rightController);
        }
        if (!leftController.isValid)
        {
            InitializeDeviceInputs(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, ref leftController);
        }
        if (!headset.isValid)
        {
            InitializeDeviceInputs(InputDeviceCharacteristics.HeadMounted, ref headset);
        }
    }

    private void InitializeDeviceInputs(InputDeviceCharacteristics inputCharacteristics, ref InputDevice inputDevice)
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(inputCharacteristics, devices);
        if(devices.Count > 0)
        {
            inputDevice = devices[0];
        }
    }

    void Update()
    {
        if(!rightController.isValid || !leftController.isValid || !headset.isValid)
        {
            InitializeInputDevice();
        }
    }
}
