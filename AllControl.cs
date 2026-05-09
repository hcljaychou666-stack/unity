using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllControl : MonoBehaviour
{
    // Start is called before the first frame update
    public class GameManager
    {
        public struct RunSummary
        {
            public int BestCherries;
            public int Deaths;
            public float TimeSeconds;
            public int Rank;

            public RunSummary(int bestCherries, int deaths, float timeSeconds, int rank)
            {
                BestCherries = bestCherries;
                Deaths = deaths;
                TimeSeconds = timeSeconds;
                Rank = rank;
            }
        }

        private const int LeaderboardSize = 5;
        private const string CherryLeaderboardKeyPrefix = "CherryLeaderboard_";

        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if( _instance == null )
                    _instance = new GameManager();
                return _instance;
            }
        }

        public int score = 0;
        public int BestThisRun { get; private set; }

        private bool runStarted;
        private bool submittedCurrentRun;
        private int deathCount;
        private float runStartTime;
        private RunSummary lastRunSummary;

        public void AddScore(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            BeginRunIfNeeded();

            score = Mathf.Max(0, score + amount);
            RecordCurrentScore();
        }

        public void BeginRunIfNeeded()
        {
            if (runStarted && !submittedCurrentRun)
            {
                return;
            }

            BeginNewRun();
        }

        public void ResetScore()
        {
            RecordCurrentScore();
            score = 0;
        }

        public void ResetScoreAfterDeath()
        {
            BeginRunIfNeeded();
            deathCount++;
            ResetScore();
        }

        public RunSummary SubmitRunToLeaderboard()
        {
            if (submittedCurrentRun)
            {
                return lastRunSummary;
            }

            if (!runStarted)
            {
                BeginNewRun();
            }

            RecordCurrentScore();

            int submittedScore = BestThisRun;
            int rank = 0;
            if (submittedScore > 0)
            {
                rank = SaveLeaderboardScore(submittedScore);
            }

            lastRunSummary = new RunSummary(
                submittedScore,
                deathCount,
                Mathf.Max(0f, Time.unscaledTime - runStartTime),
                rank);

            submittedCurrentRun = true;
            score = 0;
            BestThisRun = 0;
            return lastRunSummary;
        }

        public int[] GetLeaderboard()
        {
            int[] scores = new int[LeaderboardSize];

            for (int i = 0; i < LeaderboardSize; i++)
            {
                scores[i] = PlayerPrefs.GetInt(CherryLeaderboardKeyPrefix + i, 0);
            }

            return scores;
        }

        public void BeginNewRun()
        {
            score = 0;
            BestThisRun = 0;
            deathCount = 0;
            runStartTime = Time.unscaledTime;
            runStarted = true;
            submittedCurrentRun = false;
            lastRunSummary = new RunSummary(0, 0, 0f, 0);
        }

        private void RecordCurrentScore()
        {
            BestThisRun = Mathf.Max(BestThisRun, score);
        }

        private int SaveLeaderboardScore(int newScore)
        {
            int[] scores = GetLeaderboard();
            int rank = 0;

            for (int i = 0; i < scores.Length; i++)
            {
                if (newScore < scores[i])
                {
                    continue;
                }

                for (int j = scores.Length - 1; j > i; j--)
                {
                    scores[j] = scores[j - 1];
                }

                scores[i] = newScore;
                rank = i + 1;
                break;
            }

            if (rank == 0)
            {
                return 0;
            }

            for (int i = 0; i < scores.Length; i++)
            {
                PlayerPrefs.SetInt(CherryLeaderboardKeyPrefix + i, scores[i]);
            }

            PlayerPrefs.Save();
            return rank;
        }
    }

    // Update is called once per frame
   
}
