namespace Roulette.Contracts
{
    public class BetResponse
    {
        public string IdRoulette { get; set; }

        public int IdBet { get; set; }

        public BetState PropBetState { get; set; }

        public string Message { get; set; }

        public string MessageDetail { get; set; }
    }
}
