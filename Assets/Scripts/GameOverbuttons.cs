using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections;

public class GameOverButtons : MonoBehaviour
{
    public GameObject restartButton;

    void Start()
    {
        if (restartButton != null)
            restartButton.SetActive(false);
    }

    public void EnableRestartButton()
    {
        if (restartButton != null)
            restartButton.SetActive(true);
    }

    // ---------------- RESTART GAME ----------------
    public void RestartGame()
    {
        StartCoroutine(RestartRoutine());
    }

    IEnumerator RestartRoutine()
    {
        // Stop network session if host or client
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }

        // Wait one frame to ensure Mirror cleans up objects
        yield return null;
        yield return new WaitForSeconds(0.1f);

        // Load the game scene
        SceneManager.LoadScene("MainScene");

        // Optional: if using Mirror's auto spawn, ensure NetworkManager spawns objects again
        // NetworkManager.singleton.ServerChangeScene("MainScene"); // alternative
    }

    // ---------------- BACK TO MAIN MENU ----------------
    public void BackToMainMenu()
    {
        StartCoroutine(BackToMenuRoutine());
    }

    IEnumerator BackToMenuRoutine()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();

        yield return null;
        yield return new WaitForSeconds(0.1f);
        SceneManager.LoadScene("MainMenu");
    }
}
