using UnityEngine;

public class BearCameraPositioner : MonoBehaviour
{
    [SerializeField] private Transform bearTransform;
    
    [Header("Adjust these small values for 8x Scale")]
    [SerializeField] private Vector3 localEyeOffset = new Vector3(0f, 0.35f, 0.4f); 

    private void LateUpdate()
    {
        if (bearTransform != null)
        {
            // Position using the Bear's local orientation directions
            transform.position = bearTransform.position 
                + (bearTransform.right * localEyeOffset.x) 
                + (bearTransform.up * localEyeOffset.y) 
                + (bearTransform.forward * localEyeOffset.z);

            // Match the Bear's Y rotation so you look where the bear looks
            transform.rotation = Quaternion.Euler(0f, bearTransform.eulerAngles.y, 0f);
        }
    }
}