using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCube : MonoBehaviour
{
    public string ClientID;
    private GameObject nw;
    public float xx, yy, zz;
    // Start is called before the first frame update
    void Start()
    {
        nw = GameObject.Find("NetworkMan");
    }

    public void DestroyCube()
    {
        Destroy(gameObject);
        Debug.Log("CUBE DESTROYED**********************");
    }
    // Update is called once per frame
    void Update()
    {
        xx = this.transform.position.x;
        yy = this.transform.position.y;
        zz = this.transform.position.z;
        if(nw.GetComponent<NetworkMan>().myID == this.ClientID)
        {

        nw.GetComponent<NetworkMan>().mX = xx;
        nw.GetComponent<NetworkMan>().mY = yy;
        nw.GetComponent<NetworkMan>().mZ = zz;

            if (Input.GetKey(KeyCode.UpArrow))
            {
                transform.Translate(Vector3.forward / 36);
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                transform.Translate(-Vector3.forward / 36);
            }

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                transform.Translate(Vector3.left / 36);
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                transform.Translate(-Vector3.left / 36);
            }






        }
        

        
    }
}

