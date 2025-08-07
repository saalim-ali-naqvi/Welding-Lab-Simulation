using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class WeldingEffect : MonoBehaviour
{
    [Header("Welding Visuals")]
    public GameObject sparkParticleSystemPrefab; // Assign your particle system prefab here
    public GameObject glowingMoltenEffectPrefab; // Assign a glowing material/shader effect prefab
    private GameObject currentSparks;
    private GameObject currentGlow;

    [Header("Welding Audio")]
    public AudioClip weldingSound; // Assign your looping welding sound effect
    private AudioSource audioSource;

    [Header("Welding Logic")]
    public float weldingDistance = 0.1f; // Max distance for welding to occur
    public LayerMask weldableLayer; // Set this in Inspector to a layer for weldable objects
    public float requiredWeldTime = 5.0f; // Time needed to complete welding
    private float currentWeldTime = 0.0f;
    private bool isWeldingActive = false;
    private GameObject currentWeldTarget; // The object currently being welded

    [Header("Completion Feedback")]
    public Material completedWeldMaterial; // Assign a material for the completed weld
    public GameObject completionTextPrefab; // Optional: A text prompt prefab

    // Reference to the ActionBasedController that is interacting with the torch
    private ActionBasedController actionBasedController; // Changed type to ActionBasedController

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.clip = weldingSound;
        audioSource.loop = true; // Welding sound should loop
        audioSource.playOnAwake = false;
    }

    // Called when the torch is picked up
    [System.Obsolete]
    public void OnSelectEnter(SelectEnterEventArgs args)
    {
        // Try to get the ActionBasedController from the interactor
        actionBasedController = args.interactorObject.transform.GetComponent<ActionBasedController>(); // Cast here
        if (actionBasedController == null)
        {
            Debug.LogWarning("WeldingEffect: Could not find ActionBasedController on the interactor. Welding trigger might not work.");
        }
    }

    // Called when the torch is dropped
    public void OnSelectExit(SelectExitEventArgs args)
    {
        StopWelding();
        actionBasedController = null;
    }

    void Update()
    {
        // Only proceed if the torch is being held by an ActionBasedController
        if (actionBasedController != null && actionBasedController.activateAction.action != null) // Check if action is assigned
        {
            // Check if the primary activation button (usually trigger) is pressed
            bool triggerPressed = actionBasedController.activateAction.action.IsPressed();

            if (triggerPressed)
            {
                // Raycast from the torch tip
                RaycastHit hit;
                // Assuming the torch tip is at the transform.position of this script
                if (Physics.Raycast(transform.position, transform.forward, out hit, weldingDistance, weldableLayer))
                {
                    // If we hit a new weldable object, or if we weren't welding before
                    if (hit.collider.gameObject != currentWeldTarget || !isWeldingActive)
                    {
                        // If it's a new target, stop any ongoing welding on the previous target
                        if (currentWeldTarget != null && hit.collider.gameObject != currentWeldTarget)
                        {
                            StopWelding();
                        }
                        currentWeldTarget = hit.collider.gameObject;
                    }

                    if (!isWeldingActive)
                    {
                        StartWelding(hit.point);
                    }

                    // Increment weld time if actively welding on a valid target
                    if (isWeldingActive && currentWeldTarget == hit.collider.gameObject)
                    {
                        currentWeldTime += Time.deltaTime;
                        if (currentWeldTime >= requiredWeldTime)
                        {
                            CompleteWelding(currentWeldTarget);
                        }
                    }
                }
                else
                {
                    // Not hitting a weldable object while trigger is pressed
                    StopWelding();
                }
            }
            else
            {
                // Trigger not pressed, stop welding
                StopWelding();
            }
        }
        else
        {
            // If controller is not an ActionBasedController or action is not assigned, ensure welding is stopped
            StopWelding();
        }
    }

    void StartWelding(Vector3 hitPoint)
    {
        isWeldingActive = true;
        currentWeldTime = 0.0f; // Reset time for new welding session

        // Instantiate particle effects if they don't exist
        if (sparkParticleSystemPrefab != null && currentSparks == null)
        {
            currentSparks = Instantiate(sparkParticleSystemPrefab, hitPoint, Quaternion.identity, transform);
            // Parent to the torch or keep at hitPoint, adjust as needed for visual effect
        }
        if (glowingMoltenEffectPrefab != null && currentGlow == null)
        {
            currentGlow = Instantiate(glowingMoltenEffectPrefab, hitPoint, Quaternion.identity, transform);
        }

        // Play sound if not already playing
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void StopWelding()
    {
        if (!isWeldingActive && currentSparks == null && currentGlow == null && (audioSource == null || !audioSource.isPlaying))
        {
            // Already stopped, nothing to do
            return;
        }

        isWeldingActive = false;
        currentWeldTime = 0.0f; // Reset weld time when stopping

        // Stop and destroy particle effects
        if (currentSparks != null)
        {
            Destroy(currentSparks);
            currentSparks = null;
        }
        if (currentGlow != null)
        {
            Destroy(currentGlow);
            currentGlow = null;
        }

        // Stop sound
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    void CompleteWelding(GameObject weldedObject)
    {
        Debug.Log("Welding Completed on: " + weldedObject.name);
        StopWelding(); // Stop effects after completion

        // Visual Feedback: Change material of the welded area
        // You'll need to identify the specific part of the object to change,
        // or have a separate mesh for the weld.
        Renderer renderer = weldedObject.GetComponent<Renderer>();
        if (renderer != null && completedWeldMaterial != null)
        {
            // If the object has multiple materials, you might need to target a specific one
            // renderer.materials[index] = completedWeldMaterial;
            renderer.material = completedWeldMaterial; // Apply new material to the first material slot
        }

        // Optional: Text Prompt
        if (completionTextPrefab != null)
        {
            GameObject completionText = Instantiate(completionTextPrefab, weldedObject.transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(completionText, 3f); // Destroy after 3 seconds
        }

        // You'll need to define how to replicate the specific experiment from the video.
        // This might involve:
        // - Checking if the torch followed a specific path.
        // - Welding multiple points in sequence.
        // - Triggering a specific animation on the welded piece.
        // This script provides the foundation; you'll extend it based on your video.
    }
}