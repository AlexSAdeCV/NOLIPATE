using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOPILATE.Core
{
    public static class Constantes
    {
        // Alfabeto canónico Σ de 27 caracteres (Sección 10)
        // Mayúsculas: A, Z, R, V. Minúsculas: d, b, c, p, m. Dígitos: 0-9. Signos: -, x, =, !, #, *, ., espacio
        public static readonly HashSet<char> AlfabetoCanonico = new HashSet<char>
        {
            'A', 'Z', 'R', 'V',
            'd', 'b', 'c', 'p', 'm',
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            '-', 'x', '=', '!', '#', '*', '.', ' '
        };

        // Colores participantes en el orden oficial
        public static readonly char[] Colores = { 'A', 'Z', 'R', 'V' };

        // 12 Casillas seguras (H) donde no se permite capturar (Regla 6)
        public static readonly HashSet<int> CasillasSeguras = new HashSet<int>
        {
            5, 12, 17, 22, 29, 34, 39, 46, 51, 56, 63, 68
        };

        // Casillas de salida para cada color tras obtener un 5 (Regla 2)
        public static readonly Dictionary<char, int> CasillasSalida = new Dictionary<char, int>
        {
            { 'A', 5 },
            { 'Z', 22 },
            { 'R', 39 },
            { 'V', 56 }
        };

        // Casilla del circuito tras la cual la ficha abandona la ruta común e ingresa a p1 (Regla 8)
        public static readonly Dictionary<char, int> EntradasPasillo = new Dictionary<char, int>
        {
            { 'A', 68 },
            { 'Z', 17 },
            { 'R', 34 },
            { 'V', 51 }
        };
    }
}
