using UnityEngine;

public class UserAndGameDetailsManager : MonoBehaviour
{
    // Един глобален обект (Singleton)
    // достъпен от всички сцени
    public static UserAndGameDetailsManager Instance { get; private set; }

    // Данни за логнатия потребител
    public UserInfoClass CurrentUser { get; private set; }

    // Данни за текущо избраното ниво
    public GameInfoClass CurrentGame { get; private set; }

    private void Awake()
    {
        // Ако вече има създаден instance – унищожаваме този
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        // Този обект става глобалният
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("SessionManager CREATED");
    }

    
    // USER
    

    // Задаване на логнат потребител (Login сцена)
    public void SetUser(UserInfoClass user)
    {
        CurrentUser = user;
    }

    // Проверка дали има логнат потребител
    public bool HasUser()
    {
        return CurrentUser != null;
    }

  
    // GAME
 
    // Задаване на избрано ниво (Home сцена)
    public void SetGame(int pictureId, int difficulty)
    {
        CurrentGame = new GameInfoClass
        {
            pictureId = pictureId,
            difficulty = difficulty
        };
    }

    // Изчистване на текущата игра (при връщане към Home)
    public void ClearGame()
    {
        CurrentGame = null;
    }

    // Проверка дали има активна игра
    public bool HasGame()
    {
        return CurrentGame != null;
    }
}
