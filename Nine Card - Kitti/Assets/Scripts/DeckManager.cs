using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.XR;

public class DeckManager : MonoBehaviour
{
    private List<Card> deck = new List<Card>();

    [SerializeField] private ScoreManager scoreManager;

    [SerializeField] private GameObject cardPrefab;  // Assign the UI card prefab in Inspector
    [SerializeField] private Transform[] playerHands;   // Assign the UI container (PlayerHand)
    [SerializeField] private Transform[] playerBoards;  // Assign the board areas for each player
    [SerializeField] private GameObject playButton;  // Assign the play button in Inspector
    [SerializeField] private GameObject sortingButton;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip drawCardAudio;
    [SerializeField] private AudioClip pickCardAudio;
    [SerializeField] private AudioClip buttonClickSound;


    private bool gameStarted = false; // Prevents multiple draws
    private int currentPhase = 0; // Track the current phase


    void Start()
    {
        CreateDeck();
        ShuffleDeck();
        DealCardsToPlayers(9);

        SortByKittiRules();
    }

    void CreateDeck()
    {
        deck.Clear();
        foreach (Card.SuitType suit in System.Enum.GetValues(typeof(Card.SuitType)))
        {
            foreach (Card.RankType rank in System.Enum.GetValues(typeof(Card.RankType)))
            {
                deck.Add(new Card(suit, rank));
            }
        }
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int randomIndex = Random.Range(0, deck.Count);
            Card temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    void DealCardsToPlayers(int count)
    {
        for (int i = 0; i < count; i++)
        {
            for (int p = 0; p < playerHands.Length; p++) // Loop through each player
            {
                if (deck.Count > 0)
                {
                    Card drawnCard = deck[0];
                    deck.RemoveAt(0);

                    // Create a UI card in the corresponding player's hand
                    GameObject newCard = Instantiate(cardPrefab, playerHands[p]);
                    CardDisplay cardDisplay = newCard.GetComponent<CardDisplay>();
                    cardDisplay.SetCard(drawnCard);

                    // Store the card data for sorting
                    newCard.AddComponent<CardHolder>().CardData = drawnCard;
                }
            }
        }
    }

    public void SortPlayer3Hand()
    {
        ButtonClickSound();
        StartCoroutine(SortCardsWithAnimation(playerHands[2]));
    }

    public void SortByKittiRules()
    {
        for (int i = 0; i < playerHands.Length; i++)
        {
            if (i != 2) // Skip Player 3 (who uses animation)
            {
                List<Card> handCards = playerHands[i].GetComponentsInChildren<CardHolder>().Select(c => c.CardData).ToList();
                List<List<Card>> sortedGroups = SortHandByKittiRules(handCards);

                // Directly update the hand UI for Player 1, 2, and 4 without animation
                // Clear existing cards in the player's hand
                foreach (Transform cardTransform in playerHands[i])
                {
                    Destroy(cardTransform.gameObject);
                }

                // Add sorted cards to the player's hand UI
                foreach (var group in sortedGroups)
                {
                    foreach (var card in group)
                    {
                        GameObject newCard = Instantiate(cardPrefab, playerHands[i]);
                        CardDisplay cardDisplay = newCard.GetComponent<CardDisplay>();
                        cardDisplay.SetCard(card);
                        newCard.AddComponent<CardHolder>().CardData = card;
                    }
                }
            }
        }
    }



    private List<List<Card>> SortHandByKittiRules(List<Card> hand)
    {
        List<List<Card>> sortedGroups = new List<List<Card>>();

        // Rule 1: Trey (Three of a kind, same rank)
        var threeOfAKind = hand.GroupBy(c => c.Rank)
                               .Where(g => g.Count() >= 3)
                               .OrderByDescending(g => g.Key)
                               .Select(g => g.Take(3).ToList())
                               .ToList();
        sortedGroups.AddRange(threeOfAKind);
        hand.RemoveAll(c => threeOfAKind.SelectMany(g => g).Contains(c));

        // Rule 2: Color Run (Three consecutive cards of the same suit)
        var suitGroups = hand.GroupBy(c => c.Suit).Where(g => g.Count() >= 3);
        foreach (var group in suitGroups)
        {
            var sorted = group.OrderByDescending(c => c.Rank).ToList();
            for (int j = 0; j < sorted.Count - 2; j++)
            {
                if (sorted[j].Rank - 1 == sorted[j + 1].Rank && sorted[j + 1].Rank - 1 == sorted[j + 2].Rank)
                {
                    sortedGroups.Add(new List<Card> { sorted[j], sorted[j + 1], sorted[j + 2] });
                    hand.RemoveAll(c => sortedGroups.Last().Contains(c));
                    break;
                }
            }
        }

        // Rule 3: Run (Three consecutive cards of different suits)
        hand = hand.OrderByDescending(c => c.Rank).ToList();
        for (int i = 0; i < hand.Count - 2; i++)
        {
            if (hand[i].Rank - 1 == hand[i + 1].Rank && hand[i + 1].Rank - 1 == hand[i + 2].Rank &&
                hand[i].Suit != hand[i + 1].Suit && hand[i + 1].Suit != hand[i + 2].Suit)
            {
                sortedGroups.Add(new List<Card> { hand[i], hand[i + 1], hand[i + 2] });
                hand.RemoveAll(c => sortedGroups.Last().Contains(c));
                break;
            }
        }

        // Rule 4: Color (Any three cards of the same suit)
        foreach (var group in suitGroups)
        {
            if (group.Count() >= 3)
            {
                sortedGroups.Add(group.OrderByDescending(c => c.Rank).Take(3).ToList());
                hand.RemoveAll(c => sortedGroups.Last().Contains(c));
            }
        }

        // Rule 5: Pair (Two of a kind + one random card)
        var pairs = hand.GroupBy(c => c.Rank).Where(g => g.Count() == 2).ToList();
        if (pairs.Count > 0)
        {
            foreach (var pair in pairs)
            {
                var remainingCard = hand.Except(pair).OrderByDescending(c => c.Rank).FirstOrDefault();
                if (remainingCard != null)
                {
                    sortedGroups.Add(pair.Take(2).Concat(new List<Card> { remainingCard }).ToList());
                    hand.RemoveAll(c => sortedGroups.Last().Contains(c));
                }
            }
        }

        // Rule 6: High Card (Any three remaining cards)
        while (hand.Count >= 3)
        {
            sortedGroups.Add(hand.Take(3).ToList());
            hand = hand.Skip(3).ToList();
        }

        return sortedGroups;
    }


    private IEnumerator SortCardsWithAnimation(Transform playerHand)
    {
        List<RectTransform> cardRects = new List<RectTransform>();

        // Collect all card objects in the player's hand
        foreach (Transform cardTransform in playerHand)
        {
            cardRects.Add(cardTransform as RectTransform);
        }

        // Sort in descending order based on rank
        cardRects.Sort((a, b) =>
        {
            CardHolder cardA = a.GetComponent<CardHolder>();
            CardHolder cardB = b.GetComponent<CardHolder>();

            if (cardA != null && cardB != null)
            {
                return cardB.CardData.Rank.CompareTo(cardA.CardData.Rank); // Descending order
            }
            return 0;
        });

        // Store original positions before sorting
        Dictionary<RectTransform, Vector3> originalPositions = new Dictionary<RectTransform, Vector3>();
        for (int i = 0; i < cardRects.Count; i++)
        {
            originalPositions[cardRects[i]] = cardRects[i].anchoredPosition;
        }

        // Animate movement to sorted positions
        float duration = 0.3f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            for (int i = 0; i < cardRects.Count; i++)
            {
                Vector3 start = originalPositions[cardRects[i]];
                Vector3 target = playerHand.GetChild(i).GetComponent<RectTransform>().anchoredPosition;
                cardRects[i].anchoredPosition = Vector3.Lerp(start, target, elapsedTime / duration);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Set final positions and update sibling order
        for (int i = 0; i < cardRects.Count; i++)
        {
            cardRects[i].anchoredPosition = playerHand.GetChild(i).GetComponent<RectTransform>().anchoredPosition;
            cardRects[i].SetSiblingIndex(i);
        }
    }

    public void StartDrawing()
    {
        if (!gameStarted)
        {
            gameStarted = true;
            playButton.SetActive(false);
            sortingButton.SetActive(false);

            ButtonClickSound();

            Draggable.DisableDragging();
            Debug.Log("Game started! Dragging disabled.");

            currentPhase = 1; // Start with phase 1
            StartCoroutine(DrawCardsSequentially());
        }
    }

    public void ButtonClickSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.clip = buttonClickSound;  // Set the clip
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("AudioSource or AudioClip is missing!");
        }
    }

    public void PlayCardDrawSound()
    {
        if (audioSource != null && drawCardAudio != null)
        {
            audioSource.clip = drawCardAudio;  // Set the clip
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("AudioSource or AudioClip is missing!");
        }
    }

    IEnumerator DrawCardsSequentially()
    {
        int totalPhases = 3;
        int cardsPerPlayer = 3;
        int startingPlayer = 0; // Default to Player 1 (index 0)

        while (currentPhase <= totalPhases)
        {
            yield return new WaitForSeconds(1.0f);

            // Draw cards, starting from the previous winner
            for (int i = 0; i < playerHands.Length; i++)
            {
                int p = (startingPlayer + i) % playerHands.Length; // Circular order

                if (playerHands[p].childCount >= cardsPerPlayer)
                {
                    for (int j = 0; j < cardsPerPlayer; j++)
                    {
                        Transform cardToMove = playerHands[p].GetChild(0);

                        PlayCardDrawSound();

                        StartCoroutine(MoveCardToBoard(cardToMove, playerBoards[p]));

                        Vector3 originalScale = cardToMove.localScale;
                        cardToMove.SetParent(playerBoards[p]);
                        cardToMove.localScale = originalScale;

                        Image cardImage = cardToMove.GetComponentInChildren<Image>();
                        if (cardImage != null)
                        {
                            cardImage.rectTransform.localRotation = Quaternion.identity;
                        }
                    }

                    yield return new WaitForSeconds(1.0f);
                }
            }

            // Determine phase winner
            Dictionary<int, int> phaseScores = scoreManager.EvaluatePhase(playerBoards);
            int winnerIndex = scoreManager.DeterminePhaseWinner(phaseScores, playerBoards);

            // Set the winner as the first drawer for the next phase
            startingPlayer = winnerIndex;

            if (currentPhase < totalPhases)
            {
                yield return StartCoroutine(DestroyCardsOnBoard(winnerIndex));

                currentPhase++;
            }
            else
            {
                yield return StartCoroutine(DestroyCardsOnBoard(winnerIndex));
                scoreManager.DetermineGameWinner();
                yield break;
            }
        }
    }


    IEnumerator MoveCardToBoard(Transform card, Transform targetBoard)
    {
        Vector3 startPosition = card.position;
        Vector3 targetPosition = targetBoard.position;
        Quaternion startRotation = card.rotation;
        Vector3 originalScale = card.localScale; // Store original scale

        float duration = 0.2f;
        float elapsedTime = 0f;

        // Get the layout group and disable it temporarily to prevent scaling changes during move
        HorizontalLayoutGroup layoutGroup = targetBoard.GetComponent<HorizontalLayoutGroup>();
        bool layoutWasEnabled = layoutGroup != null && layoutGroup.enabled;
        if (layoutGroup != null) layoutGroup.enabled = false;

        // Detach from parent without changing world scale
        card.SetParent(null, true);

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            card.position = Vector3.Lerp(startPosition, targetPosition, t);
            card.rotation = Quaternion.Slerp(startRotation, Quaternion.identity, t);
            card.localScale = originalScale; // Ensure scale remains constant
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure exact final position and reset transformations
        card.position = targetPosition;
        card.rotation = Quaternion.identity;
        card.localScale = originalScale; // Ensure scale stays the same

        // Reparent the card to the target board after animation
        card.SetParent(targetBoard, false); // False ensures it follows HLG properly

        // Wait a frame before enabling the layout group again (so it updates)
        yield return null;
        if (layoutGroup != null && layoutWasEnabled) layoutGroup.enabled = true;
    }

    IEnumerator DestroyCardsOnBoard(int winningPlayer)
    {
        yield return new WaitForSeconds(1.0f);

        foreach (Transform board in playerBoards)
        {
            List<GameObject> cardsToDestroy = new List<GameObject>();

            foreach (Transform card in board)
            {
                cardsToDestroy.Add(card.gameObject);
            }

            foreach (GameObject card in cardsToDestroy)
            {
                PlayCardPickupSound();
                StartCoroutine(MoveCardToHandAndDestroy(card.transform, playerHands[winningPlayer]));  
            }
        }

        yield return new WaitForSeconds(1.0f); // Wait for the animation before moving to the next phase
    }

    IEnumerator MoveCardToHandAndDestroy(Transform card, Transform targetHand)
    {
        Vector3 startPosition = card.position;
        Vector3 targetPosition = targetHand.position;
        float duration = 0.3f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            card.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        Destroy(card.gameObject); // Destroy after animation
    }

    public void PlayCardPickupSound()
    {
        if (audioSource != null && pickCardAudio != null)
        {
            audioSource.clip = pickCardAudio;  // Set the clip
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("AudioSource or AudioClip is missing!");
        }
    }
}
