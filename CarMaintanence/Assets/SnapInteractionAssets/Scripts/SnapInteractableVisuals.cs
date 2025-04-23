using Oculus.Interaction;
using System;
using System.Collections;
using UnityEngine;

public class SnapInteractableVisuals : MonoBehaviour
{
    [SerializeField] private SnapInteractable snapInteractable;
    [SerializeField] private Material hoverMaterial;

    private GameObject currentInteractorGameObject;
    private SnapInteractor currentInteractor;
    private Coroutine cleanupCoroutine;

    private void OnEnable()
    {
        if (snapInteractable == null)
        {
            Debug.LogError("SnapInteractable reference is missing!", this);
            return;
        }

        snapInteractable.WhenInteractorAdded.Action += HandleInteractorAdded;
        snapInteractable.WhenInteractorRemoved.Action += HandleInteractorRemoved;
        snapInteractable.WhenSelectingInteractorViewAdded += HandleSelectingInteractorViewAdded;
        snapInteractable.WhenInteractorViewAdded += HandleInteractorViewAdded;
        snapInteractable.WhenInteractorViewRemoved += HandleInteractorViewRemoved;
    }

    private void OnDisable()
    {
        if (snapInteractable == null) return;

        snapInteractable.WhenInteractorAdded.Action -= HandleInteractorAdded;
        snapInteractable.WhenInteractorRemoved.Action -= HandleInteractorRemoved;
        snapInteractable.WhenSelectingInteractorViewAdded -= HandleSelectingInteractorViewAdded;
        snapInteractable.WhenInteractorViewAdded -= HandleInteractorViewAdded;
        snapInteractable.WhenInteractorViewRemoved -= HandleInteractorViewRemoved;

        CleanupGhostModel();
    }

    private void HandleInteractorAdded(SnapInteractor interactor)
    {
        if (currentInteractor == interactor) return;

        // Clean up current ghost model if it exists
        CleanupGhostModel();

        // Set new interactor and create its ghost model
        currentInteractor = interactor;
        SetupGhostModel(interactor);
    }

    private void HandleInteractorRemoved(SnapInteractor interactor)
    {
        if (currentInteractor != interactor) return;

        // Delay cleanup to ensure all events have processed
        if (cleanupCoroutine != null)
        {
            StopCoroutine(cleanupCoroutine);
        }
        cleanupCoroutine = StartCoroutine(DelayedCleanup());
    }

    private IEnumerator DelayedCleanup()
    {
        yield return null; // Wait one frame
        CleanupGhostModel();
        cleanupCoroutine = null;
    }

    private void HandleSelectingInteractorViewAdded(IInteractorView interactorView)
    {
        if (currentInteractorGameObject != null)
        {
            currentInteractorGameObject.SetActive(false);
        }
    }

    private void HandleInteractorViewAdded(IInteractorView interactorView)
    {
        // Only activate if it's our tracked interactor
        if (IsRelevantInteractorView(interactorView) && currentInteractorGameObject != null)
        {
            currentInteractorGameObject.SetActive(true);
        }
    }

    private void HandleInteractorViewRemoved(IInteractorView interactorView)
    {
        // Only deactivate if it's our tracked interactor
        if (IsRelevantInteractorView(interactorView) && currentInteractorGameObject != null)
        {
            currentInteractorGameObject.SetActive(false);
        }
    }

    private bool IsRelevantInteractorView(IInteractorView interactorView)
    {
        // Check if this interactor view belongs to our current interactor
        if (currentInteractor == null) return false;

        // Try to cast to SnapInteractor
        SnapInteractor snapInteractor = interactorView as SnapInteractor;
        return snapInteractor == currentInteractor;
    }

    private void SetupGhostModel(SnapInteractor interactor)
    {
        if (interactor == null || interactor.transform.parent == null) return;

        // Create main ghost GameObject
        Transform interactorParent = interactor.transform.parent;
        currentInteractorGameObject = new GameObject(interactorParent.name + "_Ghost");
        currentInteractorGameObject.transform.SetParent(transform, false);

        // Match the transform to the interactable's expected pose
        // (This may need adjustment based on your specific setup)
        currentInteractorGameObject.transform.localPosition = Vector3.zero;
        currentInteractorGameObject.transform.localRotation = Quaternion.identity;
        currentInteractorGameObject.transform.localScale = interactorParent.localScale;

        // Create a ghost representation by copying mesh hierarchy
        CopyMeshRecursively(interactorParent, currentInteractorGameObject.transform);
    }

    private void CopyMeshRecursively(Transform source, Transform target)
    {
        // Add mesh from the current object if it has one
        MeshFilter sourceMeshFilter = source.GetComponent<MeshFilter>();
        if (sourceMeshFilter != null && sourceMeshFilter.mesh != null)
        {
            MeshFilter targetMeshFilter = target.gameObject.AddComponent<MeshFilter>();
            targetMeshFilter.mesh = sourceMeshFilter.mesh;

            MeshRenderer targetRenderer = target.gameObject.AddComponent<MeshRenderer>();
            targetRenderer.material = hoverMaterial;
        }

        // Process all children
        foreach (Transform child in source)
        {
            // Skip the interactor itself to avoid infinite recursion or incorrect mesh nesting
            if (child.GetComponent<SnapInteractor>() != null)
                continue;

            GameObject childCopy = new GameObject(child.name);
            childCopy.transform.SetParent(target, false);
            childCopy.transform.localPosition = child.localPosition;
            childCopy.transform.localRotation = child.localRotation;
            childCopy.transform.localScale = child.localScale;

            // Recursively process this child
            CopyMeshRecursively(child, childCopy.transform);
        }
    }

    private void CleanupGhostModel()
    {
        if (currentInteractorGameObject != null)
        {
            Destroy(currentInteractorGameObject);
            currentInteractorGameObject = null;
        }
        currentInteractor = null;
    }
}