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
            Players = players;

            receiver_ = 1;
            giver_ = 0;

            State = GameState.WaitForOffer;
            NumRejections = 0;

            DealCards();
        }

        public override String ToString()
        {
            return $"Receiver: {Receiver}, Giver: {Giver}, #Rejects: {NumRejections}, State: {State}, Players: {string.Join(';', Players)}";
        }

        public void advanceGameState()
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
            var values = Card.Animal.GetValues(typeof(Card.Animal)).Cast<Card.Animal>().ToList();

            var N = Players.Count;
            var result = new List<Card.Animal>(N * NumCardsPerPlayer + 1);

            // Add each enum value n times
            for (int j = 1; j <= N; j++)
            {
                var val = values[j];
                for (int k = 0; k < NumCardsPerPlayer; k++)
                    result.Add(val);
            }

            result.Add(Card.Animal.Joker);

            // Shuffle the list
            Random rng = new Random();
            result = result.OrderBy(_ => rng.Next()).ToList();

            // Deal the cards to the players
            var n = NumCardsPerPlayer + 1;
            var i = 0;
            foreach (var p in Players)
            {
                var cards = new List<Card>();
                for (int j = 0; j < n; j++)
                {
                    cards.Add(new Card(result[i]));
                    i++;
                }
                
                p.Initialize(cards);

                n = NumCardsPerPlayer;
            }
        }
    }
}
