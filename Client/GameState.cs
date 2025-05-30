using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class GameState
    {
        private Client client_;

        private List<Card> cards_;
        public List<Card> Cards
        {
            get { return cards_; }
            set
            {
                cards_ = value;
                client_.DisplayCards(cards_);
            }
        }

        private Logger logger_;

        // When acting as Giver
        public int? OfferedCardIndex { get; set; }
        public Card OfferedCard { get => Cards[OfferedCardIndex ?? 0]; }
        public List<int> RejectedCardIndices { get; set; }

        public String? ReceiverName, GiverName;

        // When acting as receiver
        public int NumRejections = 0;
        public Card? ReceivedCard { get; set; }

        internal enum Mode
        {
            NotMyTurn,

            // Giver modes
            MakeOffer,
            WaitForReponse,

            // Receiver modes
            AwaitOffer,
            NeedToReply            
        }

        private Mode playerMode_;
        internal Mode PlayerMode
        {
            get { return playerMode_; }
            set
            {
                playerMode_ = value;
                switch (playerMode_)
                {
                    case Mode.NotMyTurn:
                        OfferedCardIndex = null;
                        RejectedCardIndices.Clear();
                        ReceiverName = null;
                        NumRejections = 0;
                        ReceivedCard = null;

                        client_.ActivateNotYourTurnMode();

                        break;

                    case Mode.MakeOffer:
                        client_.ActivateMakeOfferMode(RejectedCardIndices);
                        break;

                    case Mode.WaitForReponse:
                        break;

                    case Mode.AwaitOffer:
                        client_.ActivateAwaitOfferMode();
                        break;

                    case Mode.NeedToReply:
                        client_.ActivateNeedToReplyMode();
                        break;                    
                }
            }
        }

        internal GameState(Client client, Logger logger)
        {
            client_ = client;
            cards_ = new List<Card>();
            RejectedCardIndices = new List<int>();
            logger_ = logger;
        }

        internal void removeCard()
        {
            _ = logger_.Log($"Remove card {OfferedCard} from cards: {Cards}");
            Cards.Remove(OfferedCard);
        }

        internal void AppendCard(Card card)
        {
            _ = logger_.Log($"Add card {card} to cards: {Cards}");
            Cards.Append(card);
        }
    }
}
