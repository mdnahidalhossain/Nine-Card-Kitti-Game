using UnityEngine;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    public Image cardImage; // Reference to the card's UI Image

    public void SetCard(Card card)
    {
        // Load sprite based on card name
        string spriteName = card.Rank + "_of_" + card.Suit;
        Sprite cardSprite = Resources.Load<Sprite>("Cards/" + spriteName); // Make sure images are in "Resources/Cards"

        if (cardSprite != null)
        {
            cardImage.sprite = cardSprite;
        }
        else
        {
            Debug.LogWarning("Card sprite not found: " + spriteName);
        }
    }
}
