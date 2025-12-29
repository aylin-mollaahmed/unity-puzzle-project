using UnityEngine;
using UnityEngine.EventSystems;

public class ImageHoverClick : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public static ImageHoverClick selectedImage;

    [Header("Scales")]
    public float hoverScale = 1.1f;
    public float selectedScale = 1.2f;

    [Header("Speed")]
    public float speed = 12f;

    [Header("Panel Offset")]
    [SerializeField] private float panelOffsetY = 60f;

    private Vector3 normalScale;
    private Vector3 targetScale;
    private bool isSelected = false;

    void Awake()
    {
        normalScale = transform.localScale;
        targetScale = normalScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * speed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected)
            targetScale = normalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected)
            targetScale = normalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SelectThisImage();
    }

    void SelectThisImage()
    {
        if (selectedImage != null && selectedImage != this)
            selectedImage.Deselect();

        selectedImage = this;
        isSelected = true;
        targetScale = normalScale * selectedScale;

        if (LevelPanelController.Instance != null)
            LevelPanelController.Instance.ShowLevels();
    }

    public void Deselect()
    {
        isSelected = false;
        targetScale = normalScale;
    }
}


