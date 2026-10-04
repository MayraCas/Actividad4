// CapturarAmbientes.cs -- Exporta una imagen de cada escena para el reporte.
//
//   Haptics / Capturar imagenes de los ambientes
//
// Renderiza la camara principal de cada escena a un PNG en Capturas/ y, si
// existe la carpeta de imagenes del reporte LaTeX, deja alli una copia. Asi las
// figuras del documento se regeneran con un clic cuando cambia el escenario, en
// vez de ser capturas de pantalla sueltas que envejecen.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HapticOmni.EditorTools
{
    public static class CapturarAmbientes
    {
        private const int ANCHO = 1600;
        private const int ALTO  = 1000;

        [MenuItem("Haptics/Capturar imagenes de los ambientes", priority = 40)]
        public static void Capturar()
        {
            string destino = Path.GetFullPath(Path.Combine(Application.dataPath, "../Capturas"));
            Directory.CreateDirectory(destino);

            // Carpeta de imagenes del reporte, dos niveles por encima del proyecto.
            string reporte = Path.GetFullPath(Path.Combine(Application.dataPath, "../../src/imagenes"));
            bool hayReporte = Directory.Exists(reporte);

            string[] escenas = { "Menu", "Ambiente1_Manipulacion", "Ambiente2_Laberinto",
                                 "Ambiente3_Fuerza", "Ambiente4_Hanoi" };
            // Paralelo a `escenas`: el bucle indexa los dos con la misma i, asi que
            // anadir una escena sin anadir su salida seria un desbordamiento.
            string[] salidas = { "a4_menu", "a4_ambiente1", "a4_ambiente2",
                                 "a4_ambiente3", "a4_ambiente4" };

            string abierta = EditorSceneManager.GetActiveScene().path;

            for (int i = 0; i < escenas.Length; i++)
            {
                EditorSceneManager.OpenScene("Assets/Scenes/" + escenas[i] + ".unity");
                var cam = Camera.main;
                if (cam == null) { Debug.LogWarning("[Capturas] Sin camara en " + escenas[i]); continue; }

                byte[] png = Render(cam);
                string ruta = Path.Combine(destino, salidas[i] + ".png");
                File.WriteAllBytes(ruta, png);
                if (hayReporte) File.WriteAllBytes(Path.Combine(reporte, salidas[i] + ".png"), png);
                Debug.Log("[Capturas] " + ruta);
            }

            if (!string.IsNullOrEmpty(abierta)) EditorSceneManager.OpenScene(abierta);
            AssetDatabase.Refresh();
            Debug.Log("[Capturas] Listas en " + destino + (hayReporte ? "  (copiadas tambien a " + reporte + ")" : ""));
        }

        private static byte[] Render(Camera cam)
        {
            var rt = new RenderTexture(ANCHO, ALTO, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                useMipMap = false,
            };
            var anterior = cam.targetTexture;
            var activa   = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(ANCHO, ALTO, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, ANCHO, ALTO), 0, 0);
            tex.Apply();

            cam.targetTexture   = anterior;
            RenderTexture.active = activa;

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            return png;
        }
    }
}
