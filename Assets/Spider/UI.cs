using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public Slider StepSpeedSlider, StepHeightSlider, StepDistanceSlider;
    public Toggle menuToggle, wallClimb, FirstPersonToggle;
    public TextMeshProUGUI StepSpeedText, StepHeightText, StepDistanceText;
    public GameObject UIPanel;
    public AimAtPoint AimAtPoint;
    public SpiderController spiderController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuToggle.onValueChanged.AddListener(delegate { UIPanel.SetActive(menuToggle.isOn); });
        FirstPersonToggle.onValueChanged.AddListener(delegate { spiderController.useAnchorNormals = FirstPersonToggle.isOn; });
        StepSpeedSlider.value = AimAtPoint.stepSpeed;
        StepHeightSlider.value = AimAtPoint.stepHeight;
        StepDistanceSlider.value = AimAtPoint.stepThreshold;

        

        updateText();

        
    }
    void updateText()
    {
        StepSpeedText.text = StepSpeedSlider.value.ToString("F4");
        StepHeightText.text = StepHeightSlider.value.ToString("F4");
        StepDistanceText.text = StepDistanceSlider.value.ToString("F4");
    }

    public void stepHeightUpt()
    {
        AimAtPoint.stepHeight = StepHeightSlider.value;
        updateText();
    }

    public void stepSpeedUpt()
    {
        AimAtPoint.stepSpeed = StepSpeedSlider.value;
        updateText();
    }
    public void stepDistanceUpt()
    {
        AimAtPoint.stepThreshold = StepDistanceSlider.value;
        updateText();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ResetScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
