using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shared;

namespace Server
{
    public enum GameState { WaitForOffer, WaitForResponse }
    public class Game
    {
        public static int NumCardsPerPlayer = 4;
        public List<Player> Players { get; set; }
        public Player Receiver => Players[receiver_];
        public Player Giver => Players[giver_];

        private int giver_;
        private int receiver_;

        public Card? OfferedCard;

        public int NumRejections { get; set; }

        public GameState State { get; set; }

        public Game(List<Player> players)
        {
            Players = RandomizeOrder(players);

            receiver_ = 1;
            giver_ = 0;

            State = GameState.WaitForOffer;
            NumRejections = 0;

            DealCards();
        }

        private List<T> RandomizeOrder<T>(IEnumerable<T> source)
        {
            Random rng = new Random();
            return source.OrderBy(_ => rng.Next()).ToList();
        }

        public override String ToString()
        {
            return $"Receiver: {Receiver}, Giver: {Giver}, #Rejects: {NumRejections}, State: {State}, Players: {string.Join(';', Players)}";
        }

        public void AdvanceGameState()
        {
            // move card from giver to receiver 
            if (OfferedCard is not null)
            {
                Giver.RemoveCard(OfferedCard);
                Receiver.AddCard(OfferedCard);
                OfferedCard = null;
            }

            NumRejections = 0;
            giver_ = (giver_ + 1) % Players.Count;
            receiver_ = (receiver_ + 1) % Players.Count;
            State = GameState.WaitForOffer;
        }

        private void DealCards()
        {
            var values = Enum.GetValues(typeof(Card.Type)).Cast<Card.Type>().ToList();

            var N = Players.Count;
            var allCards = new List<Card.Type>(N * NumCardsPerPlayer + 1);

            // Add each enum value n times
            for (int j = 1; j <= N; j++)
            {
                var val = values[j];
                for (int k = 0; k < NumCardsPerPlayer; k++)
                    allCards.Add(val);
            }

            allCards.Add(Card.Type.Joker);

            // Shuffle the list
            allCards = RandomizeOrder(allCards);

            // Deal the cards to the players
            var n = NumCardsPerPlayer + 1;
            var i = 0;
            foreach (var p in Players)
            {
                var cards = new List<Card>();
                for (int j = 0; j < n; j++)
                {
                    cards.Add(new Card(allCards[i]));
                    i++;
                }

                p.Initialize(cards);

                n = NumCardsPerPlayer;
            }
        }

        public Player? DoWeHaveAWinner()
        {
            foreach (var p in Players)
            {
                // A winner is a player that has 4 identical cards
                if (p.Cards.Count == 4)
                {
                    var found = true;
                    for (int i = 0; i < p.Cards.Count - 1; i++)
                    {
                        if (p.Cards[i] != p.Cards[i + 1])
                        { found = false; break; }
                    }

                    if (found)
                        return p;
                }
            }

            return null;
        }

        public Player FindPlayerWithJoker()
        {
            foreach (var p in Players)
            {
                foreach (var card in p.Cards)
                {
                    if (card.Animal == Card.Type.Joker)
                        return p;
                }
            }

            throw new NotImplementedException();
        }
    }
}
