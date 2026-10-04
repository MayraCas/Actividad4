// SelectorAmbientes.cs -- Menu y navegacion entre los tres ambientes.
//
// Las escenas son tres archivos independientes; este componente es lo unico
// que las une. Vive en la escena Menu y tambien escucha las teclas 1/2/3 desde
// cualquier ambiente, para poder saltar entre ellos sin volver al menu.
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/Selector de ambientes")]
    public class SelectorAmbientes : MonoBehaviour
    {
        public const string EscenaMenu = "Menu";
        public static readonly string[] Escenas =
        {
            "Ambiente1_Manipulacion",
            "Ambiente2_Laberinto",
            "Ambiente3_Fuerza",
        };
        public static readonly string[] Titulos =
        {
            "1 - MANIPULACION DE OBJETOS",
            "2 - PRECISION MOTRIZ",
            "3 - INTERACCION CON FUERZA",
        };
        public static readonly string[] Descripciones =
        {
            "Tomar, mover y soltar piezas de distinto peso en su receptaculo. " +
            "Se registra el numero de agarres, las caidas y el tiempo por pieza.",

            "Llevar la esfera por el laberinto sin tocar los muros. " +
            "Se evaluan el tiempo, los errores (choques) y la trayectoria.",

            "Mover cinco correderas con resistencias distintas: libre, viscosa, " +
            "friccion seca, resorte y resorte con tope. Se registran la fuerza " +
            "aplicada y el tiempo requerido.",
        };

        private GUIStyle titulo, texto, chico, boton;
        private Texture2D fondo;

        public static void Cargar(int indice)
        {
            if (indice < 0 || indice >= Escenas.Length) return;
            SceneManager.LoadScene(Escenas[indice]);
        }

        public static void IrAlMenu()
        {
            if (SceneManager.GetActiveScene().name != EscenaMenu)
                SceneManager.LoadScene(EscenaMenu);
        }

        /// <summary>Atajos globales 1/2/3 disponibles en cualquier escena.</summary>
        public static void LeerAtajosDeEscena()
        {
            var t = Keyboard.current;
            if (t == null) return;
            if (t.digit1Key.wasPressedThisFrame) Cargar(0);
            if (t.digit2Key.wasPressedThisFrame) Cargar(1);
            if (t.digit3Key.wasPressedThisFrame) Cargar(2);
        }

        void Update() { LeerAtajosDeEscena(); }

        void Awake()
        {
            fondo = new Texture2D(1, 1);
            fondo.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.12f, 0.92f));
            fondo.Apply();
            fondo.hideFlags = HideFlags.HideAndDontSave;
        }

        private void Estilos()
        {
            if (titulo != null) return;
            titulo = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
            titulo.normal.textColor = new Color(0.60f, 0.88f, 1f);
            texto = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            texto.normal.textColor = new Color(0.92f, 0.95f, 1f);
            chico = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            chico.normal.textColor = new Color(0.66f, 0.72f, 0.82f);
            boton = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold };
        }

        void OnGUI()
        {
            Estilos();
            int w = 620, h = 430;
            var caja = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.DrawTexture(caja, fondo);

            var r = new Rect(caja.x + 30, caja.y + 24, caja.width - 60, 34);
            GUI.Label(r, "ACTIVIDAD 4", titulo); r.y += 36;
            GUI.Label(r, "Desarrollo de ambientes virtuales simples", chico); r.y += 26;

            var dev = HapticDevice.Instance;
            string estado = dev == null ? "sin HapticDevice en la escena"
                          : dev.Connected ? "dispositivo conectado: " + dev.DeviceInfo
                                          : "modo simulacion (raton): sin hardware";
            GUI.Label(r, (dev != null && dev.Connected ? "● " : "○ ") + estado, chico); r.y += 30;

            for (int i = 0; i < Escenas.Length; i++)
            {
                var rb = new Rect(r.x, r.y, r.width, 34);
                if (GUI.Button(rb, Titulos[i], boton)) Cargar(i);
                r.y += 38;
                GUI.Label(new Rect(r.x + 8, r.y, r.width - 16, 40), Descripciones[i], chico);
                r.y += 44;
            }

            GUI.Label(new Rect(r.x, caja.yMax - 34, r.width, 20),
                      "Teclas 1/2/3 cambian de ambiente en cualquier momento · ESC vuelve aqui · " +
                      "R reinicia el ensayo · F activa o desactiva las fuerzas", chico);
        }
    }
}
