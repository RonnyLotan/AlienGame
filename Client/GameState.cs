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
            init { cards_ = value; }
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
                logger_.Log($"Player mode changing from <{playerMode_}> to <{value}>");
                playerMode_ = value;
                
                OnUpdatePlayerMode();
            }
        }

        private void OnUpdatePlayerMode()
        {
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
                    client_.ActivateAwaitResponseMode();
                    break;

                case Mode.AwaitOffer:
                    client_.ActivateAwaitOfferMode();
                    break;

                case Mode.NeedToReply:
                    client_.ActivateNeedToReplyMode();
                    break;
            }
        }

        internal GameState(Client client, Logger logger, List<Card> cardList)
        {
            client_ = client;
            cards_ = cardList;
            RejectedCardIndices = new List<int>();
            logger_ = logger;
            playerMode_ = Mode.NotMyTurn;
        }

        internal void removeCard()
        {
            _ = logger_.Log($"Remove card {OfferedCard} from cards: {Cards}");
            cards_.RemoveAt(OfferedCardIndex!.Value);
        }

        internal void AppendCard(Card card)
        {
            _ = logger_.Log($"Add card {card} to cards: {string.Join(',',cards_)}");
            cards_.Add(card);
        }
    }
}
