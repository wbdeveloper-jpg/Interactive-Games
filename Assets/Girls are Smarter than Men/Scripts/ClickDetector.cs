using UnityEngine;

public class ClickDetector : MonoBehaviour
{
    public GameObject particlePrefab; // assign in inspector
    public GameObject floatingTextPrefab;
    public Transform floatingTextParent;
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Detect(Input.mousePosition);
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Detect(Input.GetTouch(0).position);
        }
    }

    private void Detect(Vector2 screenPosition)
    {
        if (!EntryFlowController.isGameActive) return;


        Vector2 pos = Camera.main.ScreenToWorldPoint(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.zero);

        if (hit.collider == null) return;

        // 🎯 OBJECT CLICK
        if (hit.collider.CompareTag("object"))
        {
            // 🚫 No color selected
            if (ColorManager.Instance.SelectedColorData == null)
            {
                GirlsGameManager.instance.ShowMessage("Select a color!", Color.white);
                return;
            }

            ColorablePart part = hit.collider.GetComponent<ColorablePart>();

            if (part != null)
            {
                part.SetUserColor(ColorManager.Instance.SelectedColorData);
                TutorialController.Instance?.OnObjectColored();
            }

            // ✨ Spawn particle
            AudioManager.Instance.PlaySFX(0);
            SpawnEffect(pos);
        }
    }

    private void SpawnEffect(Vector2 position)
    {
        if (particlePrefab == null) return;

        GameObject fx = Instantiate(particlePrefab, position, Quaternion.identity);

        // 🎨 Match particle color
        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        //if (ps != null)
        //{
        //    var main = ps.main;
        //    main.startColor = ColorManager.Instance.SelectedColor;
        //}

        // 🧹 Auto destroy
        Destroy(fx, 2f);
    }
    void ShowMessage(string msg)
    {
        GameObject obj = Instantiate(floatingTextPrefab, floatingTextParent);

        //obj.transform.position = Camera.main.WorldToScreenPoint(worldPos);

        FloatingText ft = obj.GetComponent<FloatingText>();
        if (ft != null)
        {
            ft.Show(msg,Color.red);
        }
    }
}