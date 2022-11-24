using System.Collections;
using System.Collections.Generic;
using MyPhysx;
using UnityEngine;

public class UpdatePhysxWorld : MonoBehaviour
{
    public Vector3 rotation = Vector3.zero;
    public float scale = -1.0f;
    bool isLoad = false;

    void Start()
    {
        RegisterColliderArray<BoxCollider>();
        RegisterColliderArray<CapsuleCollider>();
    }

    private void OnValidate()
    {
        if(isLoad == false)
        {
            rotation = transform.rotation.eulerAngles;
            scale = transform.localScale.x;
            isLoad = true;
            return;
        }

        rotation.x = 0;
        rotation.z = 0;
        transform.rotation = Quaternion.Euler(rotation);

        scale = Mathf.Clamp(scale, 1.0f, 10.0f);
        transform.localScale = new Vector3(scale, scale, scale);

        RegisterColliderArray<BoxCollider>();
        RegisterColliderArray<CapsuleCollider>();

        PhysxWorld.Instance().isSimulate = true;
    }

    void RegisterColliderArray<T>()
    {
        MyColliderBase colliderBase;

        var array = GetComponentsInChildren<T>();
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].GetType() == typeof(BoxCollider))
            {
                colliderBase = new MyColliderBox();

                var box = colliderBase as MyColliderBox;
                box.name = gameObject.name;
                box.pos = transform.position;
                //box.colliderType = ColliderType.Box;

                box.size = transform.localScale / 2;
                box.rotation[0] = transform.right;
                box.rotation[1] = transform.up;
                box.rotation[2] = transform.forward;

                PhysxWorld.Instance().AddCollider(box);
            }

            if (array[i].GetType() == typeof(CapsuleCollider))
            {
                colliderBase = new MyColliderCylinder();

                var cylinder = colliderBase as MyColliderCylinder;
                cylinder.name = gameObject.name;
                cylinder.pos = transform.position;
                //cylinder.colliderType = ColliderType.Cylinder;
                
                cylinder.radius = transform.localScale.x / 2;

                PhysxWorld.Instance().AddCollider(cylinder);
            }
        }
    }
}
