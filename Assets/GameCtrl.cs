using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyPhysx;

public class GameCtrl : MonoBehaviour
{
    static int frameCount = 0;

    public float speed;
    public Transform goTransform;

    private MyColliderCylinder playerColliderCylinder = new MyColliderCylinder();
    private Vector3 inputDir;
    
    private void Start()
    {
        playerColliderCylinder.name = "CylinderPlayer";
        playerColliderCylinder.radius = (goTransform.localScale.x / 2);
    }

    void FixedUpdate()
    {
        if (frameCount++ % 2 == -1)
            return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        inputDir = new Vector3(h, 0, v).normalized;
        
        if (inputDir == Vector3.zero && PhysxWorld.Instance().isSimulate == false)
            return;

        var moveOffset = inputDir * speed;
        playerColliderCylinder.pos += moveOffset;

        PhysxWorld.Instance().ColliderSimulation(playerColliderCylinder, moveOffset);
        
        goTransform.position = playerColliderCylinder.pos;

        PhysxWorld.Instance().isSimulate = false;
    }
}
