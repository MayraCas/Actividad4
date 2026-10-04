# Paquete de revision - Tarea 3

## Que cambio
```
Files C:/Users/76905/Downloads/ReportesOMNI/Actividad4/docs/superpowers/sdd/2026-09-02-hanoi-3d-omni/task-3-antes/Assets/Scripts/Ambientes/A1/Ambiente1Manipulacion.cs and Assets/Scripts/Ambientes/A1/Ambiente1Manipulacion.cs differ
Files C:/Users/76905/Downloads/ReportesOMNI/Actividad4/docs/superpowers/sdd/2026-09-02-hanoi-3d-omni/task-3-antes/Assets/Scripts/Ambientes/A2/Ambiente2Laberinto.cs and Assets/Scripts/Ambientes/A2/Ambiente2Laberinto.cs differ
Files C:/Users/76905/Downloads/ReportesOMNI/Actividad4/docs/superpowers/sdd/2026-09-02-hanoi-3d-omni/task-3-antes/Assets/Scripts/Ambientes/A3/Ambiente3Fuerza.cs and Assets/Scripts/Ambientes/A3/Ambiente3Fuerza.cs differ
Files C:/Users/76905/Downloads/ReportesOMNI/Actividad4/docs/superpowers/sdd/2026-09-02-hanoi-3d-omni/task-3-antes/Assets/Scripts/Ambientes/AmbienteBase.cs and Assets/Scripts/Ambientes/AmbienteBase.cs differ
```

## Diff completo
```diff
diff -ru -x '*.meta' C:/Users/76905/Downloads/ReportesOMNI/Actividad4/docs/superpowers/sdd/2026-09-02-hanoi-3d-omni/task-3-antes/Assets/Scripts/Ambientes/A1/Ambiente1Manipulacion.cs Assets/Scripts/Ambientes/A1/Ambiente1Manipulacion.cs
--- C:/Users/76905/Downloads/ReportesOMNI/Actividad4/docs/superpowers/sdd/2026-09-02-hanoi-3d-omni/task-3-antes/Assets/Scripts/Ambientes/A1/Ambiente1Manipulacion.cs	2026-09-02 16:28:42.450730500 -0600
+++ Assets/Scripts/Ambientes/A1/Ambiente1Manipulacion.cs	2026-09-02 16:29:53.523755500 -0600
@@ -72,7 +72,8 @@
         {
             get
             {
-                return "fecha,guia,tiempo_s,piezas,agarres,caidas,recorrido_u," +
+                return CabeceraSujeto +
+                       ",fecha,guia,tiempo_s,piezas,agarres,caidas,recorrido_u," +
                        "impulso_ns,fuerza_pico_n,err_medio_u,detalle_por_pieza";
             }
         }
@@ -271,7 +272,7 @@
                 }
             }
 
-            resumen.Fila(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
+            FilaResumen(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                          guiaActiva ? "con_guia" : "sin_guia",
                          Tiempo,
                          piezas != null ? piezas.Length : 0,
diff -ru -x '*.meta' C:/Users/76905/Downloads/ReportesOMNI/Actividad4/docs/superpowers/sdd/2026-09-02-hanoi-3d-omni/task-3-antes/Assets/Scripts/Ambientes/A2/Ambiente2Laberinto.cs Assets/Scripts/Ambientes/A2/Ambiente2Laberinto.cs
--- C:/Users/76905/Downloads/ReportesOMNI/Actividad4/docs/superpowers/sdd/2026-09-02-hanoi-3d-omni/task-3-antes/Assets/Scripts/Ambientes/A2/Ambiente2Laberinto.cs	2026-09-02 16:28:42.445045600 -0600
+++ Assets/Scripts/Ambientes/A2/Ambiente2Laberinto.cs	2026-09-02 16:30:02.856847800 -0600
@@ -73,7 +73,8 @@
         {
             get
             {
-                return "fecha,nivel,semilla,tiempo_s,choques,t_contacto_s,f_choque_pico_n," +
+                return CabeceraSujeto +
+                       ",fecha,nivel,semilla,tiempo_s,choques,t_contacto_s,f_choque_pico_n," +
                        "recorrido_u,optimo_u,eficiencia,desv_rms_u,desv_max_u";
             }
         }
@@ -208,7 +209,7 @@
 
         protected override void EscribirResumen()
         {
-            resumen.Fila(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
+            FilaResumen(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                          nivel, semilla, Tiempo, choques, tiempoEnContacto, fuerzaChoquePico,
                          recorrido, longitudOptima, Eficiencia, DesviacionRms, desviacionMax);
         }
diff -ru -x '*.meta' C:/Users/76905/Downloads/ReportesOMNI/Actividad4/docs/superpowers/sdd/2026-09-02-hanoi-3d-omni/task-3-antes/Assets/Scripts/Ambientes/A3/Ambiente3Fuerza.cs Assets/Scripts/Ambientes/A3/Ambiente3Fuerza.cs
--- C:/Users/76905/Downloads/ReportesOMNI/Actividad4/docs/superpowers/sdd/2026-09-02-hanoi-3d-omni/task-3-antes/Assets/Scripts/Ambientes/A3/Ambiente3Fuerza.cs	2026-09-02 16:28:42.433909900 -0600
+++ Assets/Scripts/Ambientes/A3/Ambiente3Fuerza.cs	2026-09-02 16:30:12.387109000 -0600
@@ -57,7 +57,8 @@
         {
             get
             {
-                return "fecha,estacion,nombre,k_nmm,b_nmms,fs_n,fk_n,tiempo_s," +
+                return CabeceraSujeto +
+                       ",fecha,estacion,nombre,k_nmm,b_nmms,fs_n,fk_n,tiempo_s," +
                        "f_media_n,f_pico_n,f_arranque_n,impulso_ns,trabajo_nmm,completada,fuente";
             }
         }
@@ -223,7 +224,7 @@
             {
                 var e = estaciones[i];
                 if (e == null) continue;
-                resumen.Fila(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
+                FilaResumen(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                              i + 1, e.nombre,
                              e.k, e.b, e.Fs, e.Fk,
                              e.TiempoActivo,
diff -ru -x '*.meta' C:/Users/76905/Downloads/ReportesOMNI/Actividad4/docs/superpowers/sdd/2026-09-02-hanoi-3d-omni/task-3-antes/Assets/Scripts/Ambientes/AmbienteBase.cs Assets/Scripts/Ambientes/AmbienteBase.cs
--- C:/Users/76905/Downloads/ReportesOMNI/Actividad4/docs/superpowers/sdd/2026-09-02-hanoi-3d-omni/task-3-antes/Assets/Scripts/Ambientes/AmbienteBase.cs	2026-09-02 16:28:42.442526700 -0600
+++ Assets/Scripts/Ambientes/AmbienteBase.cs	2026-09-02 16:29:44.364858500 -0600
@@ -5,6 +5,7 @@
 // terminar. Aqui vive esa maquina de estados, el cronometro, el muestreo de la
 // traza y los atajos de teclado comunes; lo especifico de cada escenario esta
 // en Ambiente1/2/3.
+using System.IO;
 using UnityEngine;
 using UnityEngine.InputSystem;
 
@@ -42,6 +43,66 @@
         protected abstract string CabeceraTraza { get; }
         protected abstract string CabeceraResumen { get; }
 
+        /// <summary>Columnas que anteceden a toda fila de resumen, en los cuatro
+        /// ambientes. Cada ambiente las antepone a su propia cabecera.</summary>
+        public const string CabeceraSujeto = "sujeto,sesion,ensayo,condicion";
+
+        /// <summary>Numero de ensayo de este sujeto en este ambiente. Se recalcula
+        /// al arrancar cada ensayo, no una sola vez al cargar la escena: asi hacer
+        /// varios ensayos seguidos sin salir de la escena no pisa archivos.</summary>
+        protected int Ensayo { get; private set; } = 1;
+
+        /// <summary>Nombre de un archivo propio de este ensayo, del estilo
+        /// S01_hanoi_e03_traza.csv.</summary>
+        protected string NombreArchivoEnsayo(string sufijo)
+        {
+            return SesionSujeto.IdActual + "_" + PrefijoArchivos +
+                   "_e" + Ensayo.ToString("00") + "_" + sufijo;
+        }
+
+        /// <summary>Nombre del acumulado del sujeto para este ambiente.</summary>
+        protected string NombreArchivoResumen()
+        {
+            return SesionSujeto.IdActual + "_" + PrefijoArchivos + "_resumen.csv";
+        }
+
+        /// <summary>Ruta que espera StopRecording: es relativa a Analysis/datos y el
+        /// puente crea la subcarpeta por su cuenta.</summary>
+        private string RutaRelativa1kHz()
+        {
+            return SesionSujeto.IdActual + "/" + NombreArchivoEnsayo("1khz.csv");
+        }
+
+        /// <summary>Cuenta las trazas ya guardadas por este sujeto en este ambiente.
+        /// Se cuentan archivos de traza y no filas de resumen porque el ambiente 3
+        /// escribe cinco filas por ensayo (una por estacion) y el conteo saldria mal.</summary>
+        private int SiguienteEnsayo()
+        {
+            try
+            {
+                string carpeta = RegistroCsv.CarpetaDeSujeto();
+                string patron = SesionSujeto.IdActual + "_" + PrefijoArchivos + "_e*_traza.csv";
+                return Directory.GetFiles(carpeta, patron).Length + 1;
+            }
+            catch (System.Exception e)
+            {
+                Debug.LogWarning("[" + Nombre + "] No pude contar ensayos previos: " + e.Message);
+                return 1;
+            }
+        }
+
+        /// <summary>Anade una fila de resumen con las columnas del sujeto delante.</summary>
+        protected void FilaResumen(params object[] valores)
+        {
+            var completa = new object[valores.Length + 4];
+            completa[0] = SesionSujeto.IdActual;
+            completa[1] = SesionSujeto.SesionActual;
+            completa[2] = Ensayo;
+            completa[3] = SesionSujeto.CondicionActual;
+            System.Array.Copy(valores, 0, completa, 4, valores.Length);
+            resumen.Fila(completa);
+        }
+
         /// <summary>Se llama una vez por muestra de traza mientras el ensayo corre.</summary>
         protected abstract void MuestrearTraza(float t);
         /// <summary>Se llama al terminar; aqui cada ambiente escribe su fila de resumen.</summary>
@@ -54,6 +115,7 @@
             if (dispositivo == null) dispositivo = FindFirstObjectByType<HapticDevice>();
             traza   = new RegistroCsv(CabeceraTraza);
             resumen = new RegistroCsv(CabeceraResumen);
+            Ensayo = SiguienteEnsayo();
         }
 
         protected virtual void Start() { Reiniciar(); }
@@ -76,6 +138,7 @@
         {
             if (Estado == EstadoEnsayo.EnCurso) return;
             Estado = EstadoEnsayo.EnCurso;
+            Ensayo = SiguienteEnsayo();
             Tiempo = 0f;
             acumTraza = 0f;
             traza.Limpiar();
@@ -91,12 +154,12 @@
             Estado = EstadoEnsayo.Terminado;
 
             EscribirResumen();
-            traza.Guardar(PrefijoArchivos + "_traza.csv", false);
-            resumen.Guardar(PrefijoArchivos + "_resumen.csv", true);
+            traza.Guardar(NombreArchivoEnsayo("traza.csv"), false);
+            resumen.Guardar(NombreArchivoResumen(), true);
             resumen.Limpiar();
 
             if (dispositivo != null && dispositivo.Recording)
-                dispositivo.StopRecording(PrefijoArchivos + "_1khz.csv");
+                dispositivo.StopRecording(RutaRelativa1kHz());
 
             Debug.Log("[" + Nombre + "] Ensayo terminado en " + Tiempo.ToString("F2") + " s.");
         }
```

## AmbienteBase.cs completo (tras el cambio)
```csharp
// AmbienteBase.cs -- Lo que comparten los tres ambientes de la Actividad 4.
//
// Cada ambiente es un ensayo cronometrado: se prepara, arranca cuando el
// usuario hace la primera accion, mide mientras dura y guarda dos CSV al
// terminar. Aqui vive esa maquina de estados, el cronometro, el muestreo de la
// traza y los atajos de teclado comunes; lo especifico de cada escenario esta
// en Ambiente1/2/3.
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HapticOmni
{
    public enum EstadoEnsayo { Preparado, EnCurso, Terminado }

    public abstract class AmbienteBase : MonoBehaviour
    {
        [Header("Comun")]
        public HapticDevice dispositivo;
        [Tooltip("Muestras por segundo de la traza. 100 Hz basta para dibujar la " +
                 "trayectoria y las curvas de fuerza; el detalle fino ya lo tiene " +
                 "el CSV nativo de 1 kHz.")]
        public float frecuenciaTraza = 100f;
        [Tooltip("Graba tambien el CSV nativo a 1 kHz mientras dura el ensayo.")]
        public bool grabarCsvNativo = true;

        public EstadoEnsayo Estado { get; protected set; } = EstadoEnsayo.Preparado;
        /// <summary>Segundos transcurridos desde que arranco el ensayo.</summary>
        public float Tiempo { get; private set; }

        /// <summary>Nombre corto que se muestra en el HUD.</summary>
        public abstract string Nombre { get; }
        /// <summary>Una linea por renglon del panel de estado.</summary>
        public abstract string[] LineasHUD();
        /// <summary>Ayuda de controles que se muestra abajo del panel.</summary>
        public abstract string[] LineasAyuda();

        protected RegistroCsv traza;
        protected RegistroCsv resumen;
        private float acumTraza;

        protected abstract string PrefijoArchivos { get; }
        protected abstract string CabeceraTraza { get; }
        protected abstract string CabeceraResumen { get; }

        /// <summary>Columnas que anteceden a toda fila de resumen, en los cuatro
        /// ambientes. Cada ambiente las antepone a su propia cabecera.</summary>
        public const string CabeceraSujeto = "sujeto,sesion,ensayo,condicion";

        /// <summary>Numero de ensayo de este sujeto en este ambiente. Se recalcula
        /// al arrancar cada ensayo, no una sola vez al cargar la escena: asi hacer
        /// varios ensayos seguidos sin salir de la escena no pisa archivos.</summary>
        protected int Ensayo { get; private set; } = 1;

        /// <summary>Nombre de un archivo propio de este ensayo, del estilo
        /// S01_hanoi_e03_traza.csv.</summary>
        protected string NombreArchivoEnsayo(string sufijo)
        {
            return SesionSujeto.IdActual + "_" + PrefijoArchivos +
                   "_e" + Ensayo.ToString("00") + "_" + sufijo;
        }

        /// <summary>Nombre del acumulado del sujeto para este ambiente.</summary>
        protected string NombreArchivoResumen()
        {
            return SesionSujeto.IdActual + "_" + PrefijoArchivos + "_resumen.csv";
        }

        /// <summary>Ruta que espera StopRecording: es relativa a Analysis/datos y el
        /// puente crea la subcarpeta por su cuenta.</summary>
        private string RutaRelativa1kHz()
        {
            return SesionSujeto.IdActual + "/" + NombreArchivoEnsayo("1khz.csv");
        }

        /// <summary>Cuenta las trazas ya guardadas por este sujeto en este ambiente.
        /// Se cuentan archivos de traza y no filas de resumen porque el ambiente 3
        /// escribe cinco filas por ensayo (una por estacion) y el conteo saldria mal.</summary>
        private int SiguienteEnsayo()
        {
            try
            {
                string carpeta = RegistroCsv.CarpetaDeSujeto();
                string patron = SesionSujeto.IdActual + "_" + PrefijoArchivos + "_e*_traza.csv";
                return Directory.GetFiles(carpeta, patron).Length + 1;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[" + Nombre + "] No pude contar ensayos previos: " + e.Message);
                return 1;
            }
        }

        /// <summary>Anade una fila de resumen con las columnas del sujeto delante.</summary>
        protected void FilaResumen(params object[] valores)
        {
            var completa = new object[valores.Length + 4];
            completa[0] = SesionSujeto.IdActual;
            completa[1] = SesionSujeto.SesionActual;
            completa[2] = Ensayo;
            completa[3] = SesionSujeto.CondicionActual;
            System.Array.Copy(valores, 0, completa, 4, valores.Length);
            resumen.Fila(completa);
        }

        /// <summary>Se llama una vez por muestra de traza mientras el ensayo corre.</summary>
        protected abstract void MuestrearTraza(float t);
        /// <summary>Se llama al terminar; aqui cada ambiente escribe su fila de resumen.</summary>
        protected abstract void EscribirResumen();
        /// <summary>Deja el escenario como al principio.</summary>
        protected abstract void PrepararEscenario();

        protected virtual void Awake()
        {
            if (dispositivo == null) dispositivo = FindFirstObjectByType<HapticDevice>();
            traza   = new RegistroCsv(CabeceraTraza);
            resumen = new RegistroCsv(CabeceraResumen);
            Ensayo = SiguienteEnsayo();
        }

        protected virtual void Start() { Reiniciar(); }

        /// <summary>Vuelve al estado inicial sin guardar nada.</summary>
        public virtual void Reiniciar()
        {
            Estado = EstadoEnsayo.Preparado;
            Tiempo = 0f;
            acumTraza = 0f;
            traza.Limpiar();
            PrepararEscenario();
        }

        /// <summary>Arranca el cronometro. Los ambientes la llaman en la primera
        /// accion real del usuario, no al cargar la escena: si el reloj empezara
        /// al abrir la escena el tiempo medido incluiria la lectura de las
        /// instrucciones.</summary>
        public virtual void Iniciar()
        {
            if (Estado == EstadoEnsayo.EnCurso) return;
            Estado = EstadoEnsayo.EnCurso;
            Ensayo = SiguienteEnsayo();
            Tiempo = 0f;
            acumTraza = 0f;
            traza.Limpiar();
            if (grabarCsvNativo && dispositivo != null && dispositivo.Connected)
                dispositivo.StartRecording(300);
            Debug.Log("[" + Nombre + "] Ensayo iniciado.");
        }

        /// <summary>Cierra el ensayo y guarda los archivos.</summary>
        public virtual void Terminar()
        {
            if (Estado != EstadoEnsayo.EnCurso) return;
            Estado = EstadoEnsayo.Terminado;

            EscribirResumen();
            traza.Guardar(NombreArchivoEnsayo("traza.csv"), false);
            resumen.Guardar(NombreArchivoResumen(), true);
            resumen.Limpiar();

            if (dispositivo != null && dispositivo.Recording)
                dispositivo.StopRecording(RutaRelativa1kHz());

            Debug.Log("[" + Nombre + "] Ensayo terminado en " + Tiempo.ToString("F2") + " s.");
        }

        protected virtual void Update()
        {
            LeerAtajosComunes();

            if (Estado != EstadoEnsayo.EnCurso) return;
            Tiempo += Time.deltaTime;

            acumTraza += Time.deltaTime;
            float paso = 1f / Mathf.Max(1f, frecuenciaTraza);
            while (acumTraza >= paso)
            {
                acumTraza -= paso;
                MuestrearTraza(Tiempo);
            }
        }

        private void LeerAtajosComunes()
        {
            var t = Keyboard.current;
            if (t == null) return;

            if (t.rKey.wasPressedThisFrame) Reiniciar();
            if (t.fKey.wasPressedThisFrame && dispositivo != null)
                dispositivo.forcesEnabled = !dispositivo.forcesEnabled;
            if (t.escapeKey.wasPressedThisFrame) SelectorAmbientes.IrAlMenu();

            // Botón 2 del stylus: tambien reinicia, para no soltar el equipo.
            if (dispositivo != null && dispositivo.Button2Down) Reiniciar();
        }

        /// <summary>Texto mm:ss.cs del cronometro.</summary>
        public string TiempoTexto
        {
            get
            {
                int m = Mathf.FloorToInt(Tiempo / 60f);
                float s = Tiempo - m * 60f;
                return string.Format("{0:00}:{1:00.00}", m, s);
            }
        }
    }
}
```
