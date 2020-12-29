using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Roulette.Contracts;
using Roulette.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roulette.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RouletteController : ControllerBase
    {
        private readonly ICacheService _cacheService;

        public RouletteController(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        [HttpGet("/{name}")]
        public async Task<IActionResult> GetRoulette([FromRoute] string name)
        {
            var value =  await this.GetRouletteByName(name);
            return value == null ? (IActionResult)NotFound("Ruleta no encontrada.") : Ok(value);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateRoulette([FromBody] NewRouletteRequest rouletteRequest)
        {
            var validate = await this.GetRouletteByName(rouletteRequest.RouletteId);
            if (validate == null)
            {
                var roulette = new RouletteEntity()
                {
                    Name = rouletteRequest.RouletteId,
                    PropState = RouletteState.Created
                };
                await _cacheService.SetCacheValueAsync(rouletteRequest.RouletteId, roulette);
                var response = new NewRouletteResponse()
                {
                    RouletteId = rouletteRequest.RouletteId,
                    Message = $"Ruleta con id {rouletteRequest.RouletteId} creada correctamente."
                };
                return Ok(response);
            }
            else
            {
                return NotFound($"Ruleta {rouletteRequest.RouletteId}, ya se encuentra creada y su estado es {validate.PropState}!, debe ingresar otro nombre.");
            }           
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartRoulette([FromHeader] string userId, [FromBody] OpenRouletteRequest openRouletteRequest)
        {
            var validate = await this.GetRouletteByName(openRouletteRequest.RouletteId);
            if (validate != null && (validate.PropState == RouletteState.Created || validate.PropState == RouletteState.Opened))
            {
                RouletteState state = RouletteState.Error;
                
                if (validate.PropState == RouletteState.Created)
                {
                    var openedRoulette = await OpenRoulette(openRouletteRequest.RouletteId);
                    state = openedRoulette.State;
                }

                if (state == RouletteState.Opened && openRouletteRequest.PropBet != null)
                {
                    var bets = await this.CreateBet(validate, openRouletteRequest.PropBet, userId);

                    return Ok(bets.PropBetState);
                }
                
                return Ok(state);
            }
            else
            {
                return NotFound($"Ruleta {openRouletteRequest.RouletteId}, puede que no se encuentre creada o su estado es cerrada.");
            }
        }

        [HttpPost("close")]
        public async Task<IActionResult> CloseRoulette(string rouletteId)
        {
            var validate = await this.GetRouletteByName(rouletteId);
            if (validate != null && validate.PropState != RouletteState.Closed)
            {
                validate = this.DefineWinners(validate);
                validate.PropState = RouletteState.Closed;

                await _cacheService.SetCacheValueAsync(rouletteId, validate);
            }
            return Ok(validate);
        }

        [HttpPost("all")]
        public IActionResult GetAllRoulettes()
        {
            var keys = _cacheService.GetKeysAsync();
            var listRoulettes = new List<RouletteEntity>();

            keys.ForEach(x =>
            {
                listRoulettes.Add(this.GetRouletteByName(x.ToString()).Result);
            });

            return Ok(listRoulettes);
        }

        private async Task<RouletteEntity> GetRouletteByName(string name)
        {
            var value = await _cacheService.GetCacheValueAsync(name);
            return string.IsNullOrEmpty(value) ? null : JsonConvert.DeserializeObject<RouletteEntity>(value);
        }

        private async Task<OpenRouletteResponse> OpenRoulette(string rouletteId)
        {
            var response = new OpenRouletteResponse()
            {
                State = RouletteState.Opened,
                Message = $"Ruleta con id {rouletteId} abierta correctamente."
            };
            try
            {
                var roulette = new RouletteEntity()
                {
                    Name = rouletteId,
                    PropState = RouletteState.Opened
                };
                await _cacheService.SetCacheValueAsync(rouletteId, roulette);
            }
            catch (Exception ex)
            {
                response.State = RouletteState.Error;
                response.Message = $"Ha ocurrido un error al tratar de abrir la ruleta {rouletteId}.";
                response.MessageDetail = ex.Message;
            }

            return response;
        }

        private async Task<BetResponse> CreateBet(RouletteEntity rouletteEntity, BetRequest bet, string userId)
        {
            BetResponse betResponse = new BetResponse() { 
                IdBet = -1,
                IdRoulette = rouletteEntity.Name,
                PropBetState = BetState.Created
            };
            try
            {
                rouletteEntity = AddOrInicializeBet(rouletteEntity, bet, userId);
                await _cacheService.SetCacheValueAsync(rouletteEntity.Name, rouletteEntity);
                betResponse.IdBet = rouletteEntity.Bets.Count;
                betResponse.Message = $"Apuesta creada de forma correcta";

                return betResponse;
            }
            catch (Exception ex)
            {
                betResponse.Message = $"Error al crear la apuesta.";
                betResponse.MessageDetail = ex.Message;
                betResponse.PropBetState = BetState.Error;

                return betResponse;
            }
        }

        private static RouletteEntity AddOrInicializeBet(RouletteEntity rouletteEntity, BetRequest bet, string userId)
        {
            var betsCount = rouletteEntity.Bets == null ? 1 : rouletteEntity.Bets.Count + 1;

            if (rouletteEntity.Bets == null)
            {
                rouletteEntity.Bets = new List<Bet>();
            }

            rouletteEntity.Bets.Add(new Bet()
            {
                Id = betsCount,
                PropBet = bet,
                UserId = userId
            });

            return rouletteEntity;
        }

        private RouletteEntity DefineWinners(RouletteEntity rouletteEntity)
        {
            rouletteEntity.Result = GenerateIntNumberWithGuid();
            var pair = rouletteEntity.Result % 2 == 0;
            rouletteEntity.Bets.ForEach(x =>
            {
                if (x.PropBet.IsColor)
                {
                    x.Award = (pair == (x.PropBet.Number % 2 == 0)) ? x.PropBet.Amount * 1.8 : 0;
                }
                else 
                {
                    x.Award = (rouletteEntity.Result == x.PropBet.Number) ? x.PropBet.Amount * 5 : 0;
                }
            });

            return rouletteEntity;
        }

        private static int GenerateIntNumberWithGuid()
        {
            var guid = Guid.NewGuid();
            var justNumbers = new String(guid.ToString().Where(Char.IsDigit).ToArray());
            var seed = int.Parse(justNumbers.Substring(0, 4));
            var random = new Random(seed);

            return random.Next(0, 36);
        }
    }
}
