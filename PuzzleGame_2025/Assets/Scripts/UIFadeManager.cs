using System.Collections;
using UnityEngine;

public class UIFadeManager : MonoBehaviour
{
    //Конфигурация и връзка с Inspector
    [Header("Title objects")]
    [SerializeField] private CanvasGroup photoRowGroup;
    [SerializeField] private CanvasGroup startButtonGroup; 

    [Header("Panels")]
    [SerializeField] private CanvasGroup loginPanelGroup;
    [SerializeField] private CanvasGroup registryPanelGroup;

    [Header("Timing")]
    [SerializeField] private float fadeOutTime = 0.35f;
    [SerializeField] private float fadeInTime = 0.35f;

    private bool isTransitioning = false;

    // Контролери
    public void OpenLoginFromTitle()
    {
        if (isTransitioning) return;
        StartCoroutine(OpenPanelFromTitle(loginPanelGroup));
    }

    
    public void OpenRegistryFromLogin()
    {
        if (isTransitioning) return;
        StartCoroutine(SwitchPanels(loginPanelGroup, registryPanelGroup));
    }

    public void OpenLoginFromRegistry()
    {
        if (isTransitioning) return;
        StartCoroutine(SwitchPanels(registryPanelGroup, loginPanelGroup));
    }



    //Корутини (Coroutines) 
    private IEnumerator OpenPanelFromTitle(CanvasGroup panel)
    {
        //Заключвам, за да не се стартира нова анимация 
        isTransitioning = true;
        //Редът със снимки и бутона за старт да не могат вече да се натискат
        SetGroupInteractable(photoRowGroup, false);
        SetGroupInteractable(startButtonGroup, false);
        //Да започне избледняване кадър по кадър, като първо избледнява редът със снимки и когато е готов - бутона
        yield return StartCoroutine(FadeCanvasGroup(photoRowGroup, 1f, 0f, fadeOutTime));
        yield return StartCoroutine(FadeCanvasGroup(startButtonGroup, 1f, 0f, fadeOutTime));
        //
        if (photoRowGroup != null) {

            photoRowGroup.gameObject.SetActive(false);

        }
        if (startButtonGroup != null) {

            startButtonGroup.gameObject.SetActive(false);
        }
        
        //Прави панела активен и кликаем
        SetPanelActiveAndClickable(panel);
        //Показва панела 
        yield return StartCoroutine(FadeCanvasGroup(panel, 0f, 1f, fadeInTime));
        // Освобождава за нови анимации
        isTransitioning = false;
    }
    //Смяна от един панел към друг
    private IEnumerator SwitchPanels(CanvasGroup from, CanvasGroup to)
    {
        //Затваря за бъдещи анимации
        isTransitioning = true;

        //Забранява кликането на стария панел
        SetGroupInteractable(from, false);

        //Постепенно го избледнява
        yield return StartCoroutine(FadeCanvasGroup(from, 1f, 0f, fadeOutTime));

        //Прави стария панел неактивен
        SetPanelNotActive(from);

        //Прави новия панел ативен и кликаем
        SetPanelActiveAndClickable(to);

        //Показва бавно новия панел
        yield return StartCoroutine(FadeCanvasGroup(to, 0f, 1f, fadeInTime));

        //Отваря за нови анимации
        isTransitioning = false;
    }

   
    // Помощни функции
    private void SetPanelActiveAndClickable(CanvasGroup cg)
    {
        if (cg == null) 
            return;
        cg.gameObject.SetActive(true);
        SetGroupInteractable(cg, true);
    }

    private void SetPanelNotActive(CanvasGroup cg)
    {
        if (cg == null) 
            return;
        cg.gameObject.SetActive(false);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) 
            yield break;
        float t = 0f;
        cg.alpha = from;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k;
            if (duration <=0) {

                k = 1f;

            } else {

                k = t / duration;

            }
            //Интерполация за постепенност
            cg.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }
        cg.alpha = to;
    }

    private void SetGroupInteractable(CanvasGroup cg, bool value)
    {
        if (cg == null) 
            return;
        cg.interactable = value;
        cg.blocksRaycasts = value;
    }
}
