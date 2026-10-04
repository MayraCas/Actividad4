// PanelConfigHanoi.cs -- Nivel y numero de discos, sin salir de la escena.
//
// Se abre al entrar y se puede volver a abrir con C. Mientras esta abierto el
// ambiente ignora el boton de agarre, de modo que elegir la dificultad no
// arranca el cronometro por accidente.
//
// Los niveles vienen de hanoi.json; el numero de discos se puede ajustar aparte
// dentro del rango 3-7, porque en la practica se acaba queriendo una corrida
// intermedia que el archivo no contempla.
using UnityEngine;
using UnityEngine.InputSystem;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A4 - Panel de configuracion")]
    public class PanelConfigHanoi : MonoBehaviour
    {
        public Ambiente4Hanoi ambiente;

        private const string Instrucciones =
            "Lleva los discos a la torre 3. Nunca apoyes uno grande sobre uno " +
            "pequeno: la mano sentira que no entra.";

        private GUIStyle titulo, chico, boton;
        private Texture2D fondo;
        private int nivelSel;
        private int discosSel = 3;

        void Awake()
        {
            if (ambiente == null) ambiente = FindFirstObjectByType<Ambiente4Hanoi>();

            fondo = new Texture2D(1, 1);
            fondo.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.12f, 0.94f));
            fondo.Apply();
            fondo.hideFlags = HideFlags.HideAndDontSave;
        }

        void Start()
        {
            if (ambiente == null) return;
            Sincronizar();
            ambiente.ConfigAbierta = true;
        }

        /// <summary>Deja la seleccion mostrada igual a la que el ambiente esta
        /// jugando de verdad. Se llama al abrir el panel para que no ensene una
        /// dificultad que no se llego a aplicar.</summary>
        private void Sincronizar()
        {
            nivelSel = ambiente.nivel;
            discosSel = Mathf.Clamp(ambiente.DiscosActuales,
                                    EstadoHanoi.MinDiscos, EstadoHanoi.MaxDiscos);
        }

        void Update()
        {
            var t = Keyboard.current;
            if (ambiente == null || t == null || !t.cKey.wasPressedThisFrame) return;

            bool abrir = !ambiente.ConfigAbierta;
            // Al abrir, la seleccion vuelve a la dificultad realmente en juego: si
            // el panel se cerro con C sin pulsar "Empezar", lo elegido no se aplico
            // y ensenarlo marcado haria creer que si.
            if (abrir) Sincronizar();
            ambiente.ConfigAbierta = abrir;
        }

        private void Estilos()
        {
            if (titulo != null) return;
            titulo = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            titulo.normal.textColor = new Color(0.60f, 0.88f, 1f);
            chico = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            chico.normal.textColor = new Color(0.72f, 0.78f, 0.88f);
            boton = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
        }

        void OnGUI()
        {
            if (ambiente == null || !ambiente.ConfigAbierta) return;
            Estilos();

            int w = 520, h = 300;
            var caja = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.DrawTexture(caja, fondo);

            var r = new Rect(caja.x + 26, caja.y + 22, caja.width - 52, 28);
            GUI.Label(r, "TORRES DE HANOI", titulo); r.y += 32;

            // El texto se envuelve solo, asi que su alto se pregunta en vez de
            // suponerse: con un rectangulo fijo la segunda linea se recortaba.
            float altoInstr = chico.CalcHeight(new GUIContent(Instrucciones), r.width);
            GUI.Label(new Rect(r.x, r.y, r.width, altoInstr), Instrucciones, chico);
            r.y += altoInstr + 10f;

            var cfg = ambiente.Config;
            int n = cfg.niveles != null ? cfg.niveles.Length : 0;
            GUI.Label(r, "Nivel", chico); r.y += 20;

            for (int i = 0; i < n; i++)
            {
                var rb = new Rect(r.x + i * ((r.width + 6) / Mathf.Max(1, n)), r.y,
                                  (r.width - 6 * (n - 1)) / Mathf.Max(1, n), 28);
                bool sel = i == nivelSel;
                string etiqueta = cfg.Nivel(i).nombre + " (" + cfg.Nivel(i).discos + ")";
                var color = GUI.color;
                if (sel) GUI.color = new Color(0.6f, 0.9f, 1f);
                if (GUI.Button(rb, etiqueta, boton))
                {
                    nivelSel = i;
                    discosSel = cfg.Nivel(i).discos;
                }
                GUI.color = color;
            }
            r.y += 36;

            GUI.Label(new Rect(r.x, r.y, 150, 24), "Discos: " + discosSel, chico);
            if (GUI.Button(new Rect(r.x + 150, r.y, 34, 24), "-", boton))
                discosSel = Mathf.Max(EstadoHanoi.MinDiscos, discosSel - 1);
            if (GUI.Button(new Rect(r.x + 190, r.y, 34, 24), "+", boton))
                discosSel = Mathf.Min(EstadoHanoi.MaxDiscos, discosSel + 1);
            GUI.Label(new Rect(r.x + 236, r.y, r.width - 236, 24),
                      "minimo " + ((1 << discosSel) - 1) + " movimientos", chico);
            r.y += 34;

            GUI.Label(r, cfg.Nivel(nivelSel).guiaPermitida
                         ? "Guia haptica disponible en este nivel (tecla G)."
                         : "Sin guia haptica en este nivel.", chico);
            r.y += 26;

            if (GUI.Button(new Rect(r.x, r.y, 210, 30), "Empezar", boton))
            {
                ambiente.AplicarNivel(nivelSel, discosSel);
                ambiente.ConfigAbierta = false;
            }
            GUI.Label(new Rect(r.x + 220, r.y + 6, r.width - 220, 22),
                      "C abre y cierra este panel", chico);
        }
    }
}
