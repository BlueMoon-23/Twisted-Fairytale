using UnityEngine;

public class SelfRotating : MonoBehaviour
{
    [SerializeField] private Vector3 axis = Vector3.forward; // trục xoay (Z cho 2D/UI)
    [SerializeField] private float speed = 90f;              // độ/giây, số âm để xoay ngược
    [SerializeField] private Space space = Space.Self;       // xoay theo trục local hay world

    void Update()
    {
        transform.Rotate(axis.normalized * speed * Time.deltaTime, space);
    }
}