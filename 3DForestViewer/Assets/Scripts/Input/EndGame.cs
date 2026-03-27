using UnityEngine;
using UnityEngine.InputSystem;

public class EndGame : MonoBehaviour
{
    private GameEnd controls;

    private void Awake()
    {
        controls = new GameEnd();

        // Quitアクションが押された瞬間に終了処理
        controls.Ending.Quit.performed += _ => QuitApp();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void QuitApp()
    {
        Debug.Log("Quit!");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
