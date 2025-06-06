using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            log($"MessageHandler: Received message <{msg.Text}>");

            switch (msg.Type)
            {
                case CommMessage.MessageType.Login:
                    if (msg is LoginResponseMessage loginResponseMsg)
                    {
                        log($"Received response to login attempt - {loginResponseMsg.Text}");
                        client_.LoginUser(loginResponseMsg.Success, loginResponseMsg.Reason);
                    }
                    break;

                case CommMessage.MessageType.Register:
                    if (msg is RegisterResponseMessage registerResponseMsg)
                    {
                        log($"Recieved response to user registration attempt - {registerResponseMsg.Text}");
                        if (registerResponseMsg.Success)
                        {
                            log($"User registration succeeded");
                            MessageBox.Show($"Registration succeeded - please log in", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            log($"User registration failed");
                            MessageBox.Show($"Registration failed - {registerResponseMsg.Reason}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    break;

                case CommMessage.MessageType.JoinLobby:
                    if (msg is JoinLobbyResponseMessage joinResponseMsg)
                    {
                        log($"Received response to join lobby attempt - {joinResponseMsg.Text}");
                        client_.UserJoinLobby(joinResponseMsg.Success, joinResponseMsg.Host, joinResponseMsg.Reason);
                    }
                    break;

                case CommMessage.MessageType.CreateLobby:
                    if (msg is CreateLobbyResponseMessage createResponseMsg)
                    {
                        log($"Received response to create lobby attempt - {createResponseMsg.Text}");
                        if (createResponseMsg.Success)
                        {
                            log($"Lobby creation succeeded");
                            MessageBox.Show($"Lobby creation succeeded - please log in", "Create Lobby", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            log($"Lobby creation failed");
                            MessageBox.Show($"Lobby creation failed - {createResponseMsg.Reason}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    break;

                case CommMessage.MessageType.DealCards:
                    if (msg is DealCardsClientMessage dealCardsMsg)
                    {
                        client_.StartGame(dealCardsMsg.CardList);
                    }
                    break;

                case CommMessage.MessageType.TakeCard:
                    if (msg is TakeCardClientMessage takeCardMsg)
                    {
                        log($"TakeCard message. taking the card <{takeCardMsg.Card}>");

                        client_.AddAcceptedCard(takeCardMsg.Card);

                        client_.Game.PlayerMode = GameState.Mode.NotMyTurn;
                    }
                    break;

                case CommMessage.MessageType.AcceptCard:
                    if (msg is AcceptCardClientMessage acceptCardMsg)
                    {
                        log($"AcceptCard message. Offered the card <{acceptCardMsg.Card}>");

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
                            var idx = client_.Game.OfferedCardIndex!.Value;
                            client_.Game.OfferedCardIndex = null;
                            client_.Game.RejectedCardIndices.Add(idx);
                            client_.DisableRejectedCard(idx);

                            client_.UpdateStatus($"Please make #{client_.Game.RejectedCardIndices.Count + 1} offer to player {client_.Game.ReceiverName}");
                            client_.Game.PlayerMode = GameState.Mode.MakeOffer;
                        }
                    }
                    break;

                case CommMessage.MessageType.MakeOffer:
                    if (msg is MakeOfferClientMessage makeOfferMsg)
                    {
                        var receiver = makeOfferMsg.Receiver;

                        log($"MakeOffer message. Asked to offer #{makeOfferMsg.Num + 1} card to <{receiver}>");

                        client_.Game.ReceiverName = receiver;
                        client_.UpdateStatus($"Please make #{makeOfferMsg.Num + 1} offer to <{receiver}>");

                        client_.Game.PlayerMode = GameState.Mode.MakeOffer;
                    }
                    break;

                case CommMessage.MessageType.ReceiveOffer:
                    if (msg is ReceiveOfferClientMessage recOfferMsg)
                    {
                        var giver = recOfferMsg.Giver;

                        log($"ReceiveOffer message. Expecting offers from <{giver}>");

                        client_.UpdateStatus($"Expect an offer from <{giver}>");
                        client_.Game.GiverName = giver;

                        client_.Game.PlayerMode = GameState.Mode.AwaitOffer;
                    }
                    break;

                case CommMessage.MessageType.ReceiveChat:
                    if (msg is ReceiveChatClientMessage receiveChatMsg)
                    {
                        log($"ReceiveChat message.The chat message is <{receiveChatMsg.Msg}>");

                        client_.AppendToChat($"{receiveChatMsg.Sender}: ", true, false);
                        client_.AppendToChat($"{receiveChatMsg.Msg}", false, true);
                    }
                    break;

                case CommMessage.MessageType.AnnounceWinner:
                    if (msg is AnnounceWinnerClientMessage winnerMsg)
                    {
                        log($"Game over. The winner is <{winnerMsg.Winner}> and the loser is <{winnerMsg.Loser}>");

                        client_.EndGame(winnerMsg.Winner, winnerMsg.Loser);
                    }
                    break;

                case CommMessage.MessageType.InterruptGame:
                    if (msg is InterruptGameMessage interruptMsg)
                    {
                        log($"Game interrupted by <{interruptMsg.UserName}>");

                        client_.InterruptGame(interruptMsg.UserName);
                    }
                    break;

                case CommMessage.MessageType.GameLog:
                    if (msg is GameLogClientMessage gameLogMsg)
                    {
                        log($"GameLog message. The text is <{gameLogMsg.Msg}>");

                        client_.AppendToGameLog($"{gameLogMsg.Msg}");
                    }
                    break;

                case CommMessage.MessageType.CanStartGame:
                    if (msg is CanStartGameClientMessage canStartMsg)
                    {
                        client_.EnableStartGame(canStartMsg.CanStart);
                    }
                    break;

                case CommMessage.MessageType.LobbyClosing:
                    if (msg is LobbyClosingClientMessage closingMsg)
                    {
                        client_.ExitLobby();
                    }
                    break;

                case CommMessage.MessageType.CommunicationError:
                    if (msg is CommunicationErrorMessage commErrorMsg)
                    {
                        log($"Communication error: {commErrorMsg.Error}");
                        throw new Exception(commErrorMsg.Error);
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
