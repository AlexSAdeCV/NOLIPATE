using NOPILATE.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NolipateSimulator.Core
{
    public static class M6_Salida
    {
        public static string GenerarVistaTablero(M5_Estado e)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[ TABLERO 8x4 - OCUPACIÓN ]\n");
            sb.AppendLine("casilla  ocupación         zona     detalle");
            sb.AppendLine("------------------------------------------------------------------");

            // 1. Obtener casillas del circuito (1 a 68) que contengan fichas
            var ocupadasCircuito = e.Fichas
                .Where(f => f.Value != "c" && !f.Value.StartsWith("p") && f.Value != "m")
                .GroupBy(f => f.Value)
                .OrderBy(g => int.TryParse(g.Key, out int n) ? n : 999)
                .ToList();

            // 2. Detalle de zonas
            var casaDetalle = string.Join(" ", e.Jugadores.Select(c => $"{c}:{e.Fichas.Count(f => f.Key.StartsWith(c.ToString()) && f.Value == "c")}"));
            var pasillosConFichas = e.Fichas.Where(f => f.Value.StartsWith("p")).Select(f => $"{f.Key} en {f.Value}").ToList();
            string pasilloDetalle = pasillosConFichas.Any() ? string.Join(" ", pasillosConFichas) : "vacío";
            var metaDetalle = string.Join(" ", e.Jugadores.Select(c => $"{c}:{e.Fichas.Count(f => f.Key.StartsWith(c.ToString()) && f.Value == "m")}"));

            // 3. Imprimir filas
            int maxFilas = System.Math.Max(ocupadasCircuito.Count, 3);
            for (int i = 0; i < maxFilas; i++)
            {
                string colCasilla = "-";
                string colOcupacion = "-";

                if (i < ocupadasCircuito.Count)
                {
                    string casilla = ocupadasCircuito[i].Key;
                    int.TryParse(casilla, out int numC);
                    string marcaSeguro = Constantes.CasillasSeguras.Contains(numC) ? "*" : " ";
                    colCasilla = $"{casilla.PadLeft(2, '0')}{marcaSeguro}";

                    // CORRECCIÓN: proyectar únicamente las claves (nombres de ficha)
                    var fichas = ocupadasCircuito[i].Select(x => x.Key).ToList();
                    colOcupacion = string.Join(" ", fichas);
                    if (fichas.Count == 2 && fichas[0][0] == fichas[1][0])
                        colOcupacion += " (barrera)";
                }

                string colZona = "";
                string colDetalle = "";
                if (i == 0) { colZona = "casa"; colDetalle = casaDetalle; }
                else if (i == 1) { colZona = "pasillo"; colDetalle = pasilloDetalle; }
                else if (i == 2) { colZona = "meta"; colDetalle = metaDetalle; }

                sb.AppendLine($"{colCasilla,-9} {colOcupacion,-17} {colZona,-8} {colDetalle}");
            }

            sb.AppendLine("------------------------------------------------------------------");
            sb.AppendLine("* = casilla de seguro/salida | MAYÚSCULAS = colores | zonas: c, p1-p7, m");
            return sb.ToString();
        }

        public static string GenerarVistaEstado(M5_Estado e, string ultimoResultado)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[ ESTADO Y RETROALIMENTACIÓN ]\n");
            sb.AppendLine($"Color en turno: {e.ColorActivo} | Ronda: {e.RondaActual} | Jugadores: {string.Join("-", e.Jugadores)}");
            sb.AppendLine($"Dado publicado: {(e.DadoPublicado.HasValue ? "d" + e.DadoPublicado.Value : "Ninguno")}");
            sb.AppendLine($"Seises consecutivos: {e.SeisesConsecutivos} | Bono pendiente: {(e.BonoPendiente.HasValue ? "b" + e.BonoPendiente.Value : "ninguno")}");
            sb.AppendLine($"Ganador: {(e.Ganador.HasValue ? e.Ganador.Value.ToString() + " (#" + e.Ganador.Value + ")" : "En juego")}");
            sb.AppendLine("------------------------------------------------------------------");
            sb.AppendLine(ultimoResultado);
            return sb.ToString();
        }
    }
}
