using UnityEngine;

public class Rotator : MonoBehaviour
{
    public enum axis { X, Y, Z }
    public axis rotateAxis = axis.X;
    public float rotateSpeed;
    public bool inverse;

    void Update()
    {
        if(rotateAxis == axis.X)
        {
            if (!inverse)
            {
                transform.Rotate(rotateSpeed * Time.deltaTime, 0f, 0f);
            }
            else
            {
                transform.Rotate(-rotateSpeed * Time.deltaTime, 0f, 0f);
            }
        }
        else if (rotateAxis == axis.Y)
        {
            if (!inverse)
            {
                transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
            }
            else
            {
                transform.Rotate(0f, -rotateSpeed * Time.deltaTime, 0f);
            }
        }
        else if (rotateAxis == axis.Z)
        {
            if (!inverse)
            {
                transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
            }
            else
            {
                transform.Rotate(0f, 0f, -rotateSpeed * Time.deltaTime);
            }
        }
    }
}
