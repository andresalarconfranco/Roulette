namespace Roulette.Contracts
{
    public class OpenRouletteResponse
    {
        public RouletteState State { get; set; }

        public string Message { get; set; }

        public string MessageDetail { get; set; }
    }
}
