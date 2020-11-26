using UnityEngine;

public class HighScores : MonoBehaviour
{
    public void SetHighScore(int place, string playerName, int score)
    {
        PlayerPrefs.SetString("player" + place, playerName);
        PlayerPrefs.SetInt("score" + place, score);
    }

    public string GetScoreList()
    {
        string scores = "";
        int place = 1;
        while(PlayerPrefs.HasKey("player" + place))
        {
            scores += place.ToString() + ". " +
                PlayerPrefs.GetString("player" + place) + ": " +
                PlayerPrefs.GetInt("score" + place) + '\n';
            place++;
        }
        return scores;
    }
}