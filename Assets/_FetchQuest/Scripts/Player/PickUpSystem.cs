using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/*
 * Author: Grant Reed
 * Contributors: 
 * Summary: Allows dog to pickup items
 *
 * Description:
 * Updates
 * - N/A
 */

public class PickUpSystem : MonoBehaviourPun
{

    [SerializeField] private GameObject currentItem = null;
   
    [SerializeField] private Transform holdPos;
    [SerializeField] private Transform dropPos;
    [SerializeField] private Transform interactPos;
    [SerializeField] private Vector3 pickUpBox;
    [SerializeField] private LayerMask pickUpsLayer;
    private QuestItem questItem;
    private string EventObjectTag = "EventObj";

    private string interactableTag = "Interactable";
    private bool isNetworked;
    
    private float maxMass;
    private void Awake()
    {
        if (FindObjectOfType<NetworkManager>() == null)
        {
            isNetworked = false;
        }
        else
            isNetworked = true;
    }
    private void Start()
    {
        
        switch (gameObject.tag) 
        {
            case "big": maxMass = 15; break;
            case "medium": maxMass = 10; break;
            case "small": maxMass = 5; break;
            default: maxMass = 0; break;
        }
    }

    private void Update() 
    {
         if (Input.GetKeyDown(KeyCode.E))
         {
            if (currentItem != null)
            {
                if (isNetworked)
                    Drop();
                else
                    DropRPC();
            }
            else
            {
                Collider[] items = Physics.OverlapBox(interactPos.position, pickUpBox, interactPos.rotation, pickUpsLayer);
                if (items.Length > 0)
                {
                    foreach (Collider item in items)
                    {
                        if (item.gameObject.CompareTag(EventObjectTag))
                        {
                            item.gameObject.GetComponent<Interactable>().Interact(this.gameObject);
                            break;
                        }
                        if (item.attachedRigidbody.mass <= maxMass)
                        {
                            PickUp(item.gameObject);
                            
                            break;
                        }
                    }
                }
            }
         }
    }
    public GameObject GetItem()
    {
        return currentItem;
    }

    private void PickUp(GameObject item)
    {
        if(isNetworked)
            photonView.RPC("PickUpRPC", RpcTarget.All, item.GetComponent<PhotonView>().ViewID);
        else
        {
            AudioManager.Instance.PlaySFX(AudioNames.PickUp, transform.position);

            currentItem = item;
            Collider[] cols = currentItem.GetComponentsInChildren<Collider>();
            foreach (Collider col in cols)
            {
                col.enabled = false;
            }
            currentItem.GetComponent<Rigidbody>().isKinematic = true;
            currentItem.GetComponent<Outline>().enabled = false;
            currentItem.transform.parent = holdPos;
            currentItem.transform.position = holdPos.position;
            currentItem.transform.rotation = holdPos.rotation;
            questItem = currentItem.GetComponent<QuestItem>();
            if (questItem)
            {
                questItem.pickedUp();
            }
        }
    }
    private void Drop()
    {
        photonView.RPC("DropRPC", RpcTarget.All);
    }
    
    [PunRPC]
    private void PickUpRPC(int id)
    {
        AudioManager.Instance.PlaySFX(AudioNames.PickUp, transform.position);

        currentItem = PhotonView.Find(id).gameObject;
        Collider[] cols = currentItem.GetComponentsInChildren<Collider>();
        foreach (Collider col in cols)
        {
            col.enabled = false;
        }
        currentItem.GetComponent<Rigidbody>().isKinematic = true;
        currentItem.GetComponent<Outline>().enabled = false;
        currentItem.transform.parent = holdPos;
        currentItem.transform.position = holdPos.position;
        currentItem.transform.rotation = holdPos.rotation;
        questItem = currentItem.GetComponent<QuestItem>();
        if (questItem)
        {
            questItem.pickedUp();
        }
    }

    [PunRPC]
    private void DropRPC()
    {
        if (currentItem != null)
        {
            currentItem.transform.parent = null;
            currentItem.transform.position = dropPos.position;
            currentItem.transform.rotation = dropPos.rotation;
            Collider[] cols = currentItem.GetComponentsInChildren<Collider>();
            foreach (Collider col in cols)
            {
                col.enabled = true;
            }
            currentItem.GetComponent<Rigidbody>().isKinematic = false;

            if (questItem)
            {
                questItem.dropped();
            }
            questItem = null;
            currentItem = null;
        }
    }

}

