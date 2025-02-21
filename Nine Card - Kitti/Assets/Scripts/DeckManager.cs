using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class DeckManager : MonoBehaviour
{
    public GameObject cardPrefab;  // Assign the UI card prefab in Inspector
    public Transform[] playerHands;   // Assign the UI container (PlayerHand)
    public Transform[] playerBoards;  // Assign the board areas for each player
    public GameObject playButton;  // Assign the play button in Inspector
    public GameObject sortingButton;

    private List<Card> deck = new List<Card>();
    private bool gameStarted = false; // Prevents multiple draws
    private int currentPhase = 0; // Track the current phase

    public ScoreManager scoreManager;

    void Start()
    {
        CreateDeck();
        ShuffleDeck();
        DealCardsToPlayers(9);

        playButton.SetActive(true);
        sortingButton.SetActive(true);
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

    public void SortAllHands()
    {
        foreach (Transform playerHand in playerHands)
        {
            StartCoroutine(SortCardsWithAnimation(playerHand));
        }
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
            currentPhase = 1; // Start with phase 1
            StartCoroutine(DrawCardsSequentially());
        }
    }

    IEnumerator DrawCardsSequentially()
    {
        int totalPhases = 3;
        int cardsPerPlayer = 3;

        while (currentPhase <= totalPhases)
        {
            yield return new WaitForSeconds(1.0f);

            for (int p = 0; p < playerHands.Length; p++)
            {
                if (playerHands[p].childCount >= cardsPerPlayer)
                {
                    for (int i = 0; i < cardsPerPlayer; i++)
                    {
                        Transform cardToMove = playerHands[p].GetChild(0);

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

            Dictionary<int, int> phaseScores = scoreManager.EvaluatePhase(playerBoards);
            int winnerIndex = scoreManager.DeterminePhaseWinner(phaseScores, playerBoards);

            if (currentPhase < totalPhases)
            {
                yield return StartCoroutine(DestroyCardsOnBoard(winnerIndex));
                currentPhase++;
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
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

        float duration = 0.5f;
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


}
