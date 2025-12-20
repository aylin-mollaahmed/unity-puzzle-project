using UnityEngine;
using UnityEngine.SceneManagement;

public class Lets_start_button : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("EntryPage"); 
    }
}