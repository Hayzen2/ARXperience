using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ARSurface : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] TMP_Dropdown modelDropdown;
    [SerializeField] Button backButton;
    [SerializeField] TextMeshProUGUI popupMessage;

    [Header("AR Components")]
    [SerializeField] ARRaycastManager raycastManager;
    [SerializeField] ARPlaneManager planeManager;
    [SerializeField] ARSession arSession;

    [Header("Model Management")]
    [SerializeField] GameObject[] models;
    [SerializeField] float modelScaleFactor = 0.1f;
    [SerializeField] GameObject selectionSignPrefab;

    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 1.0f;
    [SerializeField] float rotationSpeed = 5f;

    [Header("Model State")]
    List<GameObject> spawnedModels = new List<GameObject>();
    GameObject selectedModel;
    List<ARRaycastHit> hits = new List<ARRaycastHit>();
    bool isPlaneDetected = false;
    int pendingModelIndex = -1;
    bool isShowingDetectingMessage = false;
    bool isTracking = true;
    GameObject currentSign;
    Dictionary<GameObject, Vector3> modelToWorldPosition = new Dictionary<GameObject, Vector3>();

    [Header("Sound Effects")]
    [SerializeField] AudioClip[] modelAudioClips;
    AudioSource currentAudioSource;

    [Header("Credit info")]
    [SerializeField] GameObject infoPanel;
    [SerializeField] TextMeshProUGUI infoText;
    [SerializeField] Button infoButton;
    [SerializeField] Button closeInfoButton;
    [SerializeField] [TextArea(3, 10)] string[] modelInfos;

    void Start()
    {
        InitializeComponents();
        SetupEventListeners();
    }


    void InitializeComponents()
    {
        planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        popupMessage.gameObject.SetActive(false);
        infoButton.gameObject.SetActive(false);
        infoPanel.gameObject.SetActive(false);
        ARSession.stateChanged += OnARSessionStateChanged;
        planeManager.planesChanged += OnPlanesChanged;
    }

    void SetupEventListeners()
    {
        backButton.onClick.AddListener(ChooseMethod);
        infoButton.onClick.AddListener(ShowCredit);
        closeInfoButton.onClick.AddListener(CloseInfoPanel);
        modelDropdown.onValueChanged.AddListener(OnModelSelected);
        PopulateDropdown();
    }

    void Update()
    {
        DetectTouch();
    }

    void ShowCredit()
    {
        infoButton.gameObject.SetActive(false);
        if (selectedModel != null)
        {
            infoPanel.SetActive(true);
            int modelIndex = -1;
            for (int i = 0; i < models.Length; i++)
            {
                if (selectedModel.name.StartsWith(models[i].name)) // For (Clone) suffix
                {
                    modelIndex = i;
                    break;
                }
            }

            if (modelIndex != -1 && modelIndex < modelInfos.Length)
            {
                infoText.text = modelInfos[modelIndex];
            }
            else
            {
                infoText.text = "No information available";
            }
        }
       
    }

    void CloseInfoPanel()
    {
        infoPanel.SetActive(false);
        if (selectedModel)
        {
            infoButton.gameObject.SetActive(true);
        }
    }

    void OnARSessionStateChanged(ARSessionStateChangedEventArgs args)
    {
        isTracking = args.state == ARSessionState.SessionTracking;

        if (isTracking && selectedModel != null)
        {
            if (modelToWorldPosition.ContainsKey(selectedModel))
            {
                selectedModel.transform.position = modelToWorldPosition[selectedModel];
            }
            AddSelectionSign(selectedModel);
        }
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        if (args.added.Count > 0)
        {
            isPlaneDetected = true;
            if (isShowingDetectingMessage) HidePopupMessage();
        }
    }

    void PopulateDropdown()
    {
        List<string> modelNames = new List<string> { "Choose model" };
        foreach (GameObject model in models)
        {
            modelNames.Add(model.name);
        }
        modelDropdown.ClearOptions();
        modelDropdown.itemText.fontSize = 40;
        modelDropdown.itemText.enableWordWrapping = true;
        modelDropdown.itemText.fontWeight = FontWeight.Bold;
        modelDropdown.AddOptions(modelNames);
        modelDropdown.value = 0;
    }

    void OnModelSelected(int index)
    {
        if (index == 0) return;
         
        modelDropdown.value = 0; // Reset dropdown first to allow reselection

        pendingModelIndex = index;

        StartCoroutine(ContinuousPlaneCheck());
    }

    IEnumerator ContinuousPlaneCheck()
    {
        ShowPopupMessage("Scanning surface... Move device around", 0);

        while (pendingModelIndex != -1)
        {
            if (isPlaneDetected)
            {
                bool success = PlaceModelImmediately(pendingModelIndex);
                if (success)
                {
                    pendingModelIndex = -1;
                    HidePopupMessage();
                    yield break;
                }
            }
            yield return null;
        }
    }

    bool PlaceModelImmediately(int index)
    {
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            ARPlane hitPlane = planeManager.GetPlane(hits[0].trackableId);

            if (hitPlane.alignment.IsHorizontal())
            {
                GameObject newModel = Instantiate(models[index - 1],
                    hitPose.position,
                    Quaternion.LookRotation(Vector3.ProjectOnPlane(Camera.main.transform.forward, hitPlane.normal)));
                
                
                ConfigureNewModel(newModel);
                spawnedModels.Add(newModel);
                SelectModel(newModel);
                ShowPopupMessage($"Placed: {models[index - 1].name}", 2f);
                return true;
            }
        }

        return false;
    }

    void ConfigureNewModel(GameObject newModel)
    {
        if (!newModel.GetComponent<Collider>())
        {
            MeshCollider collider = newModel.AddComponent<MeshCollider>();
            collider.convex = true;
        }
        foreach (Transform child in newModel.GetComponentsInChildren<Transform>())
        {
            if (!child.GetComponent<Collider>())
            {
                MeshCollider collider = child.gameObject.AddComponent<MeshCollider>();
                collider.convex = true;
            }
        }

        newModel.transform.localScale = Vector3.one * modelScaleFactor;

        modelToWorldPosition[newModel] = newModel.transform.position;

        int modelIndex = -1;
        for (int i = 0; i < models.Length; i++)
        {
            if (newModel.name.StartsWith(models[i].name)) // For (Clone) suffix
            {
                modelIndex = i;
                break;
            }
        }

        if (modelIndex != -1 && modelIndex < modelAudioClips.Length)
        {
            AudioSource audioSource = newModel.AddComponent<AudioSource>();
            audioSource.clip = modelAudioClips[modelIndex];
            audioSource.spatialBlend = 0.8f; // Enable spatial audio
            audioSource.playOnAwake = false;
        }
    }

    void DetectTouch()
    {
        if (Input.touchCount > 0 && !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    GameObject hitObject = FindModelInHierarchy(hit.transform.gameObject);
                    if (hitObject != null) SelectModel(hitObject);
                }
                else
                {
                    DeselectModel();
                }
            }
        }
    }

    GameObject FindModelInHierarchy(GameObject obj)
    {
        Transform current = obj.transform;
        while (current != null)
        {
            if (spawnedModels.Contains(current.gameObject))
            {
                return current.gameObject;
            }
            current = current.parent;
        }

        // If no exact match found, check for parent models 
        current = obj.transform;
        while (current != null)
        {
            foreach (var model in spawnedModels)
            {
                if (current.IsChildOf(model.transform))
                {
                    return model;
                }
            }
            current = current.parent;
        }
        return null;
    }

    public void MoveModelOnPlane(Vector2 input)
    {
        if (selectedModel == null || !isTracking || input.magnitude < 0.01f) return;

        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 cameraRight = Camera.main.transform.right;
        cameraRight.y = 0;
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * input.y + cameraRight * input.x).normalized * moveSpeed;
        Vector3 targetPosition = selectedModel.transform.position + moveDirection * Time.deltaTime;

        UpdateModelPosition(targetPosition);
    }

    void UpdateModelPosition(Vector3 targetPosition)
    {
        if (selectedModel == null) return;

        selectedModel.transform.position = Vector3.Lerp(
            selectedModel.transform.position,
            targetPosition,
            Time.deltaTime * 10f
        );
        modelToWorldPosition[selectedModel] = targetPosition;
    }

    public void RotateModel(float rotationInput)
    {
        if (selectedModel == null || !isTracking) return;

        Vector3 rotationAxis = selectedModel.transform.up;
        selectedModel.transform.Rotate(rotationAxis, rotationInput * rotationSpeed, Space.World);
        modelToWorldPosition[selectedModel] = selectedModel.transform.position;
    }

    void SelectModel(GameObject model)
    {
        if (selectedModel == model)
        {
            PlayModelSound();
            return;
        }

        if (selectedModel != null) DeselectModel();
       
        selectedModel = model;

        infoButton.gameObject.SetActive(true);

        currentAudioSource = selectedModel.GetComponent<AudioSource>();

        AddSelectionSign(model);
        PlayModelSound();
    }

    void PlayModelSound()
    {
        if (currentAudioSource != null && currentAudioSource.clip != null)
        {
            currentAudioSource.Play();
        }
    }

    void DeselectModel()
    {
        if (currentSign != null) Destroy(currentSign);
        infoButton.gameObject.SetActive(false);
        selectedModel = null;
    }

    void AddSelectionSign(GameObject model)
    {
        // Calculate combined bounds of all renderers in hierarchy
        Bounds combinedBounds = new Bounds(model.transform.position, Vector3.zero);
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            combinedBounds.Encapsulate(renderer.bounds);
        }

        // Position sign above the highest point of the combined bounds
        Vector3 signPosition = combinedBounds.center;
        signPosition.y = combinedBounds.max.y + 0.5f;

        currentSign = Instantiate(selectionSignPrefab, signPosition, Quaternion.identity, model.transform); 
        currentSign.transform.localScale = Vector3.one * 0.1f;
    }

    void ShowPopupMessage(string message, float duration)
    {
        popupMessage.text = message;
        popupMessage.gameObject.SetActive(true);
        if (duration > 0) StartCoroutine(HidePopupAfterDelay(duration));
    }

    IEnumerator HidePopupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HidePopupMessage();
    }

    void HidePopupMessage()
    {
        popupMessage.gameObject.SetActive(false);
        isShowingDetectingMessage = false;
    }

    void DestroyAllModels()
    {
        foreach (var model in spawnedModels)
        {
            if (model != null) Destroy(model);
        }
        spawnedModels.Clear();
        modelToWorldPosition.Clear();
        DeselectModel();
    }

    void ChooseMethod()
    {
        DestroyAllModels();
        arSession.Reset();
        SceneManager.LoadScene("ChooseMethod");
    }

    void OnDestroy()
    {
        planeManager.planesChanged -= OnPlanesChanged;
        ARSession.stateChanged -= OnARSessionStateChanged;
    }
}