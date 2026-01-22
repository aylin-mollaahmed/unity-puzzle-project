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
       
    }

    //Контролери
    public void OnRegisterClicked()
    {

        if (regUsername.text.Length == 0 || loginPassword.text.Length == 0)
        {
            SetMessage("Please enter a username and password.");
            return;
        }
        //Ако е имало съобщения от логин-а да се махнат
        SetMessage("");
        var msg = "";
        var wasRegistryOk = repo.TryRegister(regUsername.text, regPassword.text,  ref msg);
        SetMessage(msg);

        //Ако регистрацията е успешна да се затрият полета, ако не е, невалидните данни да останат
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
            SetMessage("Please enter a username and password.");
            return;
        }

        if (repo.ValidateLogin(loginUsername.text, loginPassword.text))
        {
            SetMessage("Login successful!");
            UserInfoClass currentUser = repo.LoadUserInfo(loginUsername.text);
            UserAndGameDetailsManager.Instance.SetUser(currentUser);
            SceneManager.LoadScene("HomePage");
        }
        else
        {
           loginUsername.text = "";
           loginPassword.text = "";
            SetMessage("Invalid username or password.");
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
