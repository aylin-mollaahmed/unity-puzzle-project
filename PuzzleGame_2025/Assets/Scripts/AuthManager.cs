using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
    //Конфигурация, от отвън ще се получават  полетата за input и полето в което ще се изписва съобщението
    [Header("Login UI")]
    [SerializeField] private TMP_InputField loginUsername;
    [SerializeField] private TMP_InputField loginPassword;

    [Header("Register UI")]
    [SerializeField] private TMP_InputField regUsername;
    [SerializeField] private TMP_InputField regPassword;

    [Header("Feedback")]
    [SerializeField] private TMP_Text feedbackText;

    private XmlUserRepository repo;

    private void Awake()
    {
        
        repo = new XmlUserRepository();

        //Изчиства ако е имало някакво съобщение за грешка
        SetMessage("");
    }

    //Контролери
    public void OnRegisterClicked()
    {
       
        var msg = "";
        var wasRegistryOk = repo.TryRegister(regUsername.text, regPassword.text,  ref msg);
        SetMessage(msg);

        if (wasRegistryOk)
        {
            regUsername.text = "";
            regPassword.text = "";
        }
    }

    public void OnLoginClicked()
    {
      
        if (loginUsername.text.Length == 0 || loginPassword.text.Length == 0)
        {
            SetMessage("Моля, въведи име и парола.");
            return;
        }

        if (repo.ValidateLogin(loginUsername.text, loginPassword.text))
        {
            SetMessage("Успешен вход!");
            UserInfoClass currentUser = repo.LoadUserInfo(loginUsername.text);
            UserAndGameDetailsManager.Instance.SetUser(currentUser);
            SceneManager.LoadScene("HomePage");
        }
        else
        {
            SetMessage("Грешно потребителско име или парола.");
        }
    }

    private void SetMessage(string msg)
    {
        if (feedbackText != null)
        {
            feedbackText.text = msg;
        }
    }
}
