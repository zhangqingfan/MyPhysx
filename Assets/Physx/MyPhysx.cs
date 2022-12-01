using System.Collections.Generic;
using UnityEngine;

//Data Structure
namespace MyPhysx
{
    public enum ColliderType
    {
        Box,
        Cylinder,
        ColliderTypeMax
    }

    public class MyColliderBase
    {
        public string name;
        public Vector3 pos;
        public bool isStop = false;
        //public ColliderType colliderType;

        public virtual bool DetectBoxCollision(MyColliderBox box, ref Vector3 adjustOffset) { return false; }
        public virtual bool DetectCylinderCollision(MyColliderCylinder cylinder, ref Vector3 adjustOffset) { return false; }
    }

    public class MyColliderBox: MyColliderBase
    {
        public Vector3 size;
        public Vector3[] rotation = new Vector3[3];
        
        /*
        public override void DetectBoxCollision(MyColliderBox box, out Vector3 adjustOffset)
        {
            adjustOffset = Vector3.zero;
        }

        public override void DetectCylinderCollision(MyColliderCylinder cylinder, out Vector3 adjustOffset)
        {
            adjustOffset = Vector3.zero;
        }
        */
    }

    public class MyColliderCylinder: MyColliderBase
    {
        public float radius;
        public override bool DetectBoxCollision(MyColliderBox box, ref Vector3 adjustOffset)
        {
            var offset = pos - box.pos;
            var projectionX = Vector3.Dot(offset, box.rotation[0]) * box.rotation[0];
            var projectionZ = Vector3.Dot(offset, box.rotation[2]) * box.rotation[2];

            projectionX = Vector3.ClampMagnitude(projectionX, box.size.x);
            projectionZ = Vector3.ClampMagnitude(projectionZ, box.size.z);

            var nearestVector3 = pos - (box.pos + projectionX + projectionZ);

            if (Vector3.SqrMagnitude(nearestVector3) >= radius * radius)
                return false;

            adjustOffset = nearestVector3.normalized * (radius - nearestVector3.magnitude);
            return true;
        }

        public override bool DetectCylinderCollision(MyColliderCylinder cylinder, ref Vector3 adjustVector3) 
        {
            var offset = pos - cylinder.pos;
            if (offset.sqrMagnitude >= (radius + cylinder.radius) * (radius + cylinder.radius))
                return false;

            var a = offset.normalized;
            var b = (radius + cylinder.radius - offset.magnitude);
            adjustVector3 = offset.normalized * (radius + cylinder.radius - offset.magnitude);
            //Debug.Log(a + " / " + b + " / " + adjustVector3);
            return true;
        }
    }
}

namespace MyPhysx
{
    public class CollisionInfo
    {
        public Vector3 adjustVector3;
    }

    //Physical Environment
    public class PhysxWorld
    {
        //Thread safe?
        static PhysxWorld instance = new PhysxWorld();
        public Dictionary<string, MyColliderBase> colliderDict = new Dictionary<string, MyColliderBase>();
        public bool isSimulate = false;

        public List<CollisionInfo> collisionInfoList = new List<CollisionInfo>();

        public void AddCollider(MyColliderBase colliderBase)
        {
            if (colliderDict.ContainsKey(colliderBase.name) == true)
                Debug.Log("Already exist: " + colliderBase.name);

            colliderDict[colliderBase.name] = colliderBase;
        }
        public static PhysxWorld Instance()
        {
            return instance;
        }

        public void ColliderSimulation(MyColliderBase player, Vector3 moveOffset)
        {
            collisionInfoList.Clear();
            var adjustVector3 = Vector3.zero;

            foreach (MyColliderBase colliderBase in colliderDict.Values)
            {
                if (colliderBase is MyColliderBox)
                {
                    if (player.DetectBoxCollision((MyColliderBox)colliderBase, ref adjustVector3) == false)
                        continue;

                    var info = new CollisionInfo();
                    info.adjustVector3 = adjustVector3;  
                    collisionInfoList.Add(info);
                }
                   
                if (colliderBase is MyColliderCylinder)
                {
                    if (player.DetectCylinderCollision((MyColliderCylinder)colliderBase, ref adjustVector3) == false)
                        continue;

                    var info = new CollisionInfo(); 
                    info.adjustVector3 = adjustVector3;
                    collisionInfoList.Add(info);
                }
            }

            if (collisionInfoList.Count == 0)
                return;

            //reset to original position
            player.pos -= moveOffset;

            var middleNormal = CalculateMiddleNormal(collisionInfoList);
            var maxAngle = CalculateMaxAngle(middleNormal, collisionInfoList);
            var angle = Vector3.Angle(-moveOffset, middleNormal); //important!!!
            var adjustVec = CalculateMiddleVector3(collisionInfoList);

            //Debug.Log("angle: " + angle + " / " + maxAngle);
            if (angle <= maxAngle)
            {
                if (player.isStop == false)
                    player.pos = player.pos + adjustVec + moveOffset;
                
                Debug.Log(adjustVec.ToString("f6") + "Can not go!");
                player.isStop = true;
                
                return;
            }

            player.isStop = false;
            Debug.Log("cAN GO" + moveOffset.ToString("f6") + " //" + adjustVec.ToString("f6"));
            player.pos = player.pos + adjustVec + moveOffset;
            return;

            if (angle > maxAngle)
            {
                Debug.Log("Angle says you can move!");
                var minAngleNormal = FindMinAngleVector3(-moveOffset, collisionInfoList);
                minAngleNormal = minAngleNormal.normalized;

                var projection = Vector3.Dot(-moveOffset, minAngleNormal) * -minAngleNormal; //important!!!
                ///moveOffset -= projection;
            }
        }

        Vector3 CalculateMiddleVector3(List<CollisionInfo> infoList)
        {
            Vector3 middleVector3 = Vector3.zero;

            if (infoList.Count == 0)
                return middleVector3;

            for (int i = 0; i < infoList.Count; i++)
                middleVector3 += infoList[i].adjustVector3;

            return middleVector3;// /= infoList.Count; 
        }

        Vector3 CalculateMiddleNormal(List<CollisionInfo> infoList)
        {
            Vector3 middleVector3 = Vector3.zero;

            if (infoList.Count == 0)
                return middleVector3;

            for (int i = 0; i < infoList.Count; i++)
                middleVector3 += infoList[i].adjustVector3.normalized;

            return middleVector3 /= infoList.Count;
        }

        float CalculateMaxAngle(Vector3 normal, List<CollisionInfo> infoList)
        {
            float maxAngle = 0f;

            for (int i = 0; i < infoList.Count; i++)
            {
                var angle = Vector3.Angle(normal, infoList[i].adjustVector3);
                maxAngle = maxAngle < angle ? angle : maxAngle;
            }

            return maxAngle; 
        }

        Vector3 FindMinAngleVector3(Vector3 normal, List<CollisionInfo> infoList)
        {
            float minxAngle = 360f;
            Vector3 minAngleVec = Vector3.zero;

            for (int i = 0; i < infoList.Count; i++)
            {
                var angle = Vector3.Angle(normal, infoList[i].adjustVector3);
                if (minxAngle < angle)
                    continue;

                minxAngle = angle;
                minAngleVec = infoList[i].adjustVector3;
            }

            return minAngleVec;
        }
    }
}


