using NolipateSimulator.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOPILATE.Core
{
    public class M0_Dado
    {
        private readonly Random _rng = new Random();
        private int? _forzado;

        public int? ValorPublicado { get; private set; }

        public int? Lanzar(M5_Estado estado)
        {
            if (estado.Ganador.HasValue || estado.BonoPendiente.HasValue)
                return null;

            int valor = _forzado.HasValue ? _forzado.Value : _rng.Next(1, 7);
            _forzado = null;

            ValorPublicado = valor;
            estado.DadoPublicado = valor;

            if (valor == 6)
                estado.SeisesConsecutivos++;

            return valor;
        }

        public void Forzar(int valor)
        {
            if (valor < 1 || valor > 6)
                throw new ArgumentOutOfRangeException(nameof(valor), "El dado debe ser entre 1 y 6.");
            _forzado = valor;
        }

        public void Restaurar(int? valor)
        {
            ValorPublicado = valor;
        }
    }
}
