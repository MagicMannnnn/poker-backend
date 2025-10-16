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
        private int _playerIndex = 0;          // whose turn
        private int _street = 0;               // 0 pre, 1 flop, 2 turn, 3 river
        private int _lastAggressorIndex = -1;  // last player who bet/raised this street

        // NEW: who still needs to act this street
        private readonly HashSet<int> _toAct = new();

        public List<Card> board { get; } = new();
        public int Pot { get; private set; } = 0;
        public int betSize { get; private set; } = 0; // current max bet to match (per player Bet)
        public int totalRounds { get; private set; } = 0;

        private int SmallBlind = 10;
        private int BigBlind = 20;

        public Round(List<Player> players, int dealerIndex = 0)
        {
            _players = players;
            _dealerIndex = players.Count == 0 ? 0 : (dealerIndex % players.Count + players.Count) % players.Count;
        }

        public void startRound()
        {
            RankHand.winner = null;
            Pot = 0;
            betSize = 0;
            board.Clear();
            _deck.Reset();
            _deck.Shuffle();

            foreach (var p in _players)
            {
                p.Bet = 0;
                p.isPlaying = p.Money > 0;
                if (p.isPlaying)
                    p.setHand(_deck.Pop(), _deck.Pop());
            }

            _players.RemoveAll(p => p.Money <= 0 && !p.isPlaying);
            if (_players.Count < 2) return;

            SmallBlind = SmallBlind + ((int)Math.Floor((float)(totalRounds / _players.Count)) * 10);
            BigBlind = BigBlind + ((int)Math.Floor((float)(totalRounds / _players.Count)) * 20);
            // Post blinds
            int sb = NextIndex(_dealerIndex);
            int bb = NextIndex(sb);
            PostBlind(sb, SmallBlind);
            PostBlind(bb, BigBlind);
            betSize = _players[bb].Bet;

            // Preflop: first to act is left of big blind
            _playerIndex = NextIndex(bb);
            while (!_players[_playerIndex].isPlaying) _playerIndex = NextIndex(_playerIndex);

            _street = 0;
            _lastAggressorIndex = bb;

            // Everyone EXCEPT last aggressor must respond to the live bet
            ResetToActAllActiveExcept(_lastAggressorIndex);
        }

        private void PostBlind(int idx, int amount)
        {
            var p = _players[idx];
            int pay = Math.Min(amount, p.Money);
            p.Money -= pay;
            p.Bet += pay;
            Pot += pay;
            // if they’re all-in from posting, remove from _toAct later when we build it
        }

        public string getCurrentPlayerId() => _players[_playerIndex].Id;

        private int NextIndex(int i) => _players.Count == 0 ? 0 : (i + 1) % _players.Count;

        private void AdvanceToNextActive()
        {
            if (_players.Count == 0) return;
            int n = _players.Count;
            for (int step = 0; step < n; step++)
            {
                _playerIndex = (_playerIndex + 1) % n;
                if (_players[_playerIndex].isPlaying) return;
            }
        }

        // ==== ACTIONS =========================================================

        public bool doAction(string action, int amount)
        {
            var actor = _players[_playerIndex];
            if (!actor.isPlaying)
            {
                AdvanceToNextActive();
                return true;
            }

            switch (action)
            {
                case "fold":
                    actor.isPlaying = false;
                    _toAct.Remove(_playerIndex);
                    return AfterActionContinue();

                case "check":
                {
                    int toCall = betSize - actor.Bet;
                    if (toCall > 0)
                    {
                        // Treat as call if there is a live bet (UI might send "check" when it means "call")
                        int pay = Math.Min(toCall, actor.Money);
                        actor.Money -= pay;
                        actor.Bet += pay;
                        Pot += pay;
                        if (actor.Money == 0) RemoveAllInFromToAct(_playerIndex);
                    }
                    _toAct.Remove(_playerIndex);
                    return AfterActionContinue();
                }

                case "call":
                {
                    int toCall = Math.Max(0, betSize - actor.Bet);
                    if (toCall == 0)
                    {
                        _toAct.Remove(_playerIndex);
                        return AfterActionContinue();
                    }

                    int pay = Math.Min(toCall, actor.Money);
                    actor.Money -= pay;
                    actor.Bet += pay;
                    Pot += pay;
                    if (actor.Money == 0) RemoveAllInFromToAct(_playerIndex);

                    _toAct.Remove(_playerIndex);
                    return AfterActionContinue();
                }

                case "bet":
                case "raise":
                {
                    // amount is the add-on, not the total
                    if (amount <= 0 || amount > actor.Money) return false;

                    actor.Money -= amount;
                    actor.Bet += amount;
                    Pot += amount;

                    // Must exceed previous bet level
                    if (actor.Bet <= betSize) return false;

                    betSize = actor.Bet;
                    _lastAggressorIndex = _playerIndex;

                    // When someone bets/raises, EVERY other active player must respond again
                    ResetToActAllActiveExcept(_lastAggressorIndex);

                    if (actor.Money == 0) RemoveAllInFromToAct(_playerIndex);

                    AdvanceToNextActive();
                    return true;
                }

                default:
                    return false;
            }
        }

        // ==== END OF CYCLE / STREET ==========================================

        public async Task<bool> endCycle(Func<Task> BroadcastStateAsync, Func<object, Task> BroadcastAsync)
        {
            // If only one player remains -> award pot
            if (_players.Count(p => p.isPlaying) == 1)
            {
                RankHand.winner = _players.First(p => p.isPlaying);
                await EndRound(BroadcastStateAsync, BroadcastAsync);
                return true;
            }

            // Street ends ONLY when everyone who needs to act has acted
            if (_toAct.Count == 0)
            {
                // Advance street
                foreach (var p in _players) p.Bet = 0;
                betSize = 0;
                _lastAggressorIndex = -1;
                _street++;

                if (_street == 1)
                {
                    // Flop
                    board.Add(_deck.Pop()); board.Add(_deck.Pop()); board.Add(_deck.Pop());
                }
                else if (_street == 2 || _street == 3)
                {
                    board.Add(_deck.Pop()); // Turn / River
                }
                else // _street >= 4 -> showdown
                {
                    await EndRound(BroadcastStateAsync, BroadcastAsync);
                    return true;
                }

                // Next street: first to act is left of dealer
                _playerIndex = NextIndex(_dealerIndex);
                while (!_players[_playerIndex].isPlaying) _playerIndex = NextIndex(_playerIndex);

                // No aggressor yet; everyone has to act at least once
                ResetToActAllActiveExcept(exceptIndex: -1);
                return true;
            }

            return false;
        }

        // ==== HELPERS =========================================================

        private bool AfterActionContinue()
        {
            // If only one player remains, let endCycle finish the round
            if (_players.Count(p => p.isPlaying) <= 1) return true;

            AdvanceToNextActive();
            return true;
        }

        private void ResetToActAllActiveExcept(int exceptIndex)
        {
            _toAct.Clear();
            for (int i = 0; i < _players.Count; i++)
            {
                if (!_players[i].isPlaying) continue;
                if (i == exceptIndex) continue;   // the bettor/raiser doesn’t need to re-act
                // If player is already all-in, they don’t need to act either
                if (_players[i].Money == 0) continue;
                _toAct.Add(i);
            }
        }

        private void RemoveAllInFromToAct(int idx)
        {
            if (idx >= 0) _toAct.Remove(idx);
        }

        private async Task EndRound(Func<Task> BroadcastStateAsync, Func<object, Task> BroadcastAsync)
        {
            await BroadcastStateAsync();
            await Task.Delay(250);

            // Make sure board has 5 for showdown
            while (board.Count < 5) board.Add(_deck.Pop());

            var winners = getWinner();
            int share = winners.Count > 0 ? Pot / winners.Count : 0;
            foreach (var w in winners) w.Money += share;

            Pot = 0;
            _playerIndex = _players.IndexOf(winners[0]);
            await BroadcastStateAsync();
            await Task.Delay(250);

            // Rotate dealer & pause
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
