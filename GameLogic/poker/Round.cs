using System.Threading.Tasks;

namespace PokerServer.GameLogic.poker
{
    public class Round
    {
        private Deck _deck;
        private readonly List<Player> _players;
        private int _playerIndex = 0;
        private int _cycles = 0;
        private int _cycle_start_index;
        private int _starting_cycle_start_index;
        public List<Card> board { get; } = new List<Card>();

        public int Pot { get; set; } = 0;

        public int betSize { get; set; } = 0;

        public int totalRounds { get; set; } = 0;

        public Round(List<Player> players, int startIndex = 0)
        {
            _players = players;
            _deck = new Deck();
            _cycle_start_index = startIndex;
            _playerIndex = _cycle_start_index;
            _starting_cycle_start_index = _cycle_start_index;
        }

        public void startRound()
        {
            RankHand.winner = null;
            betSize = 20;
            _cycles = 0;
            _deck.Reset();
            _deck.Shuffle();
            _deck.Shuffle();
            foreach (Player p in _players)
            {
                p.isPlaying = true;
                p.setHand(_deck.Pop(), _deck.Pop());
                if (p.Money <= 0)
                {
                    p.isPlaying = false;
                }
            }


            //_players.RemoveAll(p => p.Money < betSize / 2);

            int bet = Math.Min(_players[^1].Money, betSize);
            _players[^1].Bet = bet;
            _players[^1].Money -= bet;
            Pot += bet;
            bet = Math.Min(_players[^2].Money, betSize / 2);
            _players[^2].Bet = bet;
            _players[^2].Money -= bet;
            Pot += bet;

        }

        public string getCurrentPlayerId()
        {
            return _players[_playerIndex].Id;
        }

        public bool doAction(string action, int amount)
        {
            switch (action)
            {
                case "fold":
                    _players[_playerIndex].isPlaying = false;
                    _playerIndex++;
                    return true;
                case "check":
                    if (betSize > 0) //call
                    {
                        int diff = betSize - _players[_playerIndex].Bet;
                        if (diff <= _players[_playerIndex].Money)
                        {
                            _players[_playerIndex].Bet = betSize;
                            _players[_playerIndex].Money -= diff;
                            Pot += diff;
                            _playerIndex++;
                        }
                        else
                        {
                            _players[_playerIndex].Bet += _players[_playerIndex].Money;
                            Pot +=  _players[_playerIndex].Money;
                            _players[_playerIndex].Money = 0;
                            _playerIndex++;
                        }
                        return true;
                    }
                    Console.WriteLine("checking...");
                    _playerIndex++;
                    return true;
                case "bet":
                    if (amount <= _players[_playerIndex].Money && amount + _players[_playerIndex].Bet > betSize)
                    {
                        _cycle_start_index = _playerIndex;
                        betSize = amount + _players[_playerIndex].Bet;
                        _players[_playerIndex].Bet = betSize;
                        _players[_playerIndex].Money -= amount;
                        Pot += amount;
                        _playerIndex++;
                        return true;
                    }
                    return false;
                default:
                    return false;


            }
        }

        public async Task<bool> endCycle(Func<Task> BroadcastStateAsync, Func<object, Task> BroadcastAsync)
        {
            foreach (var player in _players)
            {
                if (player.Money == 0)
                {
                    player.isPlaying = false;
                }
            }
            _playerIndex %= _players.Count;
            int counter = 0;
            foreach (Player p in _players)
            {
                if (!p.isPlaying)
                {
                    counter++;
                }else
                {
                    RankHand.winner = p; //if everyone else folded
                }
            }
            if (counter == _players.Count - 1)
            {
                _cycles = 3;
                _playerIndex = _cycle_start_index;
            }else
            {
                RankHand.winner = null;
                while (!_players[_playerIndex].isPlaying)
                {
                    _playerIndex++;
                    _playerIndex %= _players.Count;
                }
                while (!_players[_cycle_start_index].isPlaying)
                {
                    _cycle_start_index++;
                    _cycle_start_index %= _players.Count;
                }
            }
            if (_playerIndex == _cycle_start_index)
            {
                foreach (Player p in _players)
                {
                    p.Bet = 0;
                }
                betSize = 0;
                _playerIndex = _starting_cycle_start_index;
                _cycle_start_index = _starting_cycle_start_index;
                _cycles++;
                if (_cycles == 1)
                {
                    board.Add(_deck.Pop());
                    board.Add(_deck.Pop());
                    board.Add(_deck.Pop());
                }
                else if (_cycles == 2)
                {
                    board.Add(_deck.Pop());
                }
                else if (_cycles == 3)
                {
                    board.Add(_deck.Pop());
                }
                else if (_cycles == 4)
                {
                    await endRound(BroadcastStateAsync, BroadcastAsync);
                }

                counter = 0;
                while (!_players[_playerIndex].isPlaying)
                {
                    _playerIndex++;
                    _playerIndex %= _players.Count;
                    counter++;
                    if (counter == _players.Count)
                    {
                        break;
                    }
                }

                return true;
            }
            return false;
        }

        private async Task endRound(Func<Task> BroadcastStateAsync, Func<object, Task> BroadcastAsync)
        {
            await BroadcastStateAsync();
            await Task.Delay(2000);
            List<Player> winners = getWinner();
            foreach(Player p in winners)
            {
                p.Money += Pot / winners.Count;
            }
            
            Pot = 0;
            _starting_cycle_start_index = 0;
            _cycle_start_index = 0;
            await BroadcastStateAsync();
            await Task.Delay(15000);
            Player end = _players[^1];
            _players.RemoveAt(_players.Count - 1);
            _players.Insert(0, end);
            board.Clear();
            startRound();
            await BroadcastAsync(new { type = "update", currentBet = betSize });
            totalRounds++;
        }
        
        private List<Player> getWinner()
        {
            return RankHand.getwinners(_players, board);
        }
    }
}