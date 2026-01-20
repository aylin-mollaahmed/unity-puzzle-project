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

    private bool isTransitioning = false;

    // Контролери при клик на някой от бутоните
    public void OpenLoginFromTitle()
    {
        if (isTransitioning)
        {
            return;
        }
        StartCoroutine(OpenPanelFromTitle(loginPanelGroup));
    }

    
    public void OpenRegistryFromLogin()
    {
        if (isTransitioning)
        {
            return;
        }
        StartCoroutine(SwitchPanels(loginPanelGroup, registryPanelGroup));
    }

    public void OpenLoginFromRegistry()
    {
        if (isTransitioning)
        {
            return;
        }
        StartCoroutine(SwitchPanels(registryPanelGroup, loginPanelGroup));
    }



  
    private IEnumerator OpenPanelFromTitle(CanvasGroup panel)
    {
        //Заключваме, за да не се стартира нова анимация преди да е приключила тази
        isTransitioning = true;


        //Да започне избледняване кадър по кадър
        yield return StartCoroutine(FadeCanvasGroup(photoRowGroup, 1f, 0f, 0.35f));
        yield return StartCoroutine(FadeCanvasGroup(startButtonGroup, 1f, 0f, 0.35f));


        //Редът със снимки и бутона за старт да не могат вече да се натискат и да станат неактивни
        SetGroupNotActiveAndNotClickable(photoRowGroup);
        SetGroupNotActiveAndNotClickable(startButtonGroup);

        //Прави панела активен и кликаем
        SetGroupActiveAndClickable(panel);

        //Показва панела кадър по кадър
        yield return StartCoroutine(FadeCanvasGroup(panel, 0f, 1f, 0.35f));

        // Освобождава за нови анимации
        isTransitioning = false;
    }

    //Смяна от един панел към друг
    private IEnumerator SwitchPanels(CanvasGroup from, CanvasGroup to)
    {
        //Заключваме, за да не се стартира нова анимация преди да е приключила тази
        isTransitioning = true;

        //Постепенно избледняване на текущия панел
        yield return StartCoroutine(FadeCanvasGroup(from, 1f, 0f, 0.35f));

        //Текущия панел става некликаем и неактивен
        SetGroupNotActiveAndNotClickable(from);

        //Прави новия панел ативен и кликаем
        SetGroupActiveAndClickable(to);

        //Показва бавно новия панел
        yield return StartCoroutine(FadeCanvasGroup(to, 0f, 1f, 0.35f));

        //Отваря за нови анимации
        isTransitioning = false;
    }

   
    // Помощни функции
    private void SetGroupActiveAndClickable(CanvasGroup panel)
    {
        if (panel == null)
        {
            return;
        }
        panel.gameObject.SetActive(true);
        panel.interactable = true;
        panel.blocksRaycasts = true;
    }
    private void SetGroupNotActiveAndNotClickable(CanvasGroup panel)
    {
        if (panel == null)
        {
            return;
        }
        panel.gameObject.SetActive(false);
        panel.interactable = false;
        panel.blocksRaycasts = false;
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

    
}
