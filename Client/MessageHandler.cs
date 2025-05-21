using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class MessageHandler
    {
        private Client client_;
        private UserData user_;

        private int myId_;

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
            log($"ClientMessageHandler: Received message {msg}");

            switch (msg.Type)
            {
                case CommMessage.MessageType.Login:
                    if (msg is LoginResponseMessage loginResponseMsg)
                    {
                        if (loginResponseMsg.Success)
                        {
                            client_.isLoggedIn = true;

                            // Make changes to UI
                            // Connect button change
                            // Enable Join Lobby button
                        }
                        else
                        {
                            client_.isLoggedIn = false;
                            // post failure message
                        }
                    } 
                    break;

                case CommMessage.MessageType.JoinLobby:
                    if (msg is JoinLobbyResponseMessage lobbyResponseMsg)
                    {
                        if (lobbyResponseMsg.Success)
                        {
                            client_.isInLobby = true;

                            // Make changes to UI
                            // Connect button change
                            // Enable Join Lobby button
                        }
                        else
                        {
                            client_.isInLobby = false;
                            // post failure message
                        }
                    }
                    break;

                case CommMessage.MessageType.DealCards:
                    if (msg is DealCardsClientMessage dealCardsMsg)
                    {
                        _ = client_.logger_.Log($"Start game. Receive these cards: {dealCardsMsg.CardList}");

                        client_.Cards = dealCardsMsg.CardList;

                        client_.PlayerMode = Client.Mode.NotMyTurn;                        
                    }
                    break;

                case CommMessage.MessageType.TakeCard:
                    if (msg is TakeCardClientMessage takeCardMsg)
                    {
                        _ = client_.logger_.Log($"TakeCard message. taking the card {takeCardMsg.Card}");

                        client_.AddAcceptedCard(takeCardMsg.Card);

                        client_.PlayerMode = Client.Mode.NotMyTurn;
                    }
                    break;

                case CommMessage.MessageType.AcceptCard:
                    if (msg is AcceptCardClientMessage acceptCardMsg)
                    {
                        _ = client_.logger_.Log($"AcceptCard message. Offered the card {acceptCardMsg.Card}");

                        client_.ReceivedCard = acceptCardMsg.Card;
                        client_.UpdateStatus($"Do you accept #{client_.NumRejections + 1} card from {acceptCardMsg.Giver}");

                        client_.PlayerMode = Client.Mode.NeedToReply;                                               
                    }
                    break;

                case CommMessage.MessageType.ResponseToOffer:
                    if (msg is ResponseToOfferMessage responseMsg)
                    {
                        _ = client_.logger_.Log($"ResponseToOffer message. Received response to my offer: {(responseMsg.Accept ? "Accept" : "Reject")}");
                        if (responseMsg.Accept)
                        {
                            client_.RemoveOfferedCard();
                            client_.PlayerMode = Client.Mode.NotMyTurn;
                        }
                        else
                        {
                            client_.UpdateStatus($"Please make #{client_.RejectedCardIndices.Count + 1} offer to player {client_.ReceiverName}");
                            client_.PlayerMode = Client.Mode.MakeOffer;
                        }

                    }
                    break;

                case CommMessage.MessageType.MakeOffer:
                    if (msg is MakeOfferClientMessage makeOfferMsg)
                    {
                        var receiver = makeOfferMsg.Receiver;

                        _ = client_.logger_.Log($"MakeOffer message. Asked to offer first card to {receiver}");

                        client_.ReceiverName = receiver;
                        client_.UpdateStatus($"Please make #1 offer to player {receiver}");

                        client_.PlayerMode = Client.Mode.MakeOffer;
                    }
                    break;

                case CommMessage.MessageType.ReceiveChat:
                    if (msg is ReceiveChatClientMessage receiveChatMsg)
                    {
                        _ = client_.logger_.Log($"ReceiveChat message. The chat message is {receiveChatMsg.Msg}");

                        client_.AppendToChat($"{receiveChatMsg.Sender}:{receiveChatMsg.Msg}");                        
                    }
                    break;

                case CommMessage.MessageType.GameLog:
                    if (msg is GameLogClientMessage gameLogMsg)
                    {
                        _ = client_.logger_.Log($"GameLog message. The text is {gameLogMsg.Msg}");

                        client_.AppendToGameLog($"{gameLogMsg.Msg}");
                    }
                    break;

                default: throw new NotImplementedException();
            }

        }
    }
}
