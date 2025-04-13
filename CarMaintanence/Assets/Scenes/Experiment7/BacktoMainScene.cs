using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;


public class BacktoMainScene : MonoBehaviour
{
    [Header("Action Binding")]
    public InputAction actionReference;
    // Assign via Inspector
    public string sceneName; // Scene to load

   

    private void OnEnable()
    {
        if (actionReference != null)
        {
            actionReference.performed += OnButtonPressed;
            actionReference.Enable();
        }
    }

    private void OnDisable()
    {
        if (actionReference != null)
        {
            actionReference.performed -= OnButtonPressed;
            actionReference.Disable();
        }
    }

    private void OnButtonPressed(InputAction.CallbackContext context)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Scene name is not set!");
        }
    }
}
