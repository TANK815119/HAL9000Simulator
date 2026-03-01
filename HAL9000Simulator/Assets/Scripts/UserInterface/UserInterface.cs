using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(InputData))]
public class UserInterface : MonoBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private GameObject leftUIHand;
    [SerializeField] private GameObject rightUIHand;
    [SerializeField] private GameObject menu;
    [SerializeField] private GameObject monitor;
    [SerializeField] private GameObject cursor;
    [SerializeField] private LayerMask mask;
    [SerializeField] private Vector2 UIDirection = new Vector2(0.16f, -0.33f);
    [SerializeField] private bool portable = true;
    [SerializeField] private bool interactable = true;
    [SerializeField] private bool XROveride = false;

    private InputData inputData;
    private Vector3 innitialOffset;
    // Start is called before the first frame update
    void Start()
    {
        if(interactable)
        {
            if (portable)
            {
                menu.SetActive(false);
            }
            rightUIHand.SetActive(false);
            leftUIHand.SetActive(false);

            inputData = GetComponent<InputData>();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (interactable)
        {
            inputData.rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool menuPressed);

            if (menuPressed || !portable)
            {
                ShowMenu();
            }
            else
            {
                rightUIHand.SetActive(false);
                leftUIHand.SetActive(false);
                menu.SetActive(false);
            }

            if (XROveride == true)
            {
                ShowMenu();
            }
        }
    }

    private void ShowMenu()
    {
        if(portable)
        {
            if (!menu.activeInHierarchy)
            {
                //menu rotation is facing same direction as right hand innitially
                menu.transform.forward = head.forward;
                menu.transform.rotation = Quaternion.Euler(0f, menu.transform.eulerAngles.y, 0f);

                //offset from hand to menu
                innitialOffset = rightUIHand.transform.forward * UIDirection.x + rightUIHand.transform.up * UIDirection.y;
            }

            rightUIHand.SetActive(true);
            leftUIHand.SetActive(true);
            menu.SetActive(true);

            //menu position is a little forward of the right hand always
            menu.transform.position = rightUIHand.transform.position + innitialOffset;
        }

        //interactions from the cursor to the moniter
        //cursor.transform.position = rightUIHand.transform.position + rightUIHand.transform.forward * UIDirection.x + rightUIHand.transform.up * UIDirection.y;
        Ray ray = new Ray(rightUIHand.transform.position, rightUIHand.transform.forward * UIDirection.x + rightUIHand.transform.up * UIDirection.y);
        if (Physics.Raycast(ray, out RaycastHit info, Mathf.Infinity, mask))
        {
            if (info.collider.gameObject.Equals(monitor))
            {
                cursor.transform.position = info.point;
            }
        }
    }
}
