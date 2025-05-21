using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class Card 
    {
        public enum Animal
        {
            Joker = 0,
            Bear,
            Lion,
            Horse,
            Dog,
            Snake,
            Goat
        }

        static Dictionary<Animal, string> AnimalImagesDict = new Dictionary<Animal, string>
        {
            { Animal.Joker, "joker.png" },
            { Animal.Bear, "bear.jpg" },
            { Animal.Lion, "lion.jpg" },
            { Animal.Horse, "horse.jpg" },
            { Animal.Dog, "dog.jpeg" },
            { Animal.Snake, "snake.jpg" },
            { Animal.Goat, "goat.jpg" },
        };

        private Animal animal_;
        public String Name => $"{animal_}";

        public Image Picture { get; init; }

        public Card(Animal animal)
        {
            animal_ = animal;
            Picture = Image.FromFile($"..\\..\\..\\..\\Shared\\Card Images\\{AnimalImagesDict[animal_]}");
        }

        public static bool TryParse(String str, out Card? card)
        {
            if (Enum.TryParse<Animal>(str, out Animal animal))
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
