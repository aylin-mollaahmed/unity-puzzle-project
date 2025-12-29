using UnityEngine;

public class LevelPanelAnimator : MonoBehaviour
{
    public float slideDuration = 0.35f;
    public float targetBottom = 0f;

    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private float startBottom;
    private float time;
    private bool isShowing = false;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        startBottom = rect.offsetMin.y;   // отрицателна стойност
        canvasGroup.alpha = 0f;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        time = 0f;
        isShowing = true;
    }

    void Update()
    {
        if (!isShowing) return;

        time += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(time / slideDuration);

        // slide
        float newBottom = Mathf.Lerp(startBottom, targetBottom, t);
        rect.offsetMin = new Vector2(rect.offsetMin.x, newBottom);

        // fade
        canvasGroup.alpha = t;

        if (t >= 1f)
            isShowing = false;
    }
}

