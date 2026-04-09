using UnityEngine;

public class GateController : MonoBehaviour
{
    [Header("Door References")]
    public Transform leftDoor;
    public Transform rightDoor;
    
    [Header("Positions")]
    public Vector3 leftDoorClosedPos;
    public Vector3 leftDoorOpenPos;
    public Vector3 rightDoorClosedPos;
    public Vector3 rightDoorOpenPos;

    [Header("Settings")]
    public float moveSpeed = 5f;
    public bool isOpen = false;

    private void Start()
    {
        if (leftDoor != null)
        {
            leftDoorClosedPos = leftDoor.localPosition;
            leftDoorOpenPos = leftDoorClosedPos + new Vector3(-3f, 0, 0); // Default open offset
        }

        if (rightDoor != null)
        {
            rightDoorClosedPos = rightDoor.localPosition;
            rightDoorOpenPos = rightDoorClosedPos + new Vector3(3f, 0, 0); // Default open offset
        }
    }

    private void Update()
    {
        if (leftDoor != null && rightDoor != null)
        {
            Vector3 targetLeft = isOpen ? leftDoorOpenPos : leftDoorClosedPos;
            Vector3 targetRight = isOpen ? rightDoorOpenPos : rightDoorClosedPos;

            leftDoor.localPosition = Vector3.Lerp(leftDoor.localPosition, targetLeft, Time.deltaTime * moveSpeed);
            rightDoor.localPosition = Vector3.Lerp(rightDoor.localPosition, targetRight, Time.deltaTime * moveSpeed);
        }
    }

    [ContextMenu("Open Gate")]
    public void OpenGate()
    {
        isOpen = true;
    }

    [ContextMenu("Close Gate")]
    public void CloseGate()
    {
        isOpen = false;
    }

    [ContextMenu("Toggle Gate")]
    public void ToggleGate()
    {
        isOpen = !isOpen;
    }
}
