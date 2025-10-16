
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokerServer.GameLogic.poker
{
    public class Round
    {
        private readonly List<Player> _players;
        private readonly Deck _deck = new();
        private int _dealerIndex = 0;
        private int _playerIndex = 0;
        private int _lastAggressorIndex = -1; // index of player who last raised/bet in this betting round
        private int _street = 0; // 0 preflop, 1 flop, 2 turn, 3 river
        public List<Card> board { get; } = new();
        public int Pot { get; private set; } = 0;
        public int betSize { get; private set; } = 0;
        public int totalRounds { get; private set; } = 0;

        private const int SmallBlind = 10;
        private const int BigBlind = 20;

        public Round(List<Player> players, int dealerIndex = 0)
        {
            _players = players;
            _dealerIndex = dealerIndex % Math.Max(1, players.Count);
        }

        public void startRound()
        {
            RankHand.winner = null;
            Pot = 0;
            betSize = 0;
            board.Clear();
            _deck.Reset();
            _deck.Shuffle();

            // reset player states & deal
            foreach (var p in _players)
            {
                p.Bet = 0;
                p.isPlaying = p.Money > 0;
                if (p.isPlaying)
                    p.setHand(_deck.Pop(), _deck.Pop());
            }

            // remove broke players entirely
            _players.RemoveAll(p => p.Money <= 0 && !p.isPlaying);

            // Post blinds (assumes >= 2 players)
            if (_players.Count < 2) return;

            int sbIndex = NextIndex(_dealerIndex);
            int bbIndex = NextIndex(sbIndex);

            PostBlind(sbIndex, SmallBlind);
            PostBlind(bbIndex, BigBlind);
            betSize = _players[bbIndex].Bet;

            // Preflop: first to act is left of big blind
            _playerIndex = NextIndex(bbIndex);
            _lastAggressorIndex = bbIndex;
            _street = 0;
        }

        private void PostBlind(int idx, int amount)
        {
            var p = _players[idx];
            int bet = Math.Min(p.Money, amount);
            p.Money -= bet;
            p.Bet += bet;
            Pot += bet;
            p.isPlaying = p.Money >= 0; // still active (can be all-in)
        }

        public string getCurrentPlayerId() => _players[_playerIndex].Id;

        private int NextIndex(int i)
        {
            int n = _players.Count;
            return n == 0 ? 0 : (i + 1) % n;
        }

        private void AdvanceToNextActive()
        {
            int n = _players.Count;
            for (int step = 0; step < n; step++)
            {
                _playerIndex = (_playerIndex + 1) % n;
                if (_players[_playerIndex].isPlaying) return;
            }
        }

        public bool doAction(string action, int amount)
        {
            var actor = _players[_playerIndex];
            if (!actor.isPlaying) { AdvanceToNextActive(); return true; }

            switch (action)
            {
                case "fold":
                    actor.isPlaying = false;
                    return MoveAfterAction();
                case "check":
                    // treat as call if there's a live bet against you
                    int toCall = betSize - actor.Bet;
                    if (toCall > 0)
                    {
                        // call
                        int pay = Math.Min(toCall, actor.Money);
                        actor.Money -= pay;
                        actor.Bet += pay;
                        Pot += pay;
                    }
                    return MoveAfterAction();
                case "bet":
                case "raise":
                    int minRaise = Math.Max(BigBlind, betSize); // simplistic: at least current bet (you can refine)
                    int desired = amount;
                    int available = actor.Money;
                    if (desired <= 0 || desired > available) return false;

                    int newBetTotal = actor.Bet + desired;
                    if (newBetTotal <= betSize) return false; // must increase

                    actor.Money -= desired;
                    actor.Bet = newBetTotal;
                    Pot += desired;

                    betSize = newBetTotal;
                    _lastAggressorIndex = _playerIndex;

                    AdvanceToNextActive();
                    return true;
                default:
                    return false;
            }
        }

        private bool MoveAfterAction()
        {
            // If only one player remains, fast-forward to showdown payout
            if (_players.Count(p => p.isPlaying) == 1)
            {
                // Set the lone player as winner via RankHand.winner (used by getWinner)
                RankHand.winner = _players.First(p => p.isPlaying);
                // Force end of betting rounds and trigger endRound on next endCycle
                _street = 3;
                _playerIndex = _lastAggressorIndex; // so endCycle sees round complete
                return true;
            }

            AdvanceToNextActive();
            return true;
        }

        public async Task<bool> endCycle(Func<Task> BroadcastStateAsync, Func<object, Task> BroadcastAsync)
        {
            // betting round ends when the action returns to the last aggressor and
            // all active players have matched betSize (or are all-in)
            bool everyoneMatched = _players.Where(p => p.isPlaying)
                                           .All(p => p.Bet == betSize || p.Money == 0);
            if (_playerIndex == _lastAggressorIndex && everyoneMatched)
            {
                // advance street
                foreach (var p in _players) p.Bet = 0;
                betSize = 0;
                _lastAggressorIndex = -1;

                _street++;
                if (_street == 1)
                {
                    // flop (burn omitted in this simple model)
                    board.Add(_deck.Pop());
                    board.Add(_deck.Pop());
                    board.Add(_deck.Pop());
                }
                else if (_street == 2 || _street == 3)
                {
                    board.Add(_deck.Pop());
                }
                else if (_street >= 4)
                {
                    await endRound(BroadcastStateAsync, BroadcastAsync);
                    return true;
                }

                // Next street: first to act is left of dealer
                _playerIndex = NextIndex(_dealerIndex);
                while (!_players[_playerIndex].isPlaying)
                    _playerIndex = NextIndex(_playerIndex);

                _lastAggressorIndex = _playerIndex; // if everyone checks, round can end when pointer returns here
                return true;
            }

            return false;
        }

        private async Task endRound(Func<Task> BroadcastStateAsync, Func<object, Task> BroadcastAsync)
        {
            await BroadcastStateAsync();
            await Task.Delay(500);

            // If showdown (no early fold), ensure 5 community cards
            while (board.Count < 5) board.Add(_deck.Pop());

            var winners = getWinner();
            int share = winners.Count > 0 ? Pot / winners.Count : 0;
            foreach (var w in winners) w.Money += share;

            Pot = 0;
            await BroadcastStateAsync();
            await Task.Delay(500);

            // Rotate dealer
            _dealerIndex = NextIndex(_dealerIndex);
            totalRounds++;
            await BroadcastAsync(new { type = "update", currentBet = betSize });
            await BroadcastAsync(new { type = "pause" });
        }

        private List<Player> getWinner()
        {
            var contenders = _players.Where(p => p.isPlaying).ToList();
            return RankHand.getwinners(contenders, board);
        }
    }
}
