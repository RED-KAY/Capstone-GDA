using System;
using TMPro;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public Transform PlayerTransform => m_PlayerTransform;

    public Vector3 SheenFacilityPosition => m_SheenFacilityTransform.position;
    public Transform SheenFacilityT => m_SheenFacilityTransform;
    public Vector3 PlayerPosition => m_PlayerTransform.position;

    [SerializeField] private Transform m_SheenFacilityTransform;
    [SerializeField] private Transform m_PlayerTransform;

    [SerializeField] private TextMeshProUGUI m_GameOverText;

    [SerializeField] private GameObject m_GameOverPanel;

    [SerializeField] private GameObject m_VictoryPanel;

    [SerializeField] private GameObject m_SettingsPanel;

    private float m_CurrentTimeScale;

    public void ShowGameOver(string message)
    {
        m_GameOverPanel.SetActive(true);
        m_GameOverText.text = message;
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void Restart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    internal void ShowVictoryScreen()
    {
        m_VictoryPanel.SetActive(true);
    }

    public void pause()
    {
        Time.timeScale = Time.timeScale == 0 ? 1 : 0;
    }

    public void ShowSettings()
    {
        m_SettingsPanel.SetActive(true);
        Time.timeScale = 0; // Pause the game when settings are shown
    }

    public void HideSettings()
    {
        m_SettingsPanel.SetActive(false);
        Time.timeScale = 1;
    }
}