using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }

        private List<Card> cards_;
        public List<Card> Cards { get => cards_; }

        public Player(int id, String name)
        {
            Id = id;
            Name = name;

            cards_ = new List<Card>();
        }

        public void RemoveCard(Card card)
        {
            if (Cards.Count != 5)
                throw new Exception("Player.RemoveCard - There must be exactly 5 card");
            
            if (!Cards.Remove(card))
                throw new Exception($"Player.RemoveCard - the card {card} was not found in this player: {this}");

            return;
        }

        public void AddCard(Card card)
        {
            if (Cards.Count != 4)
                throw new Exception("Player.AddCard - There must be exactly 4 card");

            Cards.Add(card);                

            return;
        }

        public override String ToString() 
        {
            return $"Id: {Id}, Name: {Name}, Cards: {string.Join(',', Cards)}";
        }

        public void Initialize(List<Card> cards)
        {
            cards_ = cards;
        }

    }

        
}
