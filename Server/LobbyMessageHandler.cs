using Shared;
using System.Net.Sockets;

namespace Server
{
    internal class LobbyMessageHandler
    {
        private Logger logger_;
        private Lobby lobby_;

        public LobbyMessageHandler(Lobby lobby, Logger logger)
        {
            lobby_ = lobby;
            logger_ = logger;
        }

        private void log(string text)
        {
            logger_.Log(text);
        }

        public void Handle(CommMessage msg, UserData sender)
        {
            log($"LobbyMessageHandler: Received message {msg.Text} from {sender}");

            if (msg.IsError())
            {
                HandleError(msg, sender);
                return;
            }

            switch (msg.Type)
            {
                case CommMessage.MessageType.BroadcastChat:
                    if (msg is BroadcastChatServerMessage broadcastChatMsg)
                    {
                        // A chat message was received from one of the users. Need to broadcast to everyone else.
                        var response = ReceiveChatClientMessage.Create(sender.Name!, broadcastChatMsg.Msg);

                        log($"Broadcast the chat message to all players except the sender");
                        foreach (var user in lobby_.GetGuestUsers())
                        {
                            if (sender.Name != user.Name)
                            {
                                lobby_.WriteUser(user.Id, response);
                                log($"Sent {response} to player: {user}");
                            }
                        }

                        log($"Sent {CommMessage.MessageType.ReceiveChat} message to all players");

                        return;
                    }
                    break;

                case CommMessage.MessageType.StartGame:
                    if (msg is StartGameServerMessage startGameMsg)
                    {
                        log($"LobbyMessageHandler: Received Start Game messagefrom {sender}");

                        // Start the game only if the host requested it. Otherwise do nothing.
                        if (sender.Name == lobby_.Host)
                        {
                            lobby_.StartGame();
                            log($"LobbyMessageHandler: Starting a game");
                        }

                        return;
                    }
                    break;

                case CommMessage.MessageType.ExitLobbyRequest:
                    if (msg is ExitLobbyRequestServerMessage exitMsg)
                    {
                        lobby_.ExitLobbyRequest(exitMsg.UserName);
                        return;
                    }
                    break;
            }

            // All messages below are ignored if game is not in progress
            Game game;
            lock (lobby_.UpdateLock)
            {
                if (!lobby_.GameInProgress)
                    return;

                game = lobby_.Game;
            }

            switch (msg.Type)
            {
                case CommMessage.MessageType.OfferCard:
                    if (msg is OfferCardServerMessage offerCardMsg)
                    {
                        // A card is offered
                        if (game.State == GameState.WaitForOffer)
                        {
                            // The game is expecting an offer from the giver
                            if (sender.Name == game.Giver.Name)
                            {
                                lobby_.BroadcastGameLogMessage($"{sender.Name} made #{game.NumRejections + 1} offer to {game.Receiver.Name}");

                                // The offer was made by the giver
                                game.OfferedCard = offerCardMsg.Card;

                                var receiver = game.Receiver;
                                var giver = game.Giver;
                                if (game.NumRejections == 2)
                                {
                                    // Send card to receiver, which they are required to take 
                                    var response1 = TakeCardClientMessage.Create(offerCardMsg.Card);
                                    lobby_.WriteUser(receiver.Id, response1);

                                    log($"Sent {CommMessage.MessageType.TakeCard} message to Receiver <{receiver.Name}>");

                                    // Let giver know the offer was accepted
                                    var response2 = ResponseToOfferMessage.Create(true);
                                    lobby_.WriteUser(giver.Id, response2);

                                    log($"Sent {CommMessage.MessageType.ResponseToOffer} message to Giver <{giver.Name}>");

                                    // Advance game state
                                    game.advanceGameState();

                                    if (!lobby_.CheckGameOver())
                                        lobby_.NotifyClientsOfNewTurn();
                                }
                                else
                                {
                                    // Ask receiver if they accept
                                    var response = AcceptCardClientMessage.Create(offerCardMsg.Card, sender.Name);
                                    lobby_.WriteUser(receiver.Id, response);

                                    game.State = GameState.WaitForResponse;

                                    log($"Sent {CommMessage.MessageType.AcceptCard} message to Receiver\ngame: {game}");
                                }
                            }
                            else
                            {
                                var response = NotYourTurnClientMessage.Create();
                                lobby_.WriteUser(sender.Id, response);

                                log($"Sent {CommMessage.MessageType.NotYourTurn} message to sender who is not the Giver\ngame: {game}");
                            }
                        }
                        else
                        {
                            log($"Ignore received {msg.Type} message when in {game.State} game state\ngame: {game}");
                        }
                    }
                    else
                        throw new NotImplementedException();

                    break;

                case CommMessage.MessageType.InterruptGame:
                    if (msg is InterruptGameMessage interruptMsg)
                    {
                        lobby_.NotifyClientsOfInterrupt(interruptMsg);

                        lobby_.EndGame();
                    }
                    break;

                case CommMessage.MessageType.ResponseToOffer:
                    if (msg is ResponseToOfferMessage responseToOfferMsg)
                    {
                        if (game.State == GameState.WaitForResponse)
                        {
                            if (sender.Name == game.Receiver.Name)
                            {
                                if (responseToOfferMsg.Accept)
                                {
                                    log($"Receiver accepted offer\ngame: {game}");

                                    var receiver = game.Receiver;
                                    var giver = game.Giver;
                                    var offeredCard = game.OfferedCard!;

                                    lobby_.BroadcastGameLogMessage($"{receiver.Name} accepted card from {game.Giver.Name}");

                                    game.advanceGameState();

                                    // Let giver know the offer was accepted
                                    lobby_.WriteUser(giver.Id, msg);

                                    // Send card to receiver
                                    var response = TakeCardClientMessage.Create(offeredCard);
                                    lobby_.WriteUser(receiver.Id, response);

                                    log($"Sent {CommMessage.MessageType.TakeCard} message to Receiver\ngame: {game}");

                                    if (!lobby_.CheckGameOver())
                                        lobby_.NotifyClientsOfNewTurn();
                                }
                                else
                                {
                                    log($"Receiver reject offer\ngame: {game}");
                                    game.NumRejections++;
                                    game.State = GameState.WaitForOffer;

                                    var giver = game.Giver;
                                    lobby_.WriteUser(giver.Id, responseToOfferMsg);

                                    lobby_.BroadcastGameLogMessage($"{game.Receiver.Name} rejected card from {game.Giver.Name}");
                                }
                            }
                            else
                            {
                                var response = NotYourTurnClientMessage.Create();
                                lobby_.WriteUser(sender.Id, response);

                                log($"Sent {CommMessage.MessageType.NotYourTurn} message to sender who is not the Receiver\ngame: {game}");
                            }
                        }
                    }
                    break;

                default: throw new NotImplementedException();
            }

        }

        public void HandleError(CommMessage msg, UserData sender)
        {
            log($"ERROR!!!: {msg.Text}");

            if (msg.Type == CommMessage.MessageType.CommunicationError && (msg is CommunicationErrorMessage commError))
            {
                lobby_.HandleUserLeft(lobby_.GetClientHandler(sender.Id), true);
            }
        }
    }
}
