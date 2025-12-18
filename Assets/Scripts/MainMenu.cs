using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MainMenu : MonoBehaviour
{
    [Header("UI")]
    public InputField addressInput;

    NetworkManager networkManager;

    void Start()
    {
        networkManager = NetworkManager.singleton;

        if (addressInput != null)
            addressInput.text = "localhost"; // default for LAN
    }

    // HOST BUTTON
    public void HostGame()
    {
        if (!NetworkServer.active && !NetworkClient.active)
        {
            networkManager.StartHost();
        }
    }

    // JOIN BUTTON
    public void JoinGame()
    {
        if (addressInput != null)
            networkManager.networkAddress = addressInput.text;

        if (!NetworkClient.active)
        {
            networkManager.StartClient();
        }
    }

    // QUIT BUTTON
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit");
    }
}
