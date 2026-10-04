// PanelConfigLaberinto.cs -- Elegir la dificultad sin salir de la escena.
//
// Se abre al entrar y se puede volver a abrir con C. Mientras esta abierto el
// ambiente no arranca el cronometro, de modo que elegir el nivel no cuenta como
// empezar el ensayo.
//
// Muestra las tres cifras que describen la dificultad de cada nivel: el tamano
// del mapa, la holgura que le queda a la bola dentro del pasillo, y la longitud
// del camino optimo. Las tres crecen o decrecen a la vez a lo largo de la
// escalera, que es lo que hace que el numero de nivel signifique algo.
using UnityEngine;
using UnityEngine.InputSystem;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A2 - Panel de dificultad")]
    public class PanelConfigLaberinto : MonoBehaviour
    {
        public Ambiente2Laberinto ambiente;

        private const string Instrucciones =
            "Lleva la bola del cuadro verde al rojo sin rozar los muros. Cuanto mas " +
            "alto es el nivel, mas estrecho es el pasillo y menos margen tiene la mano.";

        private GUIStyle titulo, chico, boton;
        private Texture2D fondo;
        private int nivelSel = 1;

        void Awake()
        {
            if (ambiente == null) ambiente = FindFirstObjectByType<Ambiente2Laberinto>();

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

        /// <summary>Deja la seleccion mostrada igual a la que el ambiente esta jugando
        /// de verdad. Se llama al abrir el panel para que no ensene una dificultad que
        /// no se llego a aplicar.</summary>
        private void Sincronizar()
        {
            nivelSel = ambiente.NumeroDeNivel;
        }

        void Update()
        {
            var t = Keyboard.current;
            if (ambiente == null || t == null || !t.cKey.wasPressedThisFrame) return;

            bool abrir = !ambiente.ConfigAbierta;
            // Al abrir, la seleccion vuelve a la dificultad realmente en juego: si el
            // panel se cerro con C sin pulsar "Empezar", lo elegido no se aplico y
            // ensenarlo marcado haria creer que si.
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
            boton = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };
        }

        void OnGUI()
        {
            if (ambiente == null || !ambiente.ConfigAbierta) return;
            var cfg = ambiente.Config;
            if (cfg == null) return;
            Estilos();

            int w = 560, h = 330;
            var caja = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.DrawTexture(caja, fondo);

            var r = new Rect(caja.x + 26, caja.y + 22, caja.width - 52, 28);
            GUI.Label(r, "LABERINTO DE PRECISION", titulo); r.y += 32;

            // El texto se envuelve solo, asi que su alto se pregunta en vez de
            // suponerse: con un rectangulo fijo la ultima linea se recortaria.
            float altoInstr = chico.CalcHeight(new GUIContent(Instrucciones), r.width);
            GUI.Label(new Rect(r.x, r.y, r.width, altoInstr), Instrucciones, chico);
            r.y += altoInstr + 10f;

            GUI.Label(r, "Nivel", chico); r.y += 20;

            int n = Mathf.Max(1, cfg.Cuantos);
            for (int i = 0; i < n; i++)
            {
                var rb = new Rect(r.x + i * ((r.width + 6) / n), r.y,
                                  (r.width - 6 * (n - 1)) / n, 28);
                bool sel = (i + 1) == nivelSel;
                var color = GUI.color;
                if (sel) GUI.color = new Color(0.6f, 0.9f, 1f);
                if (GUI.Button(rb, (i + 1) + " " + cfg.Nivel(i).nombre, boton)) nivelSel = i + 1;
                GUI.color = color;
            }
            r.y += 36;

            var elegido = cfg.Nivel(nivelSel - 1);
            float upm = ambiente.dispositivo != null ? ambiente.dispositivo.unitsPerMillimeter : 0.025f;
            float holgura = ConfiguracionLaberinto.HolguraU(elegido) / Mathf.Max(1e-5f, upm);
            var lab = Laberinto.Generar(elegido.columnas, elegido.filas,
                                        elegido.anchoPasillo, elegido.grosorMuro, elegido.semilla);
            var celdas = lab.Resolver(new Vector2Int(0, 0),
                                      new Vector2Int(lab.columnas - 1, lab.filas - 1));
            float largo = 0f;
            for (int k = 1; k < celdas.Count; k++)
                largo += Vector2.Distance(lab.CentroCelda(celdas[k].x, celdas[k].y),
                                          lab.CentroCelda(celdas[k - 1].x, celdas[k - 1].y));

            GUI.Label(r, "Mapa " + elegido.columnas + "x" + elegido.filas +
                         "   ·   holgura " + holgura.ToString("F2") + " mm por lado" +
                         "   ·   camino optimo " + largo.ToString("F1") + " u", chico);
            r.y += 26;

            if (GUI.Button(new Rect(r.x, r.y, 210, 30), "Empezar", boton))
            {
                ambiente.AplicarNivel(nivelSel);
                ambiente.ConfigAbierta = false;
            }
            GUI.Label(new Rect(r.x + 220, r.y + 6, r.width - 220, 22),
                      "C abre y cierra este panel", chico);
        }
    }
}
