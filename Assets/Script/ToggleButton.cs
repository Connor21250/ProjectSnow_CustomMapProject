using UnityEngine;

public class ToggleButton : MonoBehaviour
{
    public GameObject[] objectsToEnable;
    public GameObject[] objectsToDisable;

    private bool pressed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Finger") && !pressed)
        {
            pressed = true;
            ActivateButton();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Finger"))
        {
            pressed = false;
        }
    }

    void ActivateButton()
    {
        foreach (GameObject obj in objectsToEnable)
            if (obj != null) obj.SetActive(true);

        foreach (GameObject obj in objectsToDisable)
            if (obj != null) obj.SetActive(false);
    }
}