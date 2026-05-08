using UnityEngine;
using UnityEngine.UI;

public class LoadingSpinner : MonoBehaviour
{
    public float rotateSpeed = 200f;

    void Update()
    {
        transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime);
    }
}