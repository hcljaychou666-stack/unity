using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Finish : MonoBehaviour
{
    [SerializeField] private string nextSceneName = string.Empty;

    private AudioSource finishSound;
    private bool levelCompleted;

    private void Start()
    {   
        finishSound = GetComponent<AudioSource>();
    }

    // Update is called once per frame
     private void OnTriggerEnter2D(Collider2D collision)

    {
        if (collision.CompareTag("Player") && !levelCompleted)
        {
            if (finishSound != null)
            {
                finishSound.Play();
            }

            levelCompleted = true;
            Invoke(nameof(CompleteLevel), 2f);
        
        }
    }

    private void CompleteLevel()
    {
        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
