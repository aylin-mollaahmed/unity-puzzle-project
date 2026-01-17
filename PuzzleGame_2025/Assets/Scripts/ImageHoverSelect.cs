using UnityEngine;
using UnityEngine.EventSystems;

public class ImageHoverSelect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public static ImageHoverSelect selectedImage;

    [Header("Scale")]
    public float hoverScale = 1.1f;
    public float selectedScale = 1.2f;

    [Header("Smooth speed")]
    public float speed = 10f;

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
        SelectThis();
    }

    private void SelectThis()
    {
        if (selectedImage != null && selectedImage != this)
            selectedImage.Deselect();

        selectedImage = this;
        isSelected = true;
        targetScale = normalScale * selectedScale;
    }

    private void Deselect()
    {
        isSelected = false;
        targetScale = normalScale;
    }
}
