using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField] internal string ID;
    private void OnTriggerEnter(Collider other)
    {
        IInteractable interactable = other.GetComponentInChildren<IInteractable>();
        if(interactable != null)
        {
            interactable.Interact(this);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        IInteractable interactable = collision.collider.GetComponentInChildren<IInteractable>();
        if (interactable != null)
        {
            interactable.Interact(this);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        IExitable exitable = other.GetComponentInChildren<IExitable>();
        if (exitable != null)
        {
            exitable.Exit(this);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        IExitable exitable = collision.collider.GetComponentInChildren<IExitable>();
        if (exitable != null)
        {
            exitable.Exit(this);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        IStayable stayable = other.GetComponentInChildren<IStayable>();
        if (stayable != null)
        {
            stayable.Stay(this);
        }
    }

   
}
