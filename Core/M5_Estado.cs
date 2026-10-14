using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NolipateSimulator.Core
{
    public class M5_Estado
    {
        public List<char> Jugadores { get; set; }
        public int TurnoIndex { get; set; }
        public int RondaActual { get; set; }
        public int? DadoPublicado { get; set; }
        public int SeisesConsecutivos { get; set; }
        public int? BonoPendiente { get; set; } // 10 o 20
        public bool TiradaExtra { get; set; }
        public char? Ganador { get; set; }

        // Mapeo Ficha (ej: "A1", "R2") -> Posición ("c", "5", "p3", "m")
        public Dictionary<string, string> Fichas { get; set; }

        public List<string> Bitacora { get; set; }
        public Stack<M5_Estado> PilaDeshacer { get; set; }

        public M5_Estado(IEnumerable<char> jugadores = null)
        {
            Jugadores = jugadores != null ? jugadores.ToList() : new List<char> { 'R', 'V' };
            TurnoIndex = 0;
            RondaActual = 1;
            DadoPublicado = null;
            SeisesConsecutivos = 0;
            BonoPendiente = null;
            TiradaExtra = false;
            Ganador = null;

            Fichas = new Dictionary<string, string>();
            foreach (char col in Jugadores)
            {
                for (int i = 1; i <= 4; i++)
                    Fichas[$"{col}{i}"] = "c";
            }

            Bitacora = new List<string>();
            PilaDeshacer = new Stack<M5_Estado>();
        }

        public char ColorActivo => Jugadores[TurnoIndex];

        public List<string> ObtenerOcupacion(string posicion) =>
            Fichas.Where(f => f.Value == posicion).Select(f => f.Key).ToList();

        public M5_Estado Clonar()
        {
            return new M5_Estado(this.Jugadores)
            {
                TurnoIndex = this.TurnoIndex,
                RondaActual = this.RondaActual,
                DadoPublicado = this.DadoPublicado,
                SeisesConsecutivos = this.SeisesConsecutivos,
                BonoPendiente = this.BonoPendiente,
                TiradaExtra = this.TiradaExtra,
                Ganador = this.Ganador,
                Bitacora = new List<string>(this.Bitacora),
                Fichas = new Dictionary<string, string>(this.Fichas)
            };
        }
    }
}
