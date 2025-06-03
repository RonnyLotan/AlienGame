using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class Card 
    {
        public enum Type
        {
            Joker = 0,
            Bear,
            Lion,
            Horse,
            Dog,
            Snake,
            Goat
        }

        static Dictionary<Type, string> AnimalImagesDict = new Dictionary<Type, string>
        {
            { Type.Joker, "joker.png" },
            { Type.Bear, "bear.jpg" },
            { Type.Lion, "lion.jpg" },
            { Type.Horse, "horse.jpg" },
            { Type.Dog, "dog.jpeg" },
            { Type.Snake, "snake.jpg" },
            { Type.Goat, "goat.jpg" },
        };

        public Type Animal;
        public String Name => $"{Animal}";

        public Image Picture { get; init; }

        public Card(Type animal)
        {
            Animal = animal;
            Picture = Image.FromFile($"..\\..\\..\\..\\Shared\\Card Images\\{AnimalImagesDict[Animal]}");
        }

        public static bool TryParse(String str, out Card? card)
        {
            if (Enum.TryParse<Type>(str, out Type animal))
            {
                card = new Card(animal);
                return true;
            }

            card = null;
            return false;
        }

        public override String ToString()
        {
            return Name;
        }

        public static bool operator==(Card a, Card b)
        {
            if (ReferenceEquals(a, b)) return true;

            if (a is null || b is null) return false;

            return a.Name == b.Name;
        }

        // != operator
        public static bool operator !=(Card a, Card b)
        {
            return !(a == b);
        }

        // Override Equals
        public override bool Equals(object? obj)
        {
            if (obj is Card other)
                return this == other;

            return false;
        }        
    }
}
