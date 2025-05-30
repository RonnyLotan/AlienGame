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

        public void Handle(CommMessage msg, UserData sender)
        {
            _ = logger_.Log($"LobbyMessageHandler: Received message {msg.Text} from {sender}");

            if (msg.Type == CommMessage.MessageType.BroadcastChat && (msg is BroadcastChatServerMessage broadcastChatMsg))
            {
                // A chat message was received from one of the users. Need to broadcast to everyone else.
                var response = ReceiveChatClientMessage.Create(sender.Name!, broadcastChatMsg.Msg);

                _ = logger_.Log($"Broadcast the chat message to all players except the sender");
                foreach (var user in lobby_.getGuestUsers())
                {
                    if (sender.Name != user.Name)
                    {
                        lobby_.WriteUser(user.Id, response);
                        _ = logger_.Log($"Sent {response} to player: {user}");
                    }
                }

                _ = logger_.Log($"Sent {CommMessage.MessageType.ReceiveChat} message to all players");

                return;
            }
            else if (msg.Type == CommMessage.MessageType.StartGame && (msg is StartGameServerMessage startGameMsg))
            {
                _ = logger_.Log($"LobbyMessageHandler: Received Start Game messagefrom {sender}");

                // Start the game only if the host requested it. Otherwise do nothing.
                if (sender.Name == lobby_.Host)
                {
                    lobby_.StartGame();
                    _ = logger_.Log($"LobbyMessageHandler: Starting a game");
                }

                return;
            }

            // All messages below are ignored if game is not in progress
            Game game;
            lock (lobby_.UpdateLock)
            {
                if (!lobby_.GameInProgress)
                    return;

                game = lobby_.Game!;
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
                                lobby_.broadcastGameLogMessage($"{sender.Name} made #{game.NumRejections + 1} offer to {game.Receiver.Name}");

                                // The offer was made by the giver
                                game.OfferedCard = offerCardMsg.Card;

                                var receiver = game.Receiver;
                                if (game.NumRejections == 2)
                                {
                                    // Send card to receiver, which they are required to take 
                                    var response = TakeCardClientMessage.Create(offerCardMsg.Card);
                                    lobby_.WriteUser(receiver.Id, response);

                                    // Advance game state
                                    game.advanceGameState();

                                    _ = logger_.Log($"Sent {CommMessage.MessageType.TakeCard} message to Receiver\ngame: {game}");
                                }
                                else
                                {
                                    // Ask receiver if they accept
                                    var response = AcceptCardClientMessage.Create(offerCardMsg.Card, sender.Name);
                                    lobby_.WriteUser(receiver.Id, response);

                                    game.State = GameState.WaitForResponse;

                                    _ = logger_.Log($"Sent {CommMessage.MessageType.AcceptCard} message to Receiver\ngame: {game}");
                                }
                            }
                            else
                            {
                                var response = NotYourTurnClientMessage.Create();
                                lobby_.WriteUser(sender.Id, response);

                                _ = logger_.Log($"Sent {CommMessage.MessageType.NotYourTurn} message to sender who is not the Giver\ngame: {game}");
                            }
                        }
                        else
                        {
                            _ = logger_.Log($"Ignore received {msg.Type} message when in {game.State} game state\ngame: {game}");
                        }
                    }
                    else
                        throw new NotImplementedException();

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
                                    _ = logger_.Log($"Receiver accepted offer\ngame: {game}");

                                    var receiver = game.Receiver;

                                    lobby_.broadcastGameLogMessage($"{receiver.Name} accepted card from {game.Giver.Name}");

                                    game.advanceGameState();

                                    var response = TakeCardClientMessage.Create(game.OfferedCard!);
                                    lobby_.WriteUser(receiver.Id, response);

                                    _ = logger_.Log($"Sent {CommMessage.MessageType.TakeCard} message to Receiver\ngame: {game}");
                                }
                                else
                                {
                                    _ = logger_.Log($"Receiver reject offer\ngame: {game}");
                                    game.NumRejections++;
                                    game.State = GameState.WaitForOffer;

                                    var giver = game.Giver;
                                    lobby_.WriteUser(giver.Id, responseToOfferMsg);

                                    lobby_.broadcastGameLogMessage($"{game.Receiver.Name} rejected card from {game.Giver.Name}");
                                }
                            }
                            else
                            {
                                var response = NotYourTurnClientMessage.Create();
                                lobby_.WriteUser(sender.Id, response);

                                _ = logger_.Log($"Sent {CommMessage.MessageType.NotYourTurn} message to sender who is not the Receiver\ngame: {game}");
                            }
                        }
                    }
                    break;

                case CommMessage.MessageType.CommunicationError:
                    if (msg is CommunicationErrorMessage commErrorMsg)
                    {
                        _ = logger_.Log($"Communication error: {commErrorMsg.Error}");
                    }
                    break;

                default: throw new NotImplementedException();
            }

        }
    }
}
