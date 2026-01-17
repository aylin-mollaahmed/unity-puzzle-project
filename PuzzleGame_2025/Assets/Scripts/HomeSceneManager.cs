using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class HomeSceneManager : MonoBehaviour
{
    [Header("User UI")]
    public TMP_Text usernameText;
    public TMP_Text pointsText;

    [Header("Difficulty UI")]
    public TMP_Dropdown difficultyDropdown;

    [Header("Message UI")]
    public TMP_Text messageText;

    [Header("Message Placement")]
    public RectTransform messageRect;          // drag RectTransform-а на Text_Message
    public float messageVisibleSeconds = 2.5f; // време за четене

    [Header("Scenes")]
    public string gameSceneName = "GameScene";

    private int selectedPictureId = -1;
    private int maxUnlockedForSelectedPicture = 1;

    private Coroutine hideMsgCo;

    private void Start()
    {
        if (difficultyDropdown != null)
        {
            difficultyDropdown.onValueChanged.RemoveAllListeners();
            difficultyDropdown.onValueChanged.AddListener(OnDifficultySelected);
        }

        TryLoadUserUI();

        if (UserAndGameDetailsManager.Instance != null)
            UserAndGameDetailsManager.Instance.ClearGame();

        ClearMessage();
    }

    private void TryLoadUserUI()
    {
        if (UserAndGameDetailsManager.Instance == null) return;
        if (!UserAndGameDetailsManager.Instance.HasUser()) return;

        UserInfoClass user = UserAndGameDetailsManager.Instance.CurrentUser;

        if (usernameText != null)
        {
            usernameText.text = "Username: " + user.username;
        }
        if (pointsText != null)
        {
            pointsText.text = "Points: " + user.totalPoints;
        }
    }

    public void OnPictureClicked(int pictureId)
    {
        if (UserAndGameDetailsManager.Instance == null || !UserAndGameDetailsManager.Instance.HasUser())
        {
            ShowMessage("Няма логнат потребител. Влез от Login/Register.");
            return;
        }

        selectedPictureId = pictureId;

        UserInfoClass user = UserAndGameDetailsManager.Instance.CurrentUser;
        maxUnlockedForSelectedPicture = GetMaxUnlockedDifficulty(user, selectedPictureId);

        // Връщаме dropdown-а на празната опция "Избери ниво…"
        if (difficultyDropdown != null)
        {
            difficultyDropdown.SetValueWithoutNotify(0);
            difficultyDropdown.RefreshShownValue();
        }

        ClearMessage();
    }

    private void OnDifficultySelected(int dropdownIndex)
    {
        // 0 = "Избери ниво..." -> не правим нищо и НЕ показваме съобщение
        if (dropdownIndex == 0)
        {
            ClearMessage();
            return;
        }

        ClearMessage();

        if (UserAndGameDetailsManager.Instance == null || !UserAndGameDetailsManager.Instance.HasUser())
        {
            ShowMessage("Няма логнат потребител. Влез от Login/Register.");
            return;
        }

        if (selectedPictureId < 1)
        {
            ShowMessage("Първо избери картинка.");
            return;
        }

        // защото index 1 = Level 1, index 2 = Level 2 ...
        int chosenDifficulty = dropdownIndex;

        // заключено
        if (chosenDifficulty > maxUnlockedForSelectedPicture)
        {
            ShowMessage($"Ниво {chosenDifficulty} е заключено. Първо реши предишните нива.");
            return;
        }

        // отключено -> директно игра
        UserAndGameDetailsManager.Instance.SetGame(selectedPictureId, chosenDifficulty);
        SceneManager.LoadScene(gameSceneName);
    }

    private int GetMaxUnlockedDifficulty(UserInfoClass user, int pictureId)
    {
        if (user == null) return 1;

        if (user.unlockedUpTo != null &&
            user.unlockedUpTo.TryGetValue(pictureId.ToString(), out string val) &&
            int.TryParse(val, out int diff))
        {
            return Mathf.Clamp(diff, 1, 4);
        }

        return 1;
    }

    private void ShowMessage(string msg)
    {
        if (messageText == null) return;

        messageText.text = msg;
        PositionMessageNextToDropdown();

        if (hideMsgCo != null) StopCoroutine(hideMsgCo);
        hideMsgCo = StartCoroutine(HideMessageAfter(messageVisibleSeconds));
    }

    private void ClearMessage()
    {
        if (hideMsgCo != null)
        {
            StopCoroutine(hideMsgCo);
            hideMsgCo = null;
        }

        if (messageText != null)
            messageText.text = "";
    }

    private IEnumerator HideMessageAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (messageText != null)
            messageText.text = "";
    }

    private void PositionMessageNextToDropdown()
    {
        if (difficultyDropdown == null) return;
        if (messageRect == null) return;

        RectTransform ddRect = difficultyDropdown.GetComponent<RectTransform>();
        if (ddRect == null) return;

        Vector3[] corners = new Vector3[4];
        ddRect.GetWorldCorners(corners);

        Vector3 leftMid = (corners[0] + corners[1]) * 0.5f;
        Vector3 rightMid = (corners[2] + corners[3]) * 0.5f;

        // 1,2,3 -> съобщението вдясно
        // 4 -> съобщението вляво
        bool placeLeft = (selectedPictureId == 4);

        float padding = 16f;

        if (!placeLeft)
        {
            messageRect.pivot = new Vector2(0f, 0.5f);
            messageRect.position = rightMid + new Vector3(padding, 0f, 0f);
        }
        else
        {
            messageRect.pivot = new Vector2(1f, 0.5f);
            messageRect.position = leftMid + new Vector3(-padding, 0f, 0f);
        }
    }
}
