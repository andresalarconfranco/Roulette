namespace Roulette.Contracts
{
    public class Bet
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public BetRequest PropBet { get; set; }

        public double Award { get; set; }
    }
}
