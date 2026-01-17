using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MoveAndShowPanel : MonoBehaviour, IPointerClickHandler
{
    [Header("Assign in Inspector")]
    public RectTransform panel;        // RectTransform на Difficulty (TMP_Dropdown)
    public CanvasGroup panelGroup;     // CanvasGroup на Difficulty

    [Header("Position")]
    public Vector2 offset = new Vector2(0f, -35f);

    private RectTransform canvasRect;

    public HomeSceneManager homeManager;
    public int pictureId = 1;

    void Awake()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.transform as RectTransform;

        if (panelGroup != null)
        {
            panelGroup.alpha = 0;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (panel == null || panelGroup == null || canvasRect == null) return;

        // менюто да е под Canvas и над всичко
        panel.SetParent(canvasRect, worldPositionStays: false);
        panel.SetAsLastSibling();

        RectTransform imgRect = transform as RectTransform;
        if (imgRect == null) return;

        // долен център на картинката (world)
        Vector3 worldBottomCenter = imgRect.TransformPoint(
            new Vector3(imgRect.rect.center.x, imgRect.rect.min.y, 0f)
        );

        // най-стабилно: камерата от самия клик (работи и при Overlay)
        Camera cam = eventData.pressEventCamera;

        // world -> screen -> local(canvas)
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldBottomCenter);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, cam, out Vector2 localPos
        );

        panel.anchoredPosition = localPos + offset;

        panelGroup.alpha = 1;
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;

        if (homeManager != null)
            homeManager.OnPictureClicked(pictureId);
    }
}

