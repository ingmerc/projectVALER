using UnityEngine;
using UnityEngine.SceneManagement;

public class CambioScena : MonoBehaviour
{

    // Metodo chiamato quando il pulsante viene premuto
    public void OnPulsantePremuto()
    {
        // Cambia la scena
        SceneManager.LoadScene(1);
    }
}
