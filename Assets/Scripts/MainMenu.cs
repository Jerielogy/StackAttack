using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MainMenu : MonoBehaviour
{
    public InputField addressInput;
    NetworkManager networkManager;

    void Start()
    {
        networkManager = NetworkManager.singleton;
        if (addressInput != null) addressInput.text = "localhost";
    }

    public void HostGame() { if (!NetworkClient.active) networkManager.StartHost(); }
    public void JoinGame()
    {
        if (addressInput != null) networkManager.networkAddress = addressInput.text;
        networkManager.StartClient();
    }
    public void QuitGame() => Application.Quit();
}