using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    private Dictionary<int, int> phaseWins = new Dictionary<int, int>(); // Player ID -> Phase Wins

    [SerializeField] private Text[] scoreText;
    [SerializeField] private Text winnerText;
    [SerializeField] private GameObject endGamePanel;

    [SerializeField] private ParticleSystem[] particleVFX;

    public Dictionary<int, int> EvaluatePhase(Transform[] playerBoards)
    {
        Dictionary<int, int> phaseScores = new Dictionary<int, int>();

        for (int i = 0; i < playerBoards.Length; i++)
        {
            List<Card> playerCards = new List<Card>();

            foreach (Transform cardTransform in playerBoards[i])
            {
                CardHolder cardHolder = cardTransform.GetComponent<CardHolder>();
                if (cardHolder != null)
                {
                    playerCards.Add(cardHolder.CardData);
                }
            }

            int score = CalculateScore(playerCards);
            phaseScores[i] = score;
            Debug.Log($"Player {i + 1} Phase Score: {score}");    

        }

        return phaseScores; // Return phase scores to be used in DeterminePhaseWinner()
    }


    private int CalculateScore(List<Card> cards)
    {
        if (cards.Count < 3) return 0; // Invalid hand

        cards.Sort((a, b) => a.Rank.CompareTo(b.Rank));

        if (IsTrey(cards)) return 600;
        if (IsColorRun(cards)) return 500;
        if (IsRun(cards)) return 400;
        if (IsColor(cards)) return 300;
        if (IsPair(cards)) return 200;

        return cards[cards.Count - 1].GetValue(); // Highest card value
    }

    //game rules/combination
    private bool IsTrey(List<Card> cards) => cards[0].Rank == cards[1].Rank && cards[1].Rank == cards[2].Rank;
    private bool IsColorRun(List<Card> cards) => IsRun(cards) && IsColor(cards);
    private bool IsRun(List<Card> cards) => (cards[2].GetValue() - cards[1].GetValue() == 1) && (cards[1].GetValue() - cards[0].GetValue() == 1);
    private bool IsColor(List<Card> cards) => cards[0].Suit == cards[1].Suit && cards[1].Suit == cards[2].Suit;
    private bool IsPair(List<Card> cards) => cards[0].Rank == cards[1].Rank || cards[1].Rank == cards[2].Rank || cards[0].Rank == cards[2].Rank;

    public int DeterminePhaseWinner(Dictionary<int, int> phaseScores, Transform[] playerBoards)
    {
        int maxScore = int.MinValue;
        List<int> tiedPlayers = new List<int>();

        // Step 1: Find the highest score and players with that score
        foreach (var entry in phaseScores)
        {
            if (entry.Value > maxScore)
            {
                maxScore = entry.Value;
                tiedPlayers.Clear();
                tiedPlayers.Add(entry.Key);
            }
            else if (entry.Value == maxScore)
            {
                tiedPlayers.Add(entry.Key);
            }
        }

        // Step 2: If only one player has the max score, they win
        if (tiedPlayers.Count == 1)
        {
            int winner = tiedPlayers[0];

            // Increase win count
            if (!phaseWins.ContainsKey(winner))
                phaseWins[winner] = 0;
            phaseWins[winner]++;

            Debug.Log($"Phase Winner: Player {winner + 1}");

            StartCoroutine(UpdateScoreUI());

            return winner;
        }

        // Step 3: If there is a tie, determine winner based on highest-ranked card
        int winnerByCard = BreakTieByHighestCard(tiedPlayers, playerBoards);

        // Increase win count
        if (!phaseWins.ContainsKey(winnerByCard))
            phaseWins[winnerByCard] = 0;
        phaseWins[winnerByCard]++;

        Debug.Log($"Phase Winner (By Highest Card): Player {winnerByCard + 1}");
        StartCoroutine(UpdateScoreUI());
        return winnerByCard;
    }

    private int BreakTieByHighestCard(List<int> tiedPlayers, Transform[] playerBoards)
    {
        int winningPlayer = -1;
        int highestCardValue = int.MinValue;

        foreach (int playerIndex in tiedPlayers)
        {
            int highestCard = GetHighestCardValue(playerBoards[playerIndex]);

            if (highestCard > highestCardValue)
            {
                highestCardValue = highestCard;
                winningPlayer = playerIndex;
            }
        }

        return winningPlayer;
    }

    private int GetHighestCardValue(Transform playerBoard)
    {
        int maxRank = int.MinValue;

        foreach (Transform cardTransform in playerBoard)
        {
            CardHolder cardHolder = cardTransform.GetComponent<CardHolder>();
            if (cardHolder != null)
            {
                int cardValue = cardHolder.CardData.GetValue();
                if (cardValue > maxRank)
                {
                    maxRank = cardValue;
                }
            }
        }

        return maxRank;
    }

    public void DetermineGameWinner()
    {
        int highestWins = 0;
        List<int> winners = new List<int>();

        foreach (var entry in phaseWins)
        {
            if (entry.Value > highestWins)
            {
                highestWins = entry.Value;
                winners.Clear();
                winners.Add(entry.Key);
            }
            else if (entry.Value == highestWins)
            {
                winners.Add(entry.Key);
            }
        }

        if (winners.Count == 1 && highestWins > 1)
        {
            int winner = winners[0]; // Get the winning player index
            string winnerMessage = "";

            if (winner == 2) // Player-3 (0-based index)
            {
                winnerMessage = "You Win!";
                particleVFX[winner].gameObject.SetActive(true);
                particleVFX[winner].Play();
            }
            else // Player-1, 2, or 4
            {
                winnerMessage = $"Player {winner + 1} Wins!";
                particleVFX[winner].gameObject.SetActive(true);
                particleVFX[winner].Play();
            }

            winnerText.text = winnerMessage;
            endGamePanel.SetActive(true); // Ensure the text is visible
        }
        else
        {
            winnerText.text = "Game is a Draw!";
            endGamePanel.SetActive(true);
        }
    }

    private IEnumerator UpdateScoreUI()
    {
        yield return new WaitForSeconds(1.0f);

        for (int i = 0; i < scoreText.Length; i++)
        {
            if (phaseWins.ContainsKey(i))
            {
                scoreText[i].text = $"{phaseWins[i]} / 3";
            }
            else
            {
                scoreText[i].text = "0 / 3";
            }
        }
    }

}
