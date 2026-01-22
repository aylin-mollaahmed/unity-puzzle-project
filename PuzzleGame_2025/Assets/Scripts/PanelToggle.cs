using UnityEngine;

public class PanelToggle : MonoBehaviour
{
    public GameObject panel;

    public void Toggle()
    {
        if (panel != null)
            panel.SetActive(!panel.activeSelf);
    }

    public void Close()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}

