using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private DeckManager deckManager;

    [HideInInspector] public Transform parentToReturnTo = null;
    [HideInInspector] public Transform placeHolderParent = null;

    private GameObject placeHolder = null;
    private Canvas canvas;
    private RectTransform rectTransform;
    private Vector3 dragOffset; // Offset to fix positioning issue

    public static bool canDrag = true; // Control dragging

    

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>(); // Get the parent canvas
        rectTransform = GetComponent<RectTransform>(); // Cache RectTransform

        // Automatically find and assign DeckManager if not assigned manually
        if (deckManager == null)
        {
            deckManager = FindObjectOfType<DeckManager>();
            if (deckManager == null)
            {
                Debug.LogError("DeckManager not found in the scene! Make sure it is added.");
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag) return;

        placeHolder = new GameObject("PlaceHolder");
        placeHolder.transform.SetParent(this.transform.parent);
        LayoutElement le = placeHolder.AddComponent<LayoutElement>();
        le.preferredWidth = this.GetComponent<LayoutElement>().preferredWidth;
        le.preferredHeight = this.GetComponent<LayoutElement>().preferredHeight;
        le.flexibleWidth = 0;
        le.flexibleHeight = 0;

        placeHolder.transform.SetSiblingIndex(this.transform.GetSiblingIndex());

        parentToReturnTo = this.transform.parent;
        placeHolderParent = parentToReturnTo;
        this.transform.SetParent(this.transform.parent.parent);

        this.GetComponent<CanvasGroup>().blocksRaycasts = false;

        // Calculate initial offset between mouse and card position
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out Vector3 worldMousePos);
        dragOffset = rectTransform.position - worldMousePos; // Store offset
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag) return;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out Vector3 worldMousePos))
        {
            rectTransform.position = worldMousePos + dragOffset; // Apply offset correction
        }

        if (placeHolder.transform.parent != placeHolderParent)
        {
            placeHolder.transform.SetParent(placeHolderParent);
        }

        int newSiblingIndex = placeHolderParent.childCount;

        for (int i = 0; i < placeHolderParent.childCount; i++)
        {
            if (this.transform.position.x < placeHolderParent.GetChild(i).position.x)
            {
                newSiblingIndex = i;

                if (placeHolder.transform.GetSiblingIndex() < newSiblingIndex)
                    newSiblingIndex--;

                break;
            }
        }

        placeHolder.transform.SetSiblingIndex(newSiblingIndex);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag) return;

        this.GetComponent<CanvasGroup>().blocksRaycasts = true;

        // Get final position and sibling index
        Transform finalParent = parentToReturnTo;
        int finalSiblingIndex = placeHolder.transform.GetSiblingIndex();

        // Ensure correct hierarchy before animation
        this.transform.SetParent(finalParent);
        this.transform.SetSiblingIndex(finalSiblingIndex);

        // Animate movement smoothly
        this.transform.DOMove(placeHolder.transform.position, 0.3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                FixLayoutUpdate(finalParent); // Ensure proper layout stacking

                // Check if deckManager is assigned
                if (deckManager != null)
                {
                    deckManager.AnalyzePlayer3Hand();
                }
                else
                {
                    Debug.LogError("deckManager is not assigned!");
                }
            });

        Destroy(placeHolder);
    }



    /// <summary>
    /// Forces Unity's Layout Group to update, preventing stacking issues.
    /// </summary>
    private void FixLayoutUpdate(Transform parent)
    {
        LayoutGroup layoutGroup = parent.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
            layoutGroup.enabled = true;
        }
    }


    public static void DisableDragging()
    {
        canDrag = false;
    }

    public static void EnableDragging()
    {
        canDrag = true;
    }
}
