using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class ARImage : MonoBehaviour
{
    [Header("Scan Line")]
    [SerializeField] Image scanLine;  
    [SerializeField] float scanSpeed = 0.1f;
    bool isImageDetected = false;

    [Header("UI Elements")]
    [SerializeField] Button backButton;
    [SerializeField] Button resetButton;

    [Header("AR Components")]
    ARTrackedImageManager trackedImageManager;
    [SerializeField] ARSession arSession;
    [SerializeField] GameObject[] models;
    Dictionary<string, GameObject> imageToModels = new Dictionary<string, GameObject>(); //Mapping image names to models
    GameObject spawnedModel = null;

    [Header("Model Settings")]
    [SerializeField] float rotationSpeed = 5f;
    [SerializeField] float modelScaleFactor = 0.01f;


    void Start()
    {
        scanLine.gameObject.SetActive(true);
        resetButton.gameObject.SetActive(false);
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        foreach (GameObject model in models)
        {
            imageToModels.Add(model.name, model);
        }
        // Register for image tracking events
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        
        backButton.onClick.AddListener(ChooseMethod);
        resetButton.onClick.AddListener(ResetScene);

    }

    void ResetScene()
    {
        DestroyModel();
        arSession.Reset();
        isImageDetected = false;
        scanLine.gameObject.SetActive(true);
        resetButton.gameObject.SetActive(false);
    }

    void Update()
    {
        RotateModel();
        
        if (!isImageDetected)
        {
            MoveScanLine();
        }
    }

    void MoveScanLine()
    {
        if (scanLine)
        {
            float yPos = Mathf.PingPong(Time.time * scanSpeed, 1) - 0.5f;
            yPos *= 4300f;
            scanLine.rectTransform.anchoredPosition = new Vector2(scanLine.rectTransform.anchoredPosition.x, yPos);
        }
    }

    void RotateModel()
    {
        if (spawnedModel)
        {
            spawnedModel.transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    void ChooseMethod()
    {
        DestroyModel();
        arSession.Reset();
        SceneManager.LoadScene("ChooseMethod");
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach(var trackedImage in args.added)
        {
            // Check if the image name exists in the dictionary
            if (imageToModels.ContainsKey(trackedImage.referenceImage.name))
            {
                // Instantiate the model at the tracked image's position and rotation
                GameObject model = imageToModels[trackedImage.referenceImage.name];
                GameObject instantiatedModel = Instantiate(model, trackedImage.transform.position, trackedImage.transform.rotation);
                instantiatedModel.transform.parent = trackedImage.transform; // Set the parent to the tracked image
                instantiatedModel.transform.localScale = Vector3.one * modelScaleFactor;
                instantiatedModel.transform.localPosition = new Vector3(0, -0.05f, 0); 
                spawnedModel = instantiatedModel;

                isImageDetected = true;
                scanLine.gameObject.SetActive(false);
                resetButton.gameObject.SetActive(true);
            }
        }
    }


    void OnDestroy()
    {
        // Unregister the event when the object is destroyed
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void DestroyModel()
    {
        if (spawnedModel != null)
        {
            Destroy(spawnedModel);
        }
        spawnedModel = null;
    }
}
