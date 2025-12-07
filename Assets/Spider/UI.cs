using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public Slider StepSpeedSlider, StepHeightSlider, StepDistanceSlider, MoveSpeedSlider,RotationSpeedSlider;
    public Toggle menuToggle, wallClimb, useNormalsToggle, CarModeToggle, JumpModeToggle, FPOVToggle;
    public TextMeshProUGUI StepSpeedText, StepHeightText, StepDistanceText, MoveSpeedSliderText, RotationSpeedText;
    public GameObject UIPanel;
    public AimAtPoint AimAtPoint;
    public JumpController spiderController;
    public GameObject Car;
    public GameObject RegularBody;

    public GameObject ThirdPOVCamera, FpovCamera;

    Vector3 initialCamPos, FPcamPos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuToggle.onValueChanged.AddListener(delegate { UIPanel.SetActive(menuToggle.isOn); });
        useNormalsToggle.onValueChanged.AddListener(delegate { spiderController.useAnchorNormals = useNormalsToggle.isOn; });
        JumpModeToggle.onValueChanged.AddListener(delegate { spiderController.enableJumpToFloor = JumpModeToggle.isOn; });
        MoveSpeedSlider.value = spiderController.moveSpeed;
        RotationSpeedSlider.value = spiderController.rotationSpeed;
        StepSpeedSlider.value = AimAtPoint.stepSpeed;
        StepHeightSlider.value = AimAtPoint.stepHeight;
        StepDistanceSlider.value = AimAtPoint.stepThreshold;

        

        updateText();

        
    }
    public void CarModeToggleChanged()
    {
        if (CarModeToggle.isOn)
        {
            Car.SetActive(true);
            RegularBody.GetComponent<MeshRenderer>().enabled = false;
        }
        else
        {
            Car.SetActive(false);
            RegularBody.GetComponent<MeshRenderer>().enabled = true;
        }
    }

    void updateText()
    {
        StepSpeedText.text = StepSpeedSlider.value.ToString("F4");
        StepHeightText.text = StepHeightSlider.value.ToString("F4");
        StepDistanceText.text = StepDistanceSlider.value.ToString("F4");
        MoveSpeedSliderText.text = MoveSpeedSlider.value.ToString("F4");
        RotationSpeedText.text = RotationSpeedSlider.value.ToString("F4");
    }

    public void POVchange()
    {
        if(FPOVToggle.isOn)
        {
            FpovCamera.SetActive(true);
            ThirdPOVCamera.SetActive(false);
            //spiderController.firstPersonView = true;
        }
        else
        {
            FpovCamera.SetActive(false);
            ThirdPOVCamera.SetActive(true);
            //spiderController.firstPersonView = false;
        }
    }

    public void moveSpeedUpt()
    {
        spiderController.moveSpeed = MoveSpeedSlider.value;
        updateText();
    }
    public void rotationSpeedUpt()
    {
        spiderController.rotationSpeed = RotationSpeedSlider.value;
        updateText();
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
