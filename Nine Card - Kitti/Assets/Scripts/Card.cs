using System;
using UnityEngine;

[Serializable]  // Allows this class to be viewed in the Inspector
public class Card
{
    public enum SuitType { Hearts, Diamonds, Clubs, Spades }
    public enum RankType { Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

    public SuitType Suit;
    public RankType Rank;

    public int GetValue()
    {
        // Return a value based on the rank for comparison
        return (int)Rank;
    }

    // Constructor
    public Card(SuitType suit, RankType rank)
    {
        Suit = suit;
        Rank = rank;
    }
}


// Helper script to store card data on the UI GameObject
public class CardHolder : MonoBehaviour
{
    public Card CardData;
}