using System.Collections.Generic;

namespace Roulette.Contracts
{
    public class RouletteEntity
    {
        public string Name { get; set; }

        public RouletteState PropState { get; set; }

        public List<Bet> Bets { get; set; }

        public int Result { get; set; }
    }
}
