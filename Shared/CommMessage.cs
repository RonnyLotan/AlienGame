using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Shared
{
    public abstract class CommMessage
    {
        public enum MessageType
        {
            // Messages from server to clients
            DealCards = 100,
            TakeCard = 101,
            AcceptCard = 102,
            MakeOffer = 104,
            ReceiveOffer = 105,
            NotYourTurn = 106,
            ReceiveChat = 107,
            Login = 108,
            JoinLobby = 109,
            GameLog = 110,
            AesKey = 111,
            Register = 112,
            CreateLobby = 113,
            CanStartGame = 114,
            AnnounceWinner = 115,
            LobbyClosing = 116,            

            // Messages from clients to server
            BroadcastChat = 200,
            OfferCard = 201,
            LoginRequest = 202,
            RegisterRequest = 203,
            CreateLobbyRequest = 204,
            JoinLobbyRequest = 205,
            ExitLobbyRequest = 206,
            StartGame = 207,
            Logout = 208,

            // Messages for both directions
            ResponseToOffer = 300,
            InterruptGame = 301,
            PublicKey = 302,
            Encrypted = 303,

            // Errors
            ParseError = 900,
            UnrecognizedMessageTypeError = 901,
            MessageBodyError = 902,
            EncryptionError = 904,
            CommunicationError = 905
        }

        protected enum ResponseToCardOffer
        {
            Accept,
            Reject
        }
        protected enum ResponseToLogin
        {
            Success,
            Failure
        }

        public enum LobbyStatus
        {
            Host,
            Guest
        }

        public abstract MessageType Type { get; }
        public virtual string Text => $"{Type}|";

        public bool isError()
        {
            return (int)Type >= 900;
        }

        public string EncryptedText(string aesKey)
        {
            if (Global.USE_ENCRYPTION)
                return EncryptedMessage.Create(Encryption.AesEncrypt(Text, aesKey)).Text;

            return Text;
        }

        public static CommMessage FromText(string rawMessage, string? aesKey = null)
        {
            string[] splitMsg = rawMessage.Split('|');

            if (splitMsg.Length != 2 || string.IsNullOrEmpty(splitMsg[0]))
                return ParseErrorMessage.Create(rawMessage);

            var type = Regex.Replace(splitMsg[0], @"[^a-zA-Z]", "");
            if (Enum.TryParse(type, out MessageType msgType))
            {
                var msgBody = splitMsg[1];
                switch (msgType)
                {
                    // Messages from server to clients
                    case MessageType.DealCards: return DealCardsClientMessage.FromText(msgBody);
                    case MessageType.TakeCard: return TakeCardClientMessage.FromText(msgBody);
                    case MessageType.AcceptCard: return AcceptCardClientMessage.FromText(msgBody);
                    case MessageType.MakeOffer: return MakeOfferClientMessage.FromText(msgBody);
                    case MessageType.ReceiveOffer: return ReceiveOfferClientMessage.FromText(msgBody);
                    case MessageType.NotYourTurn: return NotYourTurnClientMessage.FromText(msgBody);
                    case MessageType.ReceiveChat: return ReceiveChatClientMessage.FromText(msgBody);
                    case MessageType.Login: return LoginResponseMessage.FromText(msgBody);
                    case MessageType.JoinLobby: return JoinLobbyResponseMessage.FromText(msgBody);
                    case MessageType.GameLog: return GameLogClientMessage.FromText(msgBody);
                    case MessageType.Register: return RegisterResponseMessage.FromText(msgBody);
                    case MessageType.CreateLobby: return CreateLobbyResponseMessage.FromText(msgBody);
                    case MessageType.AesKey: return AesKeyMessage.FromText(msgBody);
                    case MessageType.CanStartGame: return CanStartGameClientMessage.FromText(msgBody);
                    case MessageType.AnnounceWinner: return AnnounceWinnerClientMessage.FromText(msgBody);
                    case MessageType.LobbyClosing: return LobbyClosingClientMessage.FromText(msgBody);                    

                    // Messages from clients to server
                    case MessageType.BroadcastChat: return BroadcastChatServerMessage.FromText(msgBody);
                    case MessageType.OfferCard: return OfferCardServerMessage.FromText(msgBody);
                    case MessageType.LoginRequest: return LoginRequestServerMessage.FromText(msgBody);
                    case MessageType.RegisterRequest: return RegisterRequestServerMessage.FromText(msgBody);
                    case MessageType.JoinLobbyRequest: return JoinLobbyRequestServerMessage.FromText(msgBody);
                    case MessageType.ExitLobbyRequest: return ExitLobbyRequestServerMessage.FromText(msgBody);
                    case MessageType.CreateLobbyRequest: return CreateLobbyRequestServerMessage.FromText(msgBody);
                    case MessageType.StartGame: return StartGameServerMessage.FromText(msgBody);
                    case MessageType.Logout: return LogoutServerMessage.FromText(msgBody);

                    // Messages for both directions
                    case MessageType.ResponseToOffer: return ResponseToOfferMessage.FromText(msgBody);
                    case MessageType.InterruptGame: return InterruptGameMessage.FromText(msgBody);
                    case MessageType.PublicKey: return PublicKeyMessage.FromText(msgBody);

                    case MessageType.Encrypted: return EncryptedMessage.FromText(msgBody, aesKey);

                    default: return UnrecognizedMessageTypeErrorMessage.Create(msgType.ToString(), msgBody);
                }
            }

            return UnrecognizedMessageTypeErrorMessage.Create(splitMsg[0], splitMsg[1]);
        }
    }
    
    // Server sends the client the list of cards the are dealt
    public class DealCardsClientMessage : CommMessage
    {
        private static MessageType type_ = MessageType.DealCards;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(';');

            // Only 4 or 5 cards can be dealt to a single player
            if (splitMsg.Length < 4 || splitMsg.Length > 5)
                return MessageBodyErrorMessage.Create(type_, msgBody);

            var cardList = new List<Card>(splitMsg.Length);
            foreach (string c in splitMsg)
            {
                if (Card.TryParse(c, out Card? card) && card is not null)
                    cardList.Add(card);
                else
                    return MessageBodyErrorMessage.Create(type_, msgBody);
            }

            return Create(cardList);
        }

        static public DealCardsClientMessage Create(List<Card> cardList)
        {
            return new DealCardsClientMessage { CardList = cardList };
        }

        public override MessageType Type => type_;

        public override string Text => base.Text + $"{string.Join(';', CardList)}";

        public required List<Card> CardList { get; init; }
    }

    public abstract class CardMessage : CommMessage
    {
        public CardMessage(Card card)
        {
            Card = card;
        }

        public override string Text => base.Text + $"{Card}";

        public Card Card { get; init; }
    }

    // Message to let client know they are given a new card (which they have to take)
    public class TakeCardClientMessage : CardMessage
    {
        private static MessageType type_ = MessageType.TakeCard;
        public static CommMessage FromText(string msgBody)
        {
            if (Card.TryParse(msgBody, out Card? card) && card is not null)
                return Create(card);

            return MessageBodyErrorMessage.Create(type_, msgBody);
        }

        public static TakeCardClientMessage Create(Card card)
        {
            return new TakeCardClientMessage(card);
        }

        private TakeCardClientMessage(Card card) : base(card)
        {
        }

        public override MessageType Type => type_;
    }

    // Message to let client know they are offered a new card (which they do not have to take)
    public class AcceptCardClientMessage : CardMessage
    {
        private static MessageType type_ = MessageType.AcceptCard;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(';');
            if (splitMsg.Length != 2 || string.IsNullOrEmpty(splitMsg[0]) || string.IsNullOrEmpty(splitMsg[1]))
                return MessageBodyErrorMessage.Create(type_, msgBody);

            var cardName = splitMsg[0];
            var giver = splitMsg[1];

            if (Card.TryParse(cardName, out Card? card) && card is not null)
                return Create(card, giver);

            return MessageBodyErrorMessage.Create(type_, msgBody);
        }

        public static AcceptCardClientMessage Create(Card card, string giver)
        {
            return new AcceptCardClientMessage(card, giver);
        }

        private AcceptCardClientMessage(Card card, string giver) : base(card)
        {
            Giver = giver;
        }

        public override MessageType Type => type_;

        public override string Text => base.Text + $";{Giver}";

        public string Giver;
    }

    // Message to let client know they need to offer one of their cards to the next player
    // The message include the number of the offer (either 1 or 2)
    public class MakeOfferClientMessage : CommMessage
    {
        private static MessageType type_ = MessageType.MakeOffer;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(';');
            if (splitMsg.Length != 2 || string.IsNullOrEmpty(splitMsg[0]) || string.IsNullOrEmpty(splitMsg[1]))
                return MessageBodyErrorMessage.Create(type_, msgBody);

            var numStr = splitMsg[0];
            if (int.TryParse(numStr, out int result))
                return Create(result, splitMsg[1]);

            return MessageBodyErrorMessage.Create(type_, msgBody);
        }

        public static MakeOfferClientMessage Create(int num, string receiver)
        { return new MakeOfferClientMessage(num, receiver); }

        private MakeOfferClientMessage(int num, string receiver)
        {
            Num = num;
            Receiver = receiver;
        }

        public override MessageType Type => type_;

        public override string Text => base.Text + $"{Num};{Receiver}";

        public int Num;
        public string Receiver;
    }

    // Message to let client know they will receive card offers
    public class ReceiveOfferClientMessage : CommMessage
    {
        private static MessageType type_ = MessageType.ReceiveOffer;

        public static CommMessage FromText(string msgBody)
        {
            if (string.IsNullOrEmpty(msgBody))
                return MessageBodyErrorMessage.Create(type_, msgBody);

            var giver = msgBody;
            return Create(giver);
        }

        public static ReceiveOfferClientMessage Create(string giver)
        { return new ReceiveOfferClientMessage(giver); }

        private ReceiveOfferClientMessage(string giver)
        {
            Giver = giver;
        }

        public override MessageType Type => type_;

        public override string Text => base.Text + $"{Giver}";

        public string Giver;
    }

    // Message to let client know they acted out of turn
    public class NotYourTurnClientMessage : CommMessage
    {
        private static MessageType type_ = MessageType.NotYourTurn;

        public static CommMessage FromText(string msgBody)
        {
            if (!string.IsNullOrEmpty(msgBody))
                return MessageBodyErrorMessage.Create(type_, msgBody);

            return new NotYourTurnClientMessage();
        }

        public static NotYourTurnClientMessage Create()
        {
            return new NotYourTurnClientMessage();
        }

        public override MessageType Type => type_;

        public override string Text => base.Text;
    }

    // Message to let client know they are receiving a chat message    
    public class ReceiveChatClientMessage : CommMessage
    {
        private static MessageType type_ = MessageType.ReceiveChat;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(';');

            if (splitMsg.Length != 2 || string.IsNullOrEmpty(splitMsg[0]) || string.IsNullOrEmpty(splitMsg[1]))
                return MessageBodyErrorMessage.Create(type_, msgBody);

            return Create(splitMsg[0], splitMsg[1]);
        }

        public static ReceiveChatClientMessage Create(string sender, string msg)
        {
            return new ReceiveChatClientMessage(sender, msg);
        }

        private ReceiveChatClientMessage(string sender, string msg)
        {
            Sender = sender;
            Msg = msg;
        }
        public override MessageType Type => type_;
        public override string Text => base.Text + $"{Sender};{Msg}";

        public string Sender;
        public string Msg;
    }

    public abstract class ResponseMessage : CommMessage
    {
        protected ResponseMessage(bool success, string? reason)
        {
            Success = success;
            Reason = reason;
        }

        public override string Text => base.Text + $"{(Success ? ResponseToLogin.Success : ResponseToLogin.Failure)}:{(Reason ?? "")}";

        public bool Success { get; init; }
        public string? Reason { get; init; }
    }


    // Message received from server after a login attempt (Success,Fail)
    public class LoginResponseMessage : ResponseMessage
    {
        private static MessageType type_ = MessageType.Login;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(':');

            if (splitMsg.Length != 2 || string.IsNullOrEmpty(splitMsg[0]))
                return MessageBodyErrorMessage.Create(type_, msgBody);

            var status = splitMsg[0];
            if (Enum.TryParse<ResponseToLogin>(status, out ResponseToLogin response))
            {
                if (response == ResponseToLogin.Success)
                    return Create(true, null);
                else if (response == ResponseToLogin.Failure)
                    return Create(false, splitMsg[1]);
            }

            return MessageBodyErrorMessage.Create(type_, msgBody);
        }

        public static LoginResponseMessage Create(bool success, string? reason)
        { return new LoginResponseMessage(success, reason); }

        private LoginResponseMessage(bool success, string? reason) : base(success, reason)
        {
        }

        public override MessageType Type => type_;
    }

    // Message received from server after a user registration attempt (Success,Fail)
    public class RegisterResponseMessage : ResponseMessage
    {
        private static MessageType type_ = MessageType.Register;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(':');

            if (splitMsg.Length != 2 || string.IsNullOrEmpty(splitMsg[0]))
                return MessageBodyErrorMessage.Create(type_, msgBody);

            var status = splitMsg[0];
            if (Enum.TryParse<ResponseToLogin>(status, out ResponseToLogin response))
            {
                if (response == ResponseToLogin.Success)
                    return Create(true, null);
                else if (response == ResponseToLogin.Failure)
                    return Create(false, splitMsg[1]);
            }

            return MessageBodyErrorMessage.Create(type_, msgBody);
        }

        public static RegisterResponseMessage Create(bool success, string? reason)
        { return new RegisterResponseMessage(success, reason); }

        private RegisterResponseMessage(bool success, string? reason) : base(success, reason)
        {
        }

        public override MessageType Type => type_;
    }

    // Message received from server after a lobby creation attempt (Success,Fail)
    public class CreateLobbyResponseMessage : ResponseMessage
    {
        private static MessageType type_ = MessageType.CreateLobby;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(':');

            if (splitMsg.Length != 2 || string.IsNullOrEmpty(splitMsg[0]))
                return MessageBodyErrorMessage.Create(type_, msgBody);

            var status = splitMsg[0];
            if (Enum.TryParse(status, out ResponseToLogin response))
            {
                if (response == ResponseToLogin.Success)
                    return Create(true, null);
                else if (response == ResponseToLogin.Failure)
                    return Create(false, splitMsg[1]);
            }

            return MessageBodyErrorMessage.Create(type_, msgBody);
        }

        public static CreateLobbyResponseMessage Create(bool success, string? reason)
        { return new CreateLobbyResponseMessage(success, reason); }

        private CreateLobbyResponseMessage(bool success, string? reason) : base(success, reason)
        {
        }

        public override MessageType Type => type_;
    }

    // Server lets client know it has joined the lobby successfully, or failed to join with a reason
    public class JoinLobbyResponseMessage : ResponseMessage
    {
        private static MessageType type_ = MessageType.JoinLobby;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(':');

            if (splitMsg.Length != 2 || string.IsNullOrEmpty(splitMsg[0]))
                return MessageBodyErrorMessage.Create(type_, msgBody);

            var status = splitMsg[0];
            if (Enum.TryParse(status, out ResponseToLogin response))
            {
                if (response == ResponseToLogin.Success)
                {
                    var lobbyStatus = splitMsg[1];
                    if (Enum.TryParse(lobbyStatus, out LobbyStatus s))
                        return Create(s == LobbyStatus.Host);
                }
                else if (response == ResponseToLogin.Failure)
                    return Create(splitMsg[1]);
            }

            return MessageBodyErrorMessage.Create(type_, msgBody);
        }

        public static JoinLobbyResponseMessage Create(string reason)
        { return new JoinLobbyResponseMessage(reason); }

        public static JoinLobbyResponseMessage Create(bool host)
        { return new JoinLobbyResponseMessage(host); }

        private JoinLobbyResponseMessage(string? reason) : base(false, reason)
        {
        }

        private JoinLobbyResponseMessage(bool host) : base(true, null)
        {
            Host = host;
        }

        public override MessageType Type => type_;

        public override string Text => base.Text + $"{(Success ? (Host ? LobbyStatus.Host : LobbyStatus.Guest) : "")}";

        public bool Host;
    }

    // Server lets the host know there are enough players in the lobby to start a game
    public class CanStartGameClientMessage : CommMessage
    {
        private static MessageType type_ = MessageType.CanStartGame;

        public static CommMessage FromText(string msgBody)
        {
            var canStart = bool.Parse(msgBody);

            return Create(canStart);
        }

        public static CanStartGameClientMessage Create(bool canStart)
        {
            return new CanStartGameClientMessage(canStart);
        }

        private CanStartGameClientMessage(bool canStart)
        {
            CanStart = canStart;
        }

        public override MessageType Type => type_;
        public override string Text => base.Text + $"{CanStart}";

        public bool CanStart;
    }

    // Server lets all player know there is a winner and a loser. Game is over
    public class AnnounceWinnerClientMessage : CommMessage
    {
        private static MessageType type_ = MessageType.AnnounceWinner;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(';');

            if (splitMsg.Length != 2 || string.IsNullOrEmpty(splitMsg[0]) || string.IsNullOrEmpty(splitMsg[1]))
                return MessageBodyErrorMessage.Create(type_, msgBody);

            var winner = splitMsg[0];
            var loser = splitMsg[1];

            return Create(winner, loser);
        }

        public static AnnounceWinnerClientMessage Create(string winner, string loser)
        {
            return new AnnounceWinnerClientMessage(winner, loser);
        }

        private AnnounceWinnerClientMessage(string winner, string loser) 
        {
            Winner = winner;
            Loser = loser;
        }

        public override MessageType Type => type_;
        public override string Text => base.Text + $"{Winner};{Loser}";

        public string Winner;
        public string Loser;
    }

    // Server letting clients know the lobby is closing
    public class LobbyClosingClientMessage : CommMessage
    {
        private static MessageType type_ = MessageType.LobbyClosing;

        public static CommMessage FromText(string msgBody)
        {
            return Create();
        }

        public static LobbyClosingClientMessage Create()
        {
            return new LobbyClosingClientMessage();
        }

        private LobbyClosingClientMessage()
        {
        }

        public override MessageType Type => type_;
        public override string Text => base.Text;
    }    

    // Message sent by the server to update the game log at the clients
    // Message to let client know they are receiving a chat message    
    public class GameLogClientMessage : CommMessage
    {
        private static MessageType type_ = MessageType.GameLog;

        public static CommMessage FromText(string msgBody)
        {
            return Create(msgBody);
        }

        public static GameLogClientMessage Create(string msg)
        {
            return new GameLogClientMessage(msg);
        }

        private GameLogClientMessage(string msg)
        {
            Msg = msg;
        }

        public override MessageType Type => type_;
        public override string Text => base.Text + $"{Msg}";

        public string Msg;
    }

    // Message to provide the RSA public key   
    public class AesKeyMessage : CommMessage
    {
        private static MessageType type_ = MessageType.AesKey;

        public static CommMessage FromText(string msgBody)
        {
            return Create(msgBody);
        }

        public static AesKeyMessage Create(string key)
        {
            return new AesKeyMessage(key);
        }

        private AesKeyMessage(string key)
        {
            Key = key;
        }

        public override MessageType Type => type_;
        public override string Text => base.Text + $"{Key}";

        public string Key;
    }

    //
    // Messages sent from the clients to the server
    //

    // Message to send the server chat text which should be forwarded to all other players 
    public class BroadcastChatServerMessage : CommMessage
    {
        private static MessageType type_ = MessageType.BroadcastChat;

        public static CommMessage FromText(string msgBody)
        {
            return Create(msgBody);
        }

        public static BroadcastChatServerMessage Create(string msgBody)
        {
            return new BroadcastChatServerMessage(msgBody);
        }

        private BroadcastChatServerMessage(string msg)
        {
            Msg = msg;
        }

        public override MessageType Type => type_;

        public override string Text => base.Text + Msg;

        public string Msg { get; init; }
    }

    // Message to send the server the card a player is offering to give
    public class OfferCardServerMessage : CardMessage
    {
        private static MessageType type_ = MessageType.OfferCard;

        public static CommMessage FromText(string msgBody)
        {
            if (Card.TryParse(msgBody, out Card? card) && card is not null)
                return new OfferCardServerMessage(card);

            return MessageBodyErrorMessage.Create(type_, msgBody);
        }

        public static OfferCardServerMessage Create(Card card)
        {
            return new OfferCardServerMessage(card);
        }

        private OfferCardServerMessage(Card card) : base(card)
        {
        }

        public override MessageType Type => type_;
    }

    // Attempt to log in to the server
    public class LoginRequestServerMessage : CommMessage
    {
        private static MessageType type_ = MessageType.LoginRequest;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(':');
            if (splitMsg.Length != 2 || string.IsNullOrEmpty(splitMsg[0]) || string.IsNullOrEmpty(splitMsg[1]))
                return MessageBodyErrorMessage.Create(type_, msgBody);

            var userName = splitMsg[0];
            var password = splitMsg[1];

            return Create(userName, password);
        }

        public static LoginRequestServerMessage Create(string userName, string pwd)
        {
            return new LoginRequestServerMessage(userName, pwd);
        }

        private LoginRequestServerMessage(string userName, string pwd)
        {
            UserName = userName;
            Password = pwd;
        }

        public override MessageType Type => type_;

        public override string Text => base.Text + $"{UserName}:{Password}";

        public string UserName { get; init; }
        public string Password { get; init; }
    }

    // Attempt to register a new user on the server
    public class RegisterRequestServerMessage : CommMessage
    {
        private static MessageType type_ = MessageType.RegisterRequest;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(':');
            if (splitMsg.Length != 3 ||
                string.IsNullOrEmpty(splitMsg[0]) ||
                string.IsNullOrEmpty(splitMsg[1]) ||
                string.IsNullOrEmpty(splitMsg[2]))
            {
                return MessageBodyErrorMessage.Create(type_, msgBody);
            }

            var userName = splitMsg[0];
            var password = splitMsg[1];
            var email = splitMsg[2];

            return Create(userName, password, email);
        }

        public static RegisterRequestServerMessage Create(string userName, string pwd, string email)
        {
            return new RegisterRequestServerMessage(userName, pwd, email);
        }

        private RegisterRequestServerMessage(string userName, string pwd, string email)
        {
            UserName = userName;
            Password = pwd;
            Email = email;
        }

        public override MessageType Type => type_;

        public override string Text => base.Text + $"{UserName}:{Password}:{Email}";

        public string UserName { get; init; }
        public string Password { get; init; }
        public string Email { get; init; }
    }

    public abstract class LobbyRequestServerMessage : CommMessage
    {
        protected LobbyRequestServerMessage(string name, string code)
        {
            Name = name;
            EntryCode = code;
        }

        public override string Text => base.Text + $"{Name}:{EntryCode}";

        public string Name { get; init; }
        public string EntryCode { get; init; }
    }

    // Attempt to create a new lobby
    public class CreateLobbyRequestServerMessage : LobbyRequestServerMessage
    {
        private static MessageType type_ = MessageType.CreateLobbyRequest;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(':');
            if (splitMsg.Length != 2 ||
                string.IsNullOrEmpty(splitMsg[0]) ||
                string.IsNullOrEmpty(splitMsg[1]))
            {
                return MessageBodyErrorMessage.Create(type_, msgBody);
            }

            var name = splitMsg[0];
            var password = splitMsg[1];

            return Create(name, password);
        }

        public static CreateLobbyRequestServerMessage Create(string name, string pwd)
        {
            return new CreateLobbyRequestServerMessage(name, pwd);
        }

        private CreateLobbyRequestServerMessage(string name, string pwd) : base(name, pwd)
        {
        }

        public override MessageType Type => type_;
    }

    // Message sent to the server to request entry into a lobby
    public class JoinLobbyRequestServerMessage : LobbyRequestServerMessage
    {
        private static MessageType type_ = MessageType.JoinLobbyRequest;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(':');
            if (splitMsg.Length != 2 ||
                string.IsNullOrEmpty(splitMsg[0]) ||
                string.IsNullOrEmpty(splitMsg[1]))
            {
                return MessageBodyErrorMessage.Create(type_, msgBody);
            }

            var name = splitMsg[0];
            var entryCode = splitMsg[1];

            return Create(name, entryCode);
        }

        public static JoinLobbyRequestServerMessage Create(string name, string code)
        {
            return new JoinLobbyRequestServerMessage(name, code);
        }

        private JoinLobbyRequestServerMessage(string name, string code) : base(name, code)
        {
        }

        public override MessageType Type => type_;
    }

    // Message sent to server to request to get out of the lobby
    public class ExitLobbyRequestServerMessage : CommMessage
    {
        private static MessageType type_ = MessageType.ExitLobbyRequest;

        public static CommMessage FromText(string msgBody)
        {
            var userName = msgBody;
            return Create(userName);
        }

        public static ExitLobbyRequestServerMessage Create(string userName)
        { return new ExitLobbyRequestServerMessage(userName); }

        private ExitLobbyRequestServerMessage(string userName)
        {
            UserName = userName;    
        }

        public override MessageType Type => type_;

        public override string Text => base.Text + UserName;

        public string UserName;
    }

    // Tell the server to start the game
    public class StartGameServerMessage : CommMessage
    {
        private static MessageType type_ = MessageType.StartGame;

        public static CommMessage FromText(string msgBody)
        {
            return Create();
        }

        public static StartGameServerMessage Create()
        { return new StartGameServerMessage(); }

        private StartGameServerMessage()
        {
        }

        public override MessageType Type => type_;
    }

    // Client letting the server know it is logging out.
    public class LogoutServerMessage : CommMessage
    {
        private static MessageType type_ = MessageType.Logout;

        public static CommMessage FromText(string msgBody)
        {
            return Create();
        }

        public static LogoutServerMessage Create()
        {
            return new LogoutServerMessage();
        }

        private LogoutServerMessage()
        {
        }

        public override MessageType Type => type_;        
    }

    //
    // Messages that could be sent by either Server or Clients
    //

    // Message to let server know the receiver's response to the card offer
    public class ResponseToOfferMessage : CommMessage
    {
        private static MessageType type_ = MessageType.ResponseToOffer;

        public static CommMessage FromText(string msgBody)
        {
            if (Enum.TryParse(msgBody, out ResponseToCardOffer response))
            {
                if (response == ResponseToCardOffer.Accept)
                    return Create(true);
                else if (response == ResponseToCardOffer.Reject)
                    return Create(false);
            }

            return MessageBodyErrorMessage.Create(type_, msgBody);
        }

        public static ResponseToOfferMessage Create(bool accept)
        { return new ResponseToOfferMessage(accept); }

        private ResponseToOfferMessage(bool accept)
        {
            Accept = accept;
        }

        public override MessageType Type => type_;

        public override string Text => base.Text + $"{(Accept ? ResponseToCardOffer.Accept : ResponseToCardOffer.Reject)}";

        public bool Accept { get; init; }
    }

    // Tell the server or the clients that a user has interrupted the game
    public class InterruptGameMessage : CommMessage
    {
        private static MessageType type_ = MessageType.InterruptGame;

        public static CommMessage FromText(string msgBody)
        {
            string userName = msgBody;

            return Create(userName);
        }

        public static InterruptGameMessage Create(string userName)
        { return new InterruptGameMessage(userName); }

        private InterruptGameMessage(string userName)
        {
            UserName = userName;
        }

        public override MessageType Type => type_;

        public override string Text => base.Text + UserName;

        public string UserName;
    }

    // Message to provide the RSA public key   
    public class PublicKeyMessage : CommMessage
    {
        private static MessageType type_ = MessageType.PublicKey;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(';');
            if (splitMsg.Length != 2 ||
                string.IsNullOrEmpty(splitMsg[0]) ||
                string.IsNullOrEmpty(splitMsg[1]))
            {
                return MessageBodyErrorMessage.Create(type_, msgBody);
            }

            var key = splitMsg[0];

            if (int.TryParse(splitMsg[1], out int id))
                return Create(key, id);

            return MessageBodyErrorMessage.Create(type_, msgBody);
        }

        public static PublicKeyMessage Create(string key, int Id)
        {
            return new PublicKeyMessage(key, Id);
        }

        private PublicKeyMessage(string key, int id)
        {
            Key = key;
            Id = id;
        }

        public override MessageType Type => type_;
        public override string Text => base.Text + $"{Key};{Id}";

        public string Key;
        public int Id;
    }

    // An encrypted message 
    public class EncryptedMessage : CommMessage
    {
        private static MessageType type_ = MessageType.PublicKey;

        public static new CommMessage FromText(string msgBody, string? aesKey)
        {
            if (aesKey is not null)
            {
                var decryptedContent = Encryption.AesDecrypt(msgBody, aesKey);
                return CommMessage.FromText(decryptedContent);
            }

            return EncryptionErrorMessage.Create($"No AES key was provided");
        }

        public static EncryptedMessage Create(string content)
        {
            return new EncryptedMessage(content);
        }

        private EncryptedMessage(string content)
        {
            Content = content;
        }

        public override MessageType Type => type_;
        public override string Text => base.Text + $"{Content}";

        public string Content;
    }

    //
    // Error Messages
    //

    // Message to report a parsing error of the received message
    public class ParseErrorMessage : CommMessage
    {
        private static MessageType type_ = MessageType.ParseError;

        public static CommMessage FromText(string msgBody)
        {
            return Create(msgBody);
        }

        public static ParseErrorMessage Create(string rawMessage)
        {
            return new ParseErrorMessage(rawMessage);
        }

        private ParseErrorMessage(string rawMessage)
        {
            rawMessage_ = rawMessage;
        }

        public override MessageType Type => type_;

        public override string Text => base.Text + rawMessage_;

        private string rawMessage_;
    }

    // Message to report that an unrecognized message type was received
    public class UnrecognizedMessageTypeErrorMessage : CommMessage
    {
        private static MessageType type_ = MessageType.UnrecognizedMessageTypeError;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(';');

            if (splitMsg.Length != 2)
                return MessageBodyErrorMessage.Create(type_, msgBody);

            return Create(splitMsg[0], splitMsg[1]);
        }

        public static UnrecognizedMessageTypeErrorMessage Create(string type, string msgBody)
        {
            return new UnrecognizedMessageTypeErrorMessage(type, msgBody);
        }

        private UnrecognizedMessageTypeErrorMessage(string type, string msgBody)
        {
            unknownType_ = type;
            msgBody_ = msgBody;
        }

        public override MessageType Type => type_;

        public override string Text => base.Text + $"{unknownType_};{msgBody_}";

        private string unknownType_;
        private string msgBody_;
    }

    // Message to report that the body of the message received could not be parsed
    public class MessageBodyErrorMessage : CommMessage
    {
        private static MessageType type_ = MessageType.MessageBodyError;

        public static CommMessage FromText(string msgBody)
        {
            string[] splitMsg = msgBody.Split(';');

            if (splitMsg.Length != 2)
                return MessageBodyErrorMessage.Create(type_, msgBody);

            if (Enum.TryParse<MessageType>(splitMsg[0], out MessageType msgType))
                return new MessageBodyErrorMessage(msgType, splitMsg[1]);

            return UnrecognizedMessageTypeErrorMessage.Create(splitMsg[0], splitMsg[1]);
        }

        public static MessageBodyErrorMessage Create(MessageType type, string msgBody)
        {
            return new MessageBodyErrorMessage(type, msgBody);
        }

        private MessageBodyErrorMessage(MessageType type, string msgBody)
        {
            MsgType = type;
            MsgBody = msgBody;
        }
        public override MessageType Type => type_;

        public override string Text => base.Text + $"{MsgType};{MsgBody}";

        public MessageType MsgType;
        public string MsgBody;
    }

    // Message to report a parsing error of the received message
    public class EncryptionErrorMessage : CommMessage
    {
        private static MessageType type_ = MessageType.EncryptionError;

        public static CommMessage FromText(string msgBody)
        {
            return Create(msgBody);
        }

        public static EncryptionErrorMessage Create(string reason)
        {
            return new EncryptionErrorMessage(reason);
        }

        private EncryptionErrorMessage(string reason)
        {
            reason_ = reason;
        }

        public override MessageType Type => type_;

        public override string Text => base.Text + reason_;

        private string reason_;
    }

    // Message to report a communication error
    public class CommunicationErrorMessage : CommMessage
    {
        private static MessageType type_ = MessageType.CommunicationError;

        public static CommMessage FromText(string msgBody)
        {
            return Create(msgBody);
        }

        public static CommunicationErrorMessage Create(string error)
        {
            return new CommunicationErrorMessage(error);
        }

        private CommunicationErrorMessage(string error)
        {
            Error = error;
        }

        public override MessageType Type => type_;

        public override string Text => base.Text + Error;

        public string Error;
    }
}
