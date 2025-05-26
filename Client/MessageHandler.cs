using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Client
{
    internal class MessageHandler
    {
        private Client client_;
        private UserData user_;

        public MessageHandler(Client client, UserData user)
        {
            client_ = client;
            user_ = user;
        }

        private void log(string text)
        {
            _ = user_.Logger.Log(text);
        }

        public void Handle(CommMessage msg)
        {
            log($"MessageHandler: Received message {msg.Text}");

            switch (msg.Type)
            {
                case CommMessage.MessageType.DealCards:
                    if (msg is DealCardsClientMessage dealCardsMsg)
                    {
                        client_.Game = new GameState(client_, user_.Logger);

                        log($"Start game. Receive these cards: {dealCardsMsg.CardList}");

                        client_.Game.Cards = dealCardsMsg.CardList;

                        client_.Game.PlayerMode = GameState.Mode.NotMyTurn;
                    }
                    break;

                case CommMessage.MessageType.TakeCard:
                    if (msg is TakeCardClientMessage takeCardMsg)
                    {
                        log($"TakeCard message. taking the card {takeCardMsg.Card}");

                        client_.AddAcceptedCard(takeCardMsg.Card);

                        client_.Game.PlayerMode = GameState.Mode.NotMyTurn;
                    }
                    break;

                case CommMessage.MessageType.AcceptCard:
                    if (msg is AcceptCardClientMessage acceptCardMsg)
                    {
                        log($"AcceptCard message. Offered the card {acceptCardMsg.Card}");

                        client_.Game.ReceivedCard = acceptCardMsg.Card;
                        client_.UpdateStatus($"Do you accept #{client_.Game.NumRejections + 1} card from {acceptCardMsg.Giver}");

                        client_.Game.PlayerMode = GameState.Mode.NeedToReply;
                    }
                    break;

                case CommMessage.MessageType.ResponseToOffer:
                    if (msg is ResponseToOfferMessage responseMsg)
                    {
                        log($"ResponseToOffer message. Received response to my offer: {(responseMsg.Accept ? "Accept" : "Reject")}");
                        if (responseMsg.Accept)
                        {
                            client_.RemoveOfferedCard();
                            client_.Game.PlayerMode = GameState.Mode.NotMyTurn;
                        }
                        else
                        {
                            client_.UpdateStatus($"Please make #{client_.Game.RejectedCardIndices.Count + 1} offer to player {client_.Game.ReceiverName}");
                            client_.Game.PlayerMode = GameState.Mode.MakeOffer;
                        }
                    }
                    break;

                case CommMessage.MessageType.MakeOffer:
                    if (msg is MakeOfferClientMessage makeOfferMsg)
                    {
                        var receiver = makeOfferMsg.Receiver;

                        log($"MakeOffer message. Asked to offer first card to {receiver}");

                        client_.Game.ReceiverName = receiver;
                        client_.UpdateStatus($"Please make #1 offer to player {receiver}");

                        client_.Game.PlayerMode = GameState.Mode.MakeOffer;
                    }
                    break;

                case CommMessage.MessageType.ReceiveChat:
                    if (msg is ReceiveChatClientMessage receiveChatMsg)
                    {
                        log($"ReceiveChat message. The chat message is {receiveChatMsg.Msg}");

                        client_.AppendToChat($"{receiveChatMsg.Sender}:{receiveChatMsg.Msg}");
                    }
                    break;

                case CommMessage.MessageType.GameLog:
                    if (msg is GameLogClientMessage gameLogMsg)
                    {
                        log($"GameLog message. The text is {gameLogMsg.Msg}");

                        client_.AppendToGameLog($"{gameLogMsg.Msg}");
                    }
                    break;

                case CommMessage.MessageType.CanStartGame:
                    if (msg is CanStartGameClientMessage canStartMsg)
                    {
                        client_.EnableStartGame();
                    }
                    break;

                case CommMessage.MessageType.CommunicationError:
                    if (msg is CommunicationErrorMessage commErrorMsg)
                    {
                        log($"Communication error: {commErrorMsg.Error}");
                    }
                    break;

                case CommMessage.MessageType.MessageBodyError:
                    if (msg is MessageBodyErrorMessage bodyErrorMsg)
                    {
                        log($"Message body error: Type:{bodyErrorMsg.MsgType} Body:{bodyErrorMsg.MsgBody}");
                    }
                    break;

                default: throw new NotImplementedException();
            }

        }
    }
}
