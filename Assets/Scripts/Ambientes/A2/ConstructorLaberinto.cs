// ConstructorLaberinto.cs -- Levanta el tablero en tiempo de ejecucion.
//
// Antes esto lo hacia el generador de escena del editor y el laberinto quedaba
// cocido en el archivo .unity, de modo que la escena solo podia contener un
// nivel. Al traerlo a runtime, una sola escena sirve para los cinco y la
// geometria pasa a ser configurable desde laberinto.json.
//
// El detalle que decide el diseno: EstiloAmbientes vive en Assets/Editor y NO
// existe en runtime. Por eso esta clase no puede pedirle materiales; el armazon
// que construye el editor se los deja cableados en los campos de abajo. Esos
// materiales si son assets del proyecto (EstiloAmbientes.Mat termina en
// Guardar), asi que la referencia sobrevive a guardar y recargar la escena.
//
// Esta clase no sabe nada de reglas ni de metricas: construye, mide el camino
// optimo y devuelve las piezas. Quien juega es Ambiente2Laberinto.
using System.Collections.Generic;
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A2 - Constructor del laberinto")]
    public class ConstructorLaberinto : MonoBehaviour
    {
        [Header("Cableado por el generador de escena")]
        [Tooltip("Padre de todo lo que se destruye y se recrea al cambiar de nivel.")]
        public Transform raizTablero;
        public Camera camara;
        public HapticDevice dispositivo;
        [Tooltip("La bola es la propia punta del stylus: aqui solo se le ajusta el tamano.")]
        public Transform cursor;

        [Header("Materiales")]
        public Material matPlaca, matMuro, matSalida, matMeta;

        // ---- perfil en Z del tablero ---------------------------------------
        // Los muros son ALTOS a proposito. Lo que sujeta la bola en el plano del
        // tablero es la ranura, y una ranura es fuerza, no pared: el Touch satura
        // a ~3.3 N y la ranura tiene rigidez 0.22 N/mm, asi que tirando del brazo
        // se le pueden sacar a la punta ~15 mm reales -- unas 0.38 unidades con la
        // escala de estas escenas. Con muros bajos la bola se monta encima de las
        // paredes y se sale del laberinto.
        private const float ZTraseraMuro = 0.18f;
        private const float ZMarca       = -0.20f;
        private const float HuecoRanura  = 0.02f;

        public Laberinto Actual        { get; private set; }
        public Transform Salida        { get; private set; }
        public Transform Meta          { get; private set; }
        public HapticShape[] Muros     { get; private set; }
        public Vector3[] CaminoOptimo  { get; private set; }
        public float LongitudOptima    { get; private set; }

        /// <summary>Destruye el tablero anterior y levanta el del nivel dado.</summary>
        public void Construir(NivelLaberinto n, ConfiguracionLaberinto cfg)
        {
            if (n == null || cfg == null || raizTablero == null) return;

            Limpiar();

            Actual = Laberinto.Generar(n.columnas, n.filas, n.anchoPasillo, n.grosorMuro, n.semilla);

            float zCentroMuro = ZTraseraMuro - cfg.profundidadMuro * 0.5f;
            float zPlaca      = ZTraseraMuro + 0.03f;

            CrearPlaca(n, zPlaca);
            Muros = CrearMuros(n, cfg, zCentroMuro);
            CrearRanura(n, cfg);
            CrearMarcas(n);
            MedirCamino();
            AjustarBola(n);
            AjustarCamara(cfg);
        }

        /// <summary>Vacia el tablero. Las formas se apagan ANTES de destruirlas: si
        /// solo se llamase a Destroy, seguirian registradas en el dispositivo hasta
        /// el final del frame y durante ese frame convivirian los muros del nivel
        /// viejo con los del nuevo.</summary>
        private void Limpiar()
        {
            for (int i = raizTablero.childCount - 1; i >= 0; i--)
            {
                var hijo = raizTablero.GetChild(i);
                foreach (var f in hijo.GetComponentsInChildren<HapticShape>(true))
                    f.enabled = false;
                hijo.SetParent(null, false);   // sale del arbol ya, no al final del frame
                if (Application.isPlaying) Destroy(hijo.gameObject);
                else DestroyImmediate(hijo.gameObject);
            }
            Muros = new HapticShape[0];
            Salida = Meta = null;
            CaminoOptimo = new Vector3[0];
            LongitudOptima = 1f;
        }

        private GameObject Caja(string nombre, Vector3 pos, Vector3 escala, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                // No hay fisica en este ambiente: la bola es la punta del stylus y
                // quien la detiene es el servo loop, no un collider.
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }
            go.transform.SetParent(raizTablero, false);
            go.transform.localPosition = pos;
            go.transform.localScale = escala;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private void CrearPlaca(NivelLaberinto n, float zPlaca)
        {
            var placa = Caja("Placa de fondo", new Vector3(0f, 0f, zPlaca),
                             new Vector3(Actual.AnchoTablero + 0.12f,
                                         Actual.AltoTablero + 0.12f, 0.06f), matPlaca);

            // El tiling depende del nivel, y matPlaca es un ASSET compartido:
            // escribirselo encima ensuciaria un archivo del proyecto cada vez que se
            // juega. Va por MaterialPropertyBlock, el mismo patron que HapticShape
            // usa para el color de contacto.
            var rend = placa.GetComponent<Renderer>();
            var bloque = new MaterialPropertyBlock();
            rend.GetPropertyBlock(bloque);
            bloque.SetVector("_BaseMap_ST", new Vector4(n.columnas, n.filas, 0f, 0f));
            bloque.SetVector("_MainTex_ST", new Vector4(n.columnas, n.filas, 0f, 0f));
            rend.SetPropertyBlock(bloque);
        }

        private HapticShape[] CrearMuros(NivelLaberinto n, ConfiguracionLaberinto cfg, float zCentro)
        {
            var barras = Actual.Barras();
            var formas = new List<HapticShape>(barras.Count);
            for (int i = 0; i < barras.Count; i++)
            {
                var b = barras[i];
                var go = Caja("Muro " + i,
                              new Vector3(b.centro.x, b.centro.y, zCentro),
                              new Vector3(b.tamano.x, b.tamano.y, cfg.profundidadMuro), matMuro);
                var f = go.AddComponent<HapticShape>();
                f.tipo = TipoForma.Caja;
                f.rigidez = cfg.rigidezMuro;
                f.friccion = cfg.friccionMuro;
                f.colorContacto = new Color(1f, 0.45f, 0.35f);
                formas.Add(f);
            }
            return formas.ToArray();
        }

        /// <summary>Dos semiespacios enfrentados que mantienen la bola en el plano del
        /// tablero: puede rodar por el laberinto pero no salirse hacia el frente ni
        /// hundirse en la placa.</summary>
        private void CrearRanura(NivelLaberinto n, ConfiguracionLaberinto cfg)
        {
            float sep = n.radioBola + HuecoRanura;
            Semiespacio("Fondo de la ranura", new Vector3(0, 0, -sep), Vector3.forward, cfg);
            Semiespacio("Tapa de la ranura",  new Vector3(0, 0,  sep), Vector3.back,    cfg);
        }

        private void Semiespacio(string nombre, Vector3 pos, Vector3 normal, ConfiguracionLaberinto cfg)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(raizTablero, false);
            go.transform.localPosition = pos;
            var f = go.AddComponent<HapticShape>();
            f.tipo = TipoForma.Semiespacio;
            f.normalLocal = normal;
            f.rigidez = cfg.rigidezRanura;
            f.friccion = 0f;
            f.prioritaria = true;   // siempre se envia: es la superficie de referencia
            f.colorContacto = new Color(0, 0, 0, 0);
        }

        /// <summary>Salida y meta. Van adelantadas dentro del canal (ZMarca) porque al
        /// fondo del pasillo los muros altos las taparian justo en las esquinas. Su Z
        /// no afecta a la logica: las distancias se miden en el plano.</summary>
        private void CrearMarcas(NivelLaberinto n)
        {
            Vector2 cIni = Actual.CentroCelda(0, 0);
            Vector2 cFin = Actual.CentroCelda(Actual.columnas - 1, Actual.filas - 1);
            float lado = n.anchoPasillo * 0.8f;

            Salida = Caja("Salida", new Vector3(cIni.x, cIni.y, ZMarca),
                          new Vector3(lado, lado, 0.02f), matSalida).transform;
            Meta   = Caja("Meta",   new Vector3(cFin.x, cFin.y, ZMarca),
                          new Vector3(lado, lado, 0.02f), matMeta).transform;
        }

        private void MedirCamino()
        {
            var celdas = Actual.Resolver(new Vector2Int(0, 0),
                                         new Vector2Int(Actual.columnas - 1, Actual.filas - 1));
            var camino = new Vector3[celdas.Count];
            float largo = 0f;
            for (int i = 0; i < celdas.Count; i++)
            {
                Vector2 c = Actual.CentroCelda(celdas[i].x, celdas[i].y);
                camino[i] = raizTablero.TransformPoint(new Vector3(c.x, c.y, 0f));
                if (i > 0) largo += Vector3.Distance(camino[i], camino[i - 1]);
            }
            CaminoOptimo = camino;
            LongitudOptima = Mathf.Max(1e-4f, largo);
        }

        private void AjustarBola(NivelLaberinto n)
        {
            if (dispositivo != null) dispositivo.probeRadius = n.radioBola;
            if (cursor != null) cursor.localScale = Vector3.one * Mathf.Max(0.06f, n.radioBola * 2f);
        }

        /// <summary>Coloca la camara para que el tablero quepa con el margen pedido.
        /// Se comprueban los DOS ejes y se toma el que obligue a alejarse mas: con una
        /// ventana estrecha manda el ancho, y con una apaisada manda el alto.</summary>
        private void AjustarCamara(ConfiguracionLaberinto cfg)
        {
            if (camara == null) return;
            float mediaV = Mathf.Tan(camara.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float aspecto = Mathf.Max(0.1f, camara.aspect);
            float mediaH = mediaV * aspecto;

            float zV = (Actual.AltoTablero  * 0.5f * cfg.margenCamara) / Mathf.Max(1e-4f, mediaV);
            float zH = (Actual.AnchoTablero * 0.5f * cfg.margenCamara) / Mathf.Max(1e-4f, mediaH);

            camara.transform.position = new Vector3(0f, 0f, -Mathf.Max(zV, zH));
            camara.transform.rotation = Quaternion.identity;
        }
    }
}
