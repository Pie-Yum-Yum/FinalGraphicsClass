using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the UI and controls for the spider locomotion system.
/// Handles sliders for step/movement parameters, toggles for modes, and camera switching.
/// </summary>
public class UI : MonoBehaviour
{
    // Sliders for adjusting spider movement parameters
    public Slider StepSpeedSlider, StepHeightSlider, StepDistanceSlider, MoveSpeedSlider, RotationSpeedSlider;
    
    // Toggles for enabling/disabling various features
    public Toggle menuToggle, wallClimb, useNormalsToggle, CarModeToggle, JumpModeToggle, FPOVToggle;
    
    // Text displays for the current slider values
    public TextMeshProUGUI StepSpeedText, StepHeightText, StepDistanceText, MoveSpeedSliderText, RotationSpeedText, tooltipText;
    
    // References to UI and gameplay objects
    public GameObject UIPanel;
    public AimAtPoint AimAtPoint;
    public JumpController spiderController;
    public GameObject Car;
    public GameObject RegularBody;
    private bool TooltipActive = true;
    private bool overviewMain = true;

    // Camera references for POV switching
    public GameObject ThirdPOVCamera, FpovCamera, overviewCamera;

    Vector3 initialCamPos, FPcamPos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Link menu toggle to show/hide the UI panel
        menuToggle.onValueChanged.AddListener(delegate { UIPanel.SetActive(menuToggle.isOn); });
        
        // Link anchor normals toggle to the spider controller
        useNormalsToggle.onValueChanged.AddListener(delegate { spiderController.useAnchorNormals = useNormalsToggle.isOn; });
        
        // Link jump mode toggle to enable/disable jump-to-floor
        JumpModeToggle.onValueChanged.AddListener(delegate { spiderController.enableJumpToFloor = JumpModeToggle.isOn; });
        
        // Initialize sliders with current values from controllers
        MoveSpeedSlider.value = spiderController.moveSpeed;
        RotationSpeedSlider.value = spiderController.rotationSpeed;
        StepSpeedSlider.value = AimAtPoint.stepSpeed;
        StepHeightSlider.value = AimAtPoint.stepHeight;
        StepDistanceSlider.value = AimAtPoint.stepThreshold;

        // Update UI text displays
        updateText();
    }
    /// <summary>
    /// Toggles between car mode and regular spider mode.
    /// </summary>
    public void CarModeToggleChanged()
    {
        if (CarModeToggle.isOn)
        {
            // Show car, hide spider mesh
            Car.SetActive(true);
            RegularBody.GetComponent<MeshRenderer>().enabled = false;
        }
        else
        {
            // Hide car, show spider mesh
            Car.SetActive(false);
            RegularBody.GetComponent<MeshRenderer>().enabled = true;
        }
    }

    /// <summary>
    /// Updates all text displays to show current slider values.
    /// </summary>
    void updateText()
    {
        StepSpeedText.text = StepSpeedSlider.value.ToString("F4");
        StepHeightText.text = StepHeightSlider.value.ToString("F4");
        StepDistanceText.text = StepDistanceSlider.value.ToString("F4");
        MoveSpeedSliderText.text = MoveSpeedSlider.value.ToString("F4");
        RotationSpeedText.text = RotationSpeedSlider.value.ToString("F4");
    }

    /// <summary>
    /// Switches between first-person and third-person camera views.
    /// </summary>
    public void POVchange()
    {
        if(FPOVToggle.isOn)
        {
            // Enable first-person camera
            FpovCamera.SetActive(true);
            ThirdPOVCamera.SetActive(false);
        }
        else
        {
            // Enable third-person camera
            FpovCamera.SetActive(false);
            ThirdPOVCamera.SetActive(true);
        }
    }

    private void switchView(){ // Switches the main camera between overview and POV
        if(overviewMain){
            overviewCamera.GetComponent<Camera>().rect = new Rect(0, 0, 1, 1);
            overviewCamera.GetComponent<Camera>().depth = -1;

            FpovCamera.GetComponent<Camera>().rect = new Rect(0, 0, 0.3f, 0.3f);
            ThirdPOVCamera.GetComponent<Camera>().rect = new Rect(0, 0, 0.3f, 0.3f);
            overviewMain = false;
        }else{
            overviewCamera.GetComponent<Camera>().rect = new Rect(0, 0, 0.3f, 0.3f);
            overviewCamera.GetComponent<Camera>().depth = 1;
            FpovCamera.GetComponent<Camera>().rect = new Rect(0, 0, 1, 1);
            ThirdPOVCamera.GetComponent<Camera>().rect = new Rect(0, 0, 1, 1);
            overviewMain = true;
        }
        
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.F)){
            switchView();
            if(TooltipActive){
                tooltipText.gameObject.SetActive(false);
                TooltipActive = false;
            }
        }
        overviewCamera.transform.LookAt(spiderController.transform);
    }

    /// <summary>
    /// Updates spider movement speed from the slider.
    /// </summary>
    public void moveSpeedUpt()
    {
        spiderController.moveSpeed = MoveSpeedSlider.value;
        updateText();
    }

    /// <summary>
    /// Updates spider rotation speed from the slider.
    /// </summary>
    public void rotationSpeedUpt()
    {
        spiderController.rotationSpeed = RotationSpeedSlider.value;
        updateText();
    }

    /// <summary>
    /// Updates leg step height from the slider.
    /// </summary>
    public void stepHeightUpt()
    {
        AimAtPoint.stepHeight = StepHeightSlider.value;
        updateText();
    }

    /// <summary>
    /// Updates leg step speed from the slider.
    /// </summary>
    public void stepSpeedUpt()
    {
        AimAtPoint.stepSpeed = StepSpeedSlider.value;
        updateText();
    }

    /// <summary>
    /// Updates leg step distance threshold from the slider.
    /// </summary>
    public void stepDistanceUpt()
    {
        AimAtPoint.stepThreshold = StepDistanceSlider.value;
        updateText();
    }

    /// <summary>
    /// Resets the current scene to its initial state.
    /// </summary>
    public void ResetScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
