using UnityEngine;

public class SampleController : MonoBehaviour
{
    [SerializeField]
    private string message = "Hello from the FUnity sample!";

    private void Start()
    {
        Debug.Log(message);
    }
}
