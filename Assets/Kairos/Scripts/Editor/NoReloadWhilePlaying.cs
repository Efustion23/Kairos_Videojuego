using UnityEditor;

namespace Kairos.EditorTools
{
    /// Si se guarda un script con el juego en Play, Unity recarga el dominio en caliente: los estáticos
    /// (InfiltrationController.I) y lo no serializable (rutas, barras del HUD) quedan en null y la consola
    /// se llena de NullReferenceException. Aquí la recarga espera a que termine la partida.
    [InitializeOnLoad]
    static class NoReloadWhilePlaying
    {
        static bool locked;

        static NoReloadWhilePlaying() => EditorApplication.playModeStateChanged += OnPlayMode;

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && !locked) { EditorApplication.LockReloadAssemblies(); locked = true; }
            else if (change == PlayModeStateChange.ExitingPlayMode && locked) { EditorApplication.UnlockReloadAssemblies(); locked = false; }
        }
    }
}
