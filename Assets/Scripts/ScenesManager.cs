using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenesManager : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("1 Juego");
    }

    public void CreateTrajectory()
    {
        SceneManager.LoadScene("0 Seleccionar usuario");
    }
}
