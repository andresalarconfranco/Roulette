using System.Collections.Generic;

namespace Roulette.Contracts
{
    public class OpenRouletteRequest
    {
        public string RouletteId { get; set; }

        public BetRequest PropBet { get; set; }
    }
}
