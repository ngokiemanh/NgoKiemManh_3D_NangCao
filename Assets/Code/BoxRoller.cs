using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class BoxRoller : MonoBehaviour
{
    public float rollDuration = 0.2f;
    public Transform startPoint;
    public GameObject canvasWin;
    public GameObject canvasLose;

    public List<GameObject> pathObjectsA = new List<GameObject>();
    public List<GameObject> pathObjectsB = new List<GameObject>();

    private Dictionary<GameObject, Vector3> originalPositionsA = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Vector3> originalPositionsB = new Dictionary<GameObject, Vector3>();

    private bool hasTouchedPointA = false;
    private bool hasTouchedPointB = false;

    private bool isRolling = false;
    private BoxCollider boxCollider;
    private bool hasWon = false;

    void Start()
    {
        if (startPoint != null)
        {
            transform.position = RoundVector3(startPoint.position);
            transform.rotation = Quaternion.identity;
        }

        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
            boxCollider = gameObject.AddComponent<BoxCollider>();

        boxCollider.isTrigger = false;

        if (canvasWin != null) canvasWin.SetActive(false);
        if (canvasLose != null) canvasLose.SetActive(false);

        InitPathObjects(pathObjectsA, originalPositionsA);
        InitPathObjects(pathObjectsB, originalPositionsB);
    }

    void InitPathObjects(List<GameObject> list, Dictionary<GameObject, Vector3> dict)
    {
        foreach (GameObject obj in list)
        {
            if (obj != null)
            {
                dict[obj] = obj.transform.position;
                obj.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (isRolling || hasWon) return;

        CheckStandingOnPointB();
        CheckStandingOnWin();
        CheckStandingOnBlockX();
    }

    public void MoveUp() => TryMove(Vector3.forward, Vector3.right);
    public void MoveDown() => TryMove(Vector3.back, Vector3.left);
    public void MoveLeft() => TryMove(Vector3.left, Vector3.forward);
    public void MoveRight() => TryMove(Vector3.right, Vector3.back);

    void TryMove(Vector3 direction, Vector3 axis)
    {
        if (!isRolling && !hasWon)
        {
           
            GameManager.Instance?.AddScore(1);
            StartCoroutine(Roll(direction, axis));
        }
    }

    IEnumerator Roll(Vector3 direction, Vector3 rotationAxis)
    {
        isRolling = true;

        float halfHeight = transform.localScale.x / 2f;
        Vector3 offset = direction * 0.5f;
        offset.y = -halfHeight;
        Vector3 pivot = transform.position + offset;

        GameObject pivotGO = new GameObject("Pivot");
        pivotGO.transform.position = pivot;
        transform.SetParent(pivotGO.transform);

        pivotGO.transform.DORotate(rotationAxis * 90f, rollDuration, RotateMode.WorldAxisAdd)
            .SetEase(Ease.OutCubic);

        yield return new WaitForSeconds(rollDuration);

        transform.SetParent(null);
        Destroy(pivotGO);

        transform.position = RoundVector3(transform.position);
        transform.rotation = Quaternion.Euler(
            Mathf.Round(transform.eulerAngles.x / 90f) * 90f,
            Mathf.Round(transform.eulerAngles.y / 90f) * 90f,
            Mathf.Round(transform.eulerAngles.z / 90f) * 90f
        );

        isRolling = false;

        CheckFallOrWin();
    }

    void CheckFallOrWin()
    {
        if (transform.localScale.z > 1f || transform.localScale.x > 1f)
        {
            Vector3 dir = (transform.localScale.z > 1f) ? transform.forward : transform.right;
            Vector3 center = transform.position;
            Vector3 p1 = center + dir * 0.5f;
            Vector3 p2 = center - dir * 0.5f;

            bool p1OnGround = Physics.Raycast(p1 + Vector3.up * 0.1f, Vector3.down, 1.2f);
            bool p2OnGround = Physics.Raycast(p2 + Vector3.up * 0.1f, Vector3.down, 1.2f);

            if ((p1OnGround ? 1 : 0) + (p2OnGround ? 1 : 0) < 2)
            {
                HandleFall();
            }
            else
            {
                boxCollider.isTrigger = false;
            }
        }
        else
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 1.2f) &&
                LayerMask.LayerToName(hit.collider.gameObject.layer) == "Win" &&
                (Mathf.Approximately(transform.eulerAngles.x, 90f) || Mathf.Approximately(transform.eulerAngles.x, 270f)))
            {
                boxCollider.isTrigger = true;
                StartCoroutine(ShowCanvasWinAfterDelay(0.2f));
            }
            else
            {
                boxCollider.isTrigger = false;
            }
        }
    }

    void HandleFall()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 1.2f) &&
            LayerMask.LayerToName(hit.collider.gameObject.layer) == "Win" &&
            (Mathf.Approximately(transform.eulerAngles.x, 90f) || Mathf.Approximately(transform.eulerAngles.x, 270f)))
        {
            boxCollider.isTrigger = true;
            StartCoroutine(ShowCanvasWinAfterDelay(0.2f));
            return;
        }

        boxCollider.isTrigger = true;
        StartCoroutine(ShowCanvasLoseAfterDelay(0.2f));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PointA"))
        {
            TogglePath(pathObjectsA, originalPositionsA, ref hasTouchedPointA);
        }
        else if (other.CompareTag("PointB"))
        {
            float xRot = Mathf.Round(transform.eulerAngles.x);
            bool isVertical = Mathf.Approximately(xRot, 90f) || Mathf.Approximately(xRot, 270f);

            if (isVertical && hasTouchedPointB)
            {
                TogglePath(pathObjectsB, originalPositionsB, ref hasTouchedPointB);
            }
        }
    }

    void TogglePath(List<GameObject> list, Dictionary<GameObject, Vector3> dict, ref bool hasTouched)
    {
        if (!hasTouched)
        {
            foreach (GameObject obj in list)
            {
                if (obj != null)
                {
                    obj.transform.position = dict[obj] + Vector3.down * 2f;
                    obj.SetActive(true);
                    obj.transform.DOMoveY(dict[obj].y, 0.4f).SetEase(Ease.OutBack);
                }
            }
        }
        else
        {
            foreach (GameObject obj in list)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        hasTouched = !hasTouched;
    }

    void CheckStandingOnPointB()
    {
        if (isRolling) return;

        float xRot = Mathf.Round(transform.eulerAngles.x);
        bool isVertical = Mathf.Approximately(xRot, 90f) || Mathf.Approximately(xRot, 270f);
        if (!isVertical) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, 0.2f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("PointB") && !hasTouchedPointB)
            {
                TogglePath(pathObjectsB, originalPositionsB, ref hasTouchedPointB);
                break;
            }
        }
    }

    void CheckStandingOnWin()
    {
        if (hasWon || isRolling) return;

        float xRot = Mathf.Round(transform.eulerAngles.x);
        bool isVertical = Mathf.Approximately(xRot, 90f) || Mathf.Approximately(xRot, 270f);
        if (!isVertical) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 1.2f))
        {
            if (LayerMask.LayerToName(hit.collider.gameObject.layer) == "Win")
            {
                boxCollider.isTrigger = true;
                StartCoroutine(ShowCanvasWinAfterDelay(0.2f));
            }
        }
    }

    IEnumerator ShowCanvasWinAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (canvasWin != null && !hasWon)
        {
            hasWon = true;
            GameManager.Instance?.OnLevelWin();
            canvasWin.SetActive(true);
        }
    }

    IEnumerator ShowCanvasLoseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (canvasLose != null && !hasWon)
        {
            hasWon = true;
            GameManager.Instance?.OnLevelLose();
            canvasLose.SetActive(true);
        }
    }

    Vector3 RoundVector3(Vector3 v)
    {
        return new Vector3(
            Mathf.Round(v.x),
            1f,
            Mathf.Round(v.z / 1.5f) * 1.5f
        );
    }

    void CheckStandingOnBlockX()
    {
        if (hasWon || isRolling) return;

        float xRot = Mathf.Round(transform.eulerAngles.x);
        bool isVertical = Mathf.Approximately(xRot, 90f) || Mathf.Approximately(xRot, 270f);
        if (!isVertical) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 1.2f))
        {
            if (hit.collider.CompareTag("BlockX"))
            {
                boxCollider.isTrigger = true;
                StartCoroutine(ShowCanvasLoseAfterDelay(0.2f));
            }
        }
    }
}
