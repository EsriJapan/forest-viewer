using UnityEngine;
using TMPro;

public class ChangeTMP : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tmp;

    public void ChangeTMPforGameObject()
    {
        tmp.text = transform.parent.name;
    }
}
