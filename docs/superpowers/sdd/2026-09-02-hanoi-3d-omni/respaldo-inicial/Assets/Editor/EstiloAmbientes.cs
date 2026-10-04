// EstiloAmbientes.cs -- Utilidades de construccion para los tres ambientes.
//
// Todo el aspecto de las escenas se genera aqui por codigo: materiales URP,
// texturas de rejilla y de ruido, iluminacion y post-proceso. No se descarga
// ningun asset externo, de modo que las escenas se pueden borrar y regenerar
// enteras desde el menu Haptics, y lo que se ve en el reporte es exactamente lo
// que produce este archivo.
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HapticOmni.EditorTools
{
    public static class EstiloAmbientes
    {
        public const string CarpetaMat = "Assets/Materials";
        public const string CarpetaTex = "Assets/Texturas";
        public const string CarpetaSet = "Assets/Settings";

        // ---- paleta comun -------------------------------------------------------
        public static readonly Color Fondo      = new Color(0.020f, 0.028f, 0.045f);
        public static readonly Color Cian       = new Color(0.35f, 0.85f, 1.00f);
        public static readonly Color Ambar      = new Color(1.00f, 0.75f, 0.25f);
        public static readonly Color Verde      = new Color(0.35f, 0.90f, 0.55f);
        public static readonly Color Rojo       = new Color(1.00f, 0.35f, 0.32f);
        public static readonly Color Violeta    = new Color(0.62f, 0.50f, 0.95f);
        public static readonly Color GrisMedio  = new Color(0.16f, 0.19f, 0.26f);
        public static readonly Color GrisClaro  = new Color(0.42f, 0.47f, 0.56f);

        public static readonly Color[] PaletaPiezas =
        {
            new Color(0.36f, 0.80f, 1.00f),   // cian
            new Color(0.45f, 0.90f, 0.55f),   // verde
            new Color(1.00f, 0.72f, 0.28f),   // ambar
            new Color(0.95f, 0.42f, 0.45f),   // rojo
            new Color(0.68f, 0.55f, 0.98f),   // violeta
        };

        // ---- carpetas -----------------------------------------------------------
        public static void AsegurarCarpetas()
        {
            if (!AssetDatabase.IsValidFolder(CarpetaMat)) AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(CarpetaTex)) AssetDatabase.CreateFolder("Assets", "Texturas");
            if (!AssetDatabase.IsValidFolder(CarpetaSet)) AssetDatabase.CreateFolder("Assets", "Settings");
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        // ---- materiales ---------------------------------------------------------
        private static Shader ShaderLit
        {
            get { return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"); }
        }

        public static Material Mat(string nombre, Color color, float emision = 0f,
                                   float suavidad = 0.35f, float metalico = 0f)
        {
            var m = new Material(ShaderLit) { name = nombre };
            m.color = color;
            if (m.HasProperty("_BaseColor"))  m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
            if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic", metalico);
            if (emision > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * emision);
            }
            return Guardar(m, nombre);
        }

        public static Material MatTexturizado(string nombre, Color color, Texture2D tex,
                                              Vector2 tiling, float emision = 0f, float suavidad = 0.2f)
        {
            var m = Mat(nombre, color, emision, suavidad);
            if (m.HasProperty("_BaseMap"))
            {
                m.SetTexture("_BaseMap", tex);
                m.SetTextureScale("_BaseMap", tiling);
            }
            else if (m.HasProperty("_MainTex"))
            {
                m.SetTexture("_MainTex", tex);
                m.SetTextureScale("_MainTex", tiling);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material MatTransparente(string nombre, Color color)
        {
            var m = new Material(ShaderLit) { name = nombre };
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.renderQueue = (int)RenderQueue.Transparent;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            return Guardar(m, nombre);
        }

        private static Material Guardar(Material m, string nombre)
        {
            string ruta = CarpetaMat + "/" + nombre + ".mat";
            var existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (existente != null)
            {
                existente.shader = m.shader;
                existente.CopyPropertiesFromMaterial(m);
                Object.DestroyImmediate(m);
                EditorUtility.SetDirty(existente);
                return existente;
            }
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        // ---- texturas procedurales ----------------------------------------------
        /// <summary>Rejilla: lineas finas cada 1/8 y gruesas en el borde. Da a la mesa
        /// y al tablero una referencia visual de escala sin descargar nada.
        ///
        /// Ojo con los valores: la textura MULTIPLICA al color base, asi que un fondo
        /// de 0.16 convierte cualquier gris en casi negro. Por eso hay dos variantes:
        /// la oscura para el tablero del laberinto (donde interesa el contraste con
        /// los muros) y la clara para las superficies que deben verse iluminadas.</summary>
        public static Texture2D TexturaRejilla(string nombre = "T_Rejilla", int lado = 256,
                                               int divisiones = 8, float fondo = 0.16f,
                                               float linea = 0.55f, float borde = 1.0f)
        {
            return TexturaCacheada(nombre, lado, (x, y) =>
            {
                int paso = lado / divisiones;
                bool fina   = (x % paso < 1) || (y % paso < 1);
                bool gruesa = (x < 2) || (y < 2) || (x >= lado - 2) || (y >= lado - 2);
                float v = gruesa ? borde : fina ? linea : fondo;
                return new Color(v, v, v, 1f);
            });
        }

        /// <summary>Variante de fondo claro: la rejilla se dibuja mas oscura que la
        /// superficie, como el papel milimetrado de un banco de trabajo.</summary>
        public static Texture2D TexturaRejillaClara()
        {
            return TexturaRejilla("T_RejillaClara", 256, 8, 0.92f, 0.60f, 0.45f);
        }

        /// <summary>Ruido fino para superficies asperas (la mesa, las correderas con
        /// friccion). Da textura visual coherente con lo que se siente al tacto.</summary>
        public static Texture2D TexturaRuido(string nombre = "T_Ruido", int lado = 256,
                                             float escala = 24f)
        {
            return TexturaCacheada(nombre, lado, (x, y) =>
            {
                float n = Mathf.PerlinNoise(x / (float)lado * escala, y / (float)lado * escala);
                float g = Mathf.Lerp(0.55f, 1.0f, n);
                return new Color(g, g, g, 1f);
            });
        }

        /// <summary>Degradado radial suave; sirve de vineta para los paneles de fondo.</summary>
        public static Texture2D TexturaDegradado(string nombre = "T_Degradado", int lado = 256)
        {
            return TexturaCacheada(nombre, lado, (x, y) =>
            {
                float dx = (x / (float)lado - 0.5f) * 2f;
                float dy = (y / (float)lado - 0.5f) * 2f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / 1.414f);
                float g = Mathf.Lerp(1f, 0.35f, d * d);
                return new Color(g, g, g, 1f);
            });
        }

        private static Texture2D TexturaCacheada(string nombre, int lado, System.Func<int, int, Color> f)
        {
            string ruta = CarpetaTex + "/" + nombre + ".png";
            var existente = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            if (existente != null) return existente;

            var t = new Texture2D(lado, lado, TextureFormat.RGBA32, true);
            var px = new Color[lado * lado];
            for (int y = 0; y < lado; y++)
                for (int x = 0; x < lado; x++)
                    px[y * lado + x] = f(x, y);
            t.SetPixels(px);
            t.Apply();

            File.WriteAllBytes(Path.GetFullPath(ruta), t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);

            var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            if (imp != null)
            {
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.filterMode = FilterMode.Bilinear;
                imp.anisoLevel = 8;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        // ---- primitivas ---------------------------------------------------------
        public static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre,
                                           Vector3 posLocal, Vector3 escala, Material mat,
                                           bool conCollider = false)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            if (padre != null) go.transform.SetParent(padre, false);
            go.transform.localPosition = posLocal;
            go.transform.localScale = escala;
            if (!conCollider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Fila de puntos luminosos que numera un elemento del 1 al 5. Es la
        /// alternativa a poner texto 3D: no depende de fuentes ni de TextMeshPro.</summary>
        public static GameObject Numeral(int n, string nombre, Transform padre, Vector3 posLocal,
                                         Color color, float tam = 0.045f, float separacion = 0.075f)
        {
            var raiz = new GameObject(nombre);
            raiz.transform.SetParent(padre, false);
            raiz.transform.localPosition = posLocal;
            var mat = Mat("M_Punto_" + ColorUtility.ToHtmlStringRGB(color), color, 1.0f);
            float x0 = -(n - 1) * separacion * 0.5f;
            for (int i = 0; i < n; i++)
                Primitiva(PrimitiveType.Sphere, "punto" + i, raiz.transform,
                          new Vector3(x0 + i * separacion, 0f, 0f), Vector3.one * tam, mat);
            return raiz;
        }

        // ---- camara, luces y post-proceso ---------------------------------------
        public static Camera Camara(Vector3 pos, Vector3 euler, float fov = 50f)
        {
            var go = new GameObject("Camara principal") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Fondo;
            cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            go.AddComponent<AudioListener>();

            var datos = go.GetComponent<UniversalAdditionalCameraData>();
            if (datos == null) datos = go.AddComponent<UniversalAdditionalCameraData>();
            datos.renderPostProcessing = true;
            datos.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            return cam;
        }

        public static void Iluminacion()
        {
            // Ambiente alto a proposito: las escenas son oscuras para que los
            // elementos emisivos destaquen, pero sin luz ambiental suficiente los
            // grises se hunden en el fondo y las capturas del reporte salen negras.
            RenderSettings.ambientMode      = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor  = new Color(0.26f, 0.31f, 0.42f);
            RenderSettings.ambientEquatorColor = new Color(0.17f, 0.20f, 0.27f);
            RenderSettings.ambientGroundColor  = new Color(0.07f, 0.08f, 0.12f);
            RenderSettings.fog = false;

            var key = new GameObject("Luz principal");
            var l1 = key.AddComponent<Light>();
            l1.type = LightType.Directional;
            l1.intensity = 1.5f;
            l1.color = new Color(1f, 0.96f, 0.90f);
            l1.shadows = LightShadows.Soft;
            l1.shadowStrength = 0.45f;      // sombras presentes pero no negras
            key.transform.rotation = Quaternion.Euler(46f, -30f, 0f);

            var fill = new GameObject("Luz de relleno");
            var l2 = fill.AddComponent<Light>();
            l2.type = LightType.Directional;
            l2.intensity = 0.55f;
            l2.color = new Color(0.45f, 0.62f, 1f);
            l2.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(-16f, 136f, 0f);

            var rim = new GameObject("Luz de contorno");
            var l3 = rim.AddComponent<Light>();
            l3.type = LightType.Directional;
            l3.intensity = 0.35f;
            l3.color = new Color(1f, 0.72f, 0.45f);
            l3.shadows = LightShadows.None;
            rim.transform.rotation = Quaternion.Euler(-8f, 205f, 0f);
        }

        /// <summary>Volumen global con bloom y vineta. El bloom es lo que hace que los
        /// materiales emisivos (cursor, meta, indicadores) destaquen sobre el fondo
        /// oscuro sin subir la iluminacion general.</summary>
        public static void PostProceso(string nombrePerfil)
        {
            var perfil = PerfilVolumen(nombrePerfil);
            var go = new GameObject("Post-proceso");
            var v = go.AddComponent<Volume>();
            v.isGlobal = true;
            v.priority = 1f;
            v.sharedProfile = perfil;
        }

        private static VolumeProfile PerfilVolumen(string nombre)
        {
            string ruta = CarpetaSet + "/" + nombre + ".asset";
            var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
            if (perfil != null)
            {
                // Se reconstruye entero: si solo se devolviera el existente, cambiar
                // los ajustes aqui no tendria ningun efecto al regenerar las escenas.
                for (int i = perfil.components.Count - 1; i >= 0; i--)
                    Object.DestroyImmediate(perfil.components[i], true);
                perfil.components.Clear();
            }
            else
            {
                perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(perfil, ruta);
            }

            // Umbral en 1.0: solo florece lo que ya esta sobreexpuesto (los
            // materiales emisivos). Con 0.85 tambien florecian los grises claros y
            // toda la escena salia lavada.
            var bloom = perfil.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.overrideState = true; bloom.threshold.value = 1.0f;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.45f;
            bloom.scatter.overrideState   = true; bloom.scatter.value   = 0.60f;

            var vig = perfil.Add<Vignette>(true);
            vig.active = true;
            vig.intensity.overrideState = true; vig.intensity.value = 0.24f;
            vig.smoothness.overrideState = true; vig.smoothness.value = 0.55f;

            var tono = perfil.Add<Tonemapping>(true);
            tono.active = true;
            tono.mode.overrideState = true; tono.mode.value = TonemappingMode.Neutral;

            // El tonemapper Neutral desatura; se compensa aqui para que los cuatro
            // colores de las piezas sigan distinguiendose entre si en la captura.
            var color = perfil.Add<ColorAdjustments>(true);
            color.active = true;
            color.postExposure.overrideState = true; color.postExposure.value = 0.25f;
            color.saturation.overrideState   = true; color.saturation.value   = 18f;
            color.contrast.overrideState     = true; color.contrast.value     = 8f;

            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();
            return perfil;
        }

        // ---- montaje comun del equipo haptico -----------------------------------
        /// <summary>Crea el objeto EspacioHaptico con el HapticDevice y el cursor.
        /// Es identico en los tres ambientes: el equipo y su mapeo mm -> unidades no
        /// cambian, solo lo hace el escenario que hay alrededor.</summary>
        public static HapticDevice EspacioHaptico(out GameObject cursor, float radioPunta,
                                                  Color colorCursor)
        {
            var raiz = new GameObject("EspacioHaptico");
            var dev = raiz.AddComponent<HapticDevice>();
            dev.unitsPerMillimeter = 0.025f;   // 160x120x70 mm -> 4.0 x 3.0 x 1.75 u
            dev.deviceCenterMm     = Vector3.zero;
            dev.probeRadius        = radioPunta;
            dev.contactDamping     = 0.0015f;
            dev.damping            = 0.0025f;
            dev.shapesEnabled      = true;
            dev.attractorsEnabled  = false;
            dev.floorEnabled       = false;

            var matCursor = Mat("M_Cursor", colorCursor, 1.0f, 0.6f);
            var matEstela = Mat("M_Estela", colorCursor * 0.9f, 0.9f);
            var matFlecha = Mat("M_Flecha", Ambar, 1.2f);

            cursor = Primitiva(PrimitiveType.Sphere, "Cursor (punta del stylus)", raiz.transform,
                               Vector3.zero, Vector3.one * Mathf.Max(0.06f, radioPunta * 2f), matCursor);

            var estela = cursor.AddComponent<TrailRenderer>();
            estela.time = 2.0f;
            estela.startWidth = Mathf.Max(0.03f, radioPunta * 0.7f);
            estela.endWidth = 0f;
            estela.sharedMaterial = matEstela;
            estela.numCapVertices = 4;
            estela.minVertexDistance = 0.02f;

            var flechaGO = new GameObject("Vector de fuerza");
            flechaGO.transform.SetParent(cursor.transform, false);
            var flecha = flechaGO.AddComponent<LineRenderer>();
            flecha.sharedMaterial = matFlecha;
            flecha.widthMultiplier = 0.03f;
            flecha.useWorldSpace = true;
            flecha.positionCount = 2;

            var hc = cursor.AddComponent<HapticCursor>();
            hc.estela = estela;
            hc.flechaFuerza = flecha;
            hc.colorLibre = colorCursor;

            return dev;
        }

        /// <summary>Marco de alambre del espacio util del dispositivo. Es la referencia
        /// que le dice al usuario hasta donde llega el brazo.</summary>
        public static void MarcoEspacioDeTrabajo(Transform padre, float upm, Material mat)
        {
            var go = new GameObject("Marco del espacio de trabajo");
            go.transform.SetParent(padre, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat;
            lr.widthMultiplier = 0.006f;   // referencia discreta: no debe robar la escena
            lr.useWorldSpace = false;
            lr.loop = false;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;

            Vector3 h = new Vector3(160f, 120f, 70f) * upm * 0.5f;
            Vector3 A = new Vector3(-h.x, -h.y, -h.z), B = new Vector3(h.x, -h.y, -h.z);
            Vector3 C = new Vector3( h.x,  h.y, -h.z), D = new Vector3(-h.x, h.y, -h.z);
            Vector3 E = new Vector3(-h.x, -h.y,  h.z), F = new Vector3(h.x, -h.y,  h.z);
            Vector3 G = new Vector3( h.x,  h.y,  h.z), H = new Vector3(-h.x, h.y,  h.z);

            Vector3[] camino = { A, B, C, D, A, E, F, G, H, E, H, D, C, G, F, B };
            lr.positionCount = camino.Length;
            lr.SetPositions(camino);
        }

        // ---- escenas ------------------------------------------------------------
        public static void RegistrarEscena(string ruta, int posicion = -1)
        {
            var lista = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (lista.Exists(s => s.path == ruta)) return;
            var nueva = new EditorBuildSettingsScene(ruta, true);
            if (posicion >= 0 && posicion <= lista.Count) lista.Insert(posicion, nueva);
            else lista.Add(nueva);
            EditorBuildSettings.scenes = lista.ToArray();
        }
    }
}
