### Task 6: `DiscoHanoi` y `TorreHanoi`

**Files:**
- Create: `Assets/Scripts/Ambientes/A4/DiscoHanoi.cs`
- Create: `Assets/Scripts/Ambientes/A4/TorreHanoi.cs`

**Interfaces:**
- Consumes: `ConfiguracionHanoi.GrosorDisco` (tarea 5), `HapticShape`, `TipoForma`.
- Produces:
  - `DiscoHanoi`: campos públicos `indice`, `masaKg`, `radio`, `retardoPorKg`; propiedades `Agarrado`, `Cayendo`, `Agarres`, `Caidas`, `TiempoEnMano`, `PesoN`; métodos `Configurar(int, float, float, float, float, float)`, `Agarrar(Vector3)`, `SeguirPunta(Vector3, float)`, `PosarEn(Vector3)`, `Caer()`, `DistanciaA(Vector3)`, `Reiniciar()`, `ActualizarCaida(float)` → `bool`
  - `TorreHanoi`: campos `indice`, `radioAceptacion`, `alturaBase`, `barrera`, `margenBarrera`; métodos `Contiene(Vector3)`, `DistanciaHorizontal(Vector3)`, `PosicionDisco(int nivel)`, `MostrarBarrera(bool visible, int altura, float rigidez)`

- [ ] **Paso 1: Escribir `DiscoHanoi`**

`Assets/Scripts/Ambientes/A4/DiscoHanoi.cs`:

```csharp
// DiscoHanoi.cs -- Un disco: la parte que se ve y se siente.
//
// El disco no sabe las reglas del juego; de eso se encarga EstadoHanoi. Aqui
// solo vive el ciclo fisico, que es el mismo que ya funciona en ObjetoAgarrable
// del ambiente 1:
//
//   En la torre  cinematico y quieto, con su HapticShape encendido: se puede tocar.
//   Agarrado     sigue a la punta con un resorte amortiguado cuyo retardo crece
//                con la masa. Se le apaga la forma haptica, porque si no el servo
//                loop intentaria expulsar la punta del interior del disco justo
//                cuando el usuario lo esta sujetando y la mano sentiria dos
//                fuerzas peleandose.
//   Cayendo      lo mueve la fisica de Unity. Soltar fuera de una torre tiene
//                consecuencia: el disco cae a la mesa, cuenta como caida y vuelve
//                a su torre al cabo de un momento.
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A4 - Disco de Hanoi")]
    [RequireComponent(typeof(Rigidbody))]
    public class DiscoHanoi : MonoBehaviour
    {
        [Header("Identidad")]
        [Tooltip("0 es el disco mas pequeno.")]
        public int indice = 0;

        [Header("Fisica")]
        [Tooltip("Masa en kg. El peso que siente la mano es m*g, saturado a 3 N por " +
                 "el puente.")]
        public float masaKg = 0.10f;
        [Tooltip("Radio en unidades de Unity. Manda en la regla del juego: un disco " +
                 "solo se apoya sobre otro de radio mayor.")]
        public float radio = 0.2f;
        [Tooltip("Retardo del seguimiento cuando esta agarrado, en segundos por kg.")]
        public float retardoPorKg = 0.35f;
        [Tooltip("Segundos que tarda en volver a su torre despues de caerse.")]
        public float tiempoDeRegreso = 1.0f;

        public bool  Agarrado     { get; private set; }
        public bool  Cayendo      { get; private set; }
        public int   Agarres      { get; private set; }
        public int   Caidas       { get; private set; }
        public float TiempoEnMano { get; private set; }

        /// <summary>Peso en newtons: lo que el puente aplica como fuerza constante.</summary>
        public float PesoN { get { return masaKg * 9.81f; } }

        private HapticShape forma;
        private Rigidbody cuerpo;
        private Vector3 desfaseAgarre;
        private Vector3 velSuavizado;
        private float tCaida;

        void Awake()
        {
            forma  = GetComponent<HapticShape>();
            cuerpo = GetComponent<Rigidbody>();
            cuerpo.mass = masaKg;
        }

        /// <summary>Ajusta tamano, masa y aspecto haptico. La llama el ambiente cada
        /// vez que cambia el nivel: la escena crea siempre siete discos y el nivel
        /// decide cuantos se activan y como de grandes son.</summary>
        public void Configurar(int indice, float masaKg, float radio, float retardoPorKg,
                               float rigidez, float friccion)
        {
            this.indice = indice;
            this.masaKg = masaKg;
            this.radio = radio;
            this.retardoPorKg = retardoPorKg;

            // El cilindro primitivo de Unity mide 1 de diametro y 2 de alto.
            transform.localScale = new Vector3(radio * 2f,
                                               ConfiguracionHanoi.GrosorDisco * 0.5f,
                                               radio * 2f);

            if (cuerpo == null) cuerpo = GetComponent<Rigidbody>();
            cuerpo.mass = masaKg;

            if (forma == null) forma = GetComponent<HapticShape>();
            if (forma != null)
            {
                // No hay primitiva cilindro en el puente: una caja de semilados
                // (radio, medio grosor, radio) es la aproximacion util. La punta
                // toca las esquinas un poco antes que el borde real, y a este
                // tamano la diferencia no se percibe.
                forma.tipo = TipoForma.Caja;
                forma.semiladosOverride = new Vector3(radio, ConfiguracionHanoi.GrosorDisco * 0.5f, radio);
                forma.rigidez = rigidez;
                forma.friccion = friccion;
            }
        }

        public void Reiniciar()
        {
            Agarrado = false;
            Cayendo = false;
            Agarres = 0;
            Caidas = 0;
            TiempoEnMano = 0f;
            tCaida = 0f;
            velSuavizado = Vector3.zero;
            HacerQuieto();
        }

        private void HacerQuieto()
        {
            if (cuerpo == null) cuerpo = GetComponent<Rigidbody>();
            cuerpo.isKinematic = true;
            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
            if (forma != null) forma.enabled = true;
        }

        /// <summary>Distancia de la punta a la superficie del disco.</summary>
        public float DistanciaA(Vector3 punta)
        {
            return forma != null ? forma.DistanciaA(punta)
                                 : Vector3.Distance(punta, transform.position) - radio;
        }

        public void Agarrar(Vector3 puntaMundo)
        {
            if (Agarrado) return;
            Agarrado = true;
            Cayendo = false;
            Agarres++;
            desfaseAgarre = transform.position - puntaMundo;
            velSuavizado = Vector3.zero;
            cuerpo.isKinematic = true;
            if (forma != null) forma.enabled = false;   // que no pelee con la mano
        }

        public void SeguirPunta(Vector3 puntaMundo, float dt)
        {
            if (!Agarrado) return;
            TiempoEnMano += dt;
            float retardo = Mathf.Max(0.01f, masaKg * retardoPorKg);
            transform.position = Vector3.SmoothDamp(transform.position,
                                                    puntaMundo + desfaseAgarre,
                                                    ref velSuavizado, retardo);
        }

        /// <summary>Lo deja fijo en un sitio concreto: colocado en una torre, o
        /// devuelto a la suya tras un intento ilegal.</summary>
        public void PosarEn(Vector3 destino)
        {
            Agarrado = false;
            Cayendo = false;
            tCaida = 0f;
            transform.SetPositionAndRotation(destino, Quaternion.identity);
            HacerQuieto();
        }

        /// <summary>Soltado fuera de toda torre: se lo lleva la fisica.</summary>
        public void Caer()
        {
            Agarrado = false;
            Cayendo = true;
            Caidas++;
            tCaida = 0f;
            if (forma != null) forma.enabled = true;
            cuerpo.isKinematic = false;
            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
        }

        /// <summary>Avanza el temporizador de la caida. Devuelve true cuando ya toca
        /// devolverlo a su torre.</summary>
        public bool ActualizarCaida(float dt)
        {
            if (!Cayendo) return false;
            tCaida += dt;
            return tCaida >= tiempoDeRegreso;
        }
    }
}
```

- [ ] **Paso 2: Escribir `TorreHanoi`**

`Assets/Scripts/Ambientes/A4/TorreHanoi.cs`:

```csharp
// TorreHanoi.cs -- Un poste, y la barrera que dice "aqui no".
//
// El poste es una caja haptica vertical: se puede tocar, y sirve de referencia
// tactil para localizar la torre sin mirar. La barrera es lo que hace que este
// ambiente sea haptico y no solo tridimensional: cuando el disco que llevas no
// cabe en esta torre, se enciende un semiespacio justo por encima de la cima y
// la mano topa con un techo invisible. El "no" se siente antes de intentar
// soltar, que es exactamente lo que un guante o un raton no pueden dar.
//
// La aceptacion no usa triggers de fisica: es una distancia horizontal al eje
// del poste, evaluada en el instante de soltar. El criterio es explicito y
// medible en vez de depender de si dos colliders se solapaban ese frame.
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A4 - Torre de Hanoi")]
    public class TorreHanoi : MonoBehaviour
    {
        [Tooltip("0, 1 o 2. El juego empieza en la 0 y se resuelve en la 2.")]
        public int indice = 0;

        [Tooltip("Radio horizontal dentro del cual se considera que el disco esta " +
                 "sobre esta torre, en unidades de Unity.")]
        public float radioAceptacion = 0.46f;

        [Tooltip("Y de la mesa: donde se apoya la cara inferior del primer disco.")]
        public float alturaBase = -1.02f;

        [Tooltip("Semiespacio que se enciende para rechazar un disco que no cabe.")]
        public HapticShape barrera;

        [Tooltip("Margen sobre la cima al que se coloca la barrera.")]
        public float margenBarrera = 0.06f;

        /// <summary>True si el punto cae dentro del cilindro de aceptacion.</summary>
        public bool Contiene(Vector3 p)
        {
            Vector3 c = transform.position;
            float dh = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z));
            return dh <= radioAceptacion;
        }

        /// <summary>Distancia horizontal de un punto al eje del poste.</summary>
        public float DistanciaHorizontal(Vector3 p)
        {
            Vector3 c = transform.position;
            return Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z));
        }

        /// <summary>Centro del disco que ocupe el nivel dado (0 = el de abajo).</summary>
        public Vector3 PosicionDisco(int nivel)
        {
            Vector3 c = transform.position;
            float y = alturaBase + ConfiguracionHanoi.GrosorDisco * (nivel + 0.5f);
            return new Vector3(c.x, y, c.z);
        }

        /// <summary>Enciende o apaga el techo de rechazo. `altura` es el numero de
        /// discos que ya hay en la torre.</summary>
        public void MostrarBarrera(bool visible, int altura, float rigidez)
        {
            if (barrera == null) return;
            if (visible)
            {
                Vector3 c = transform.position;
                float y = alturaBase + ConfiguracionHanoi.GrosorDisco * altura + margenBarrera;
                barrera.transform.position = new Vector3(c.x, y, c.z);
                barrera.rigidez = rigidez;
            }
            if (barrera.gameObject.activeSelf != visible)
                barrera.gameObject.SetActive(visible);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.5f);
            Vector3 c = new Vector3(transform.position.x, alturaBase + 0.01f, transform.position.z);
            const int n = 32;
            Vector3 prev = c + new Vector3(radioAceptacion, 0, 0);
            for (int i = 1; i <= n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                Vector3 p = c + new Vector3(Mathf.Cos(a) * radioAceptacion, 0,
                                            Mathf.Sin(a) * radioAceptacion);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
```

- [ ] **Paso 3: Punto de control**

- Ambos archivos compilan sin errores ni warnings.
- `DiscoHanoi` no contiene ninguna regla del juego (ni comparaciones de radio entre discos): esa lógica es de `EstadoHanoi`.
- `TorreHanoi.PosicionDisco(0)` deja la cara inferior del disco exactamente en `alturaBase`.

---

