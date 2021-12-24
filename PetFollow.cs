using UnityEngine;
using System;
using System.Collections;
using XMLEngine.GameFramework.Logic;
using XMLEngine.GameEngine.Logic;
using XMLEngine.Drawing;


public delegate void PetItemEventHandler(object sender, PetEventArgs args);

public class PetFollow : MonoBehaviour
{
    public Transform target;
    
    
    
    public float stopRange = 0.0f;    
    public float offsetX = 0.0f;
    public float offsetY = 0.0f;
    public float offsetZ = 0.0f;
    public float ActionRange = 0.4f;  
    public float WalkRange = 0.2f; 

    public Transform TargetShadow
    {
        get;
        set;
    }
    
    
    
    public float MoveSpeed
    {
        get
        {
            return moveSpeed;
        }

    }
    
    private Vector3 targetPosition = Vector3.zero;
    public PetItemEventHandler PetItemEvent = null;

    private PetEventArgs petEventArgs = null;
    
    private float moveSpeed = 0;
    private float moveDelta = 0;
    private float real_move_delta = 0;
    Transform myTransform; 
    void Awake()
    {
        myTransform = transform; 
        petEventArgs = new PetEventArgs();
        ischangepos = true;
    }
    bool ischangepos = false;
    void Update()
    {
        if (target == null)
        {
            return;
        }

        Vector3 _targetPosition = target.localPosition + target.forward * offsetX + target.up * offsetY + target.right * offsetZ;

        bool  isCanMove = SetCanArriveTarget(_targetPosition);

        if (isCanMove)
        {
            targetPosition = _targetPosition;
        }else
        {
            if  (targetPosition ==Vector3.zero)
                targetPosition = _targetPosition;
        }
           

        
        Vector3 myPosition = myTransform.localPosition;
        
        myPosition.y = 0;
        targetPosition.y = 0;
        float distance = Vector3.Distance(myPosition, targetPosition);
        if (distance > ActionRange)
        {
            
            SetMoveSpeed(10.0f, distance);
            
            myTransform.LookAt(target.position);
        }
        else if (distance > stopRange && distance < WalkRange && moveSpeed > 4.0f)
        {
            SetMoveSpeed(4.0f, distance);
        }
        else if(distance <= stopRange)
        {
            SetMoveSpeed(0, 1);
        }
        float real_speed = 0;
        if (moveDelta > 0)
        {
            myTransform.localRotation = Quaternion.Slerp(myTransform.localRotation, target.localRotation, 0.05f);
            
            targetPosition = Global.GetGroundPos(null, GSpriteTypes.Leader, targetPosition.x, targetPosition.z, Global.ConstGroundMaxHeight);

            targetPosition.y += offsetY;
            var next_pos = Vector3.Lerp(myTransform.localPosition, targetPosition, moveDelta);
            var delta = Vector3.Distance(next_pos, myTransform.localPosition);
            real_speed = delta / Time.deltaTime;
            
            
            
            
            
            myTransform.localPosition = next_pos;

            if (TargetShadow != null)
            {
                Vector3 pos = myTransform.position;
                pos.y = targetPosition.y + 0.02f - offsetY;
                TargetShadow.position = pos;
            }
            ischangepos = true;
        }
        else
        {
            if (ischangepos)
            {
                ischangepos = false;
            
            
            
            
            
                    targetPosition = Global.GetGroundPos(null, GSpriteTypes.Leader, targetPosition.x, targetPosition.z, Global.ConstGroundMaxHeight);
                    targetPosition.y += offsetY;
                    myTransform.localPosition = new Vector3(myTransform.localPosition.x, targetPosition.y, myTransform.localPosition.z);
                
                if (TargetShadow != null)
                {
                    Vector3 pos = myTransform.position;
                    pos.y = targetPosition.y + 0.02f - offsetY;
                    TargetShadow.position = pos;
                }
            }
        }

        
        
        

        
        
        
        

        
        if (real_speed > 4)
        {
            if (PetItemEvent != null)
            {
                petEventArgs.StepType = 1; 
                PetItemEvent(this, petEventArgs);
            }
        }
        else if (real_speed > 0 && real_speed <= 4)
        {
            if (PetItemEvent != null)
            {
                petEventArgs.StepType = 3; 
                PetItemEvent(this, petEventArgs);
            }
        }
        else if (real_speed <= 0)
        {
            if (PetItemEvent != null)
            {
                petEventArgs.StepType = 2;  
                PetItemEvent(this, petEventArgs);
            }
        }

    }


    bool SetCanArriveTarget( Vector3 pos )
    {
        Point center = new Point((int)(pos.x * 100), (int)(pos.z * 100));
        if (Global.OnObstruction(center, Global.CurrentMapData))
        {
            return false;
        }
        return true;
    }


    void SetMoveSpeed(float speed, float distance)
    {
        if (moveSpeed != speed)
        {
            
            moveSpeed = speed;
            moveDelta = Time.deltaTime * speed / distance;
        }
    }
    
    
    
    
    public void MoveByVector(Vector3 dir)
    {
        if (target == null) return;

        Vector3 targetPosition = target.localPosition + target.forward * dir.x + target.up * dir.y + target.right * dir.z;
        myTransform.localRotation = target.localRotation;
        
        targetPosition = Global.GetGroundPos(null, GSpriteTypes.Leader, targetPosition.x, targetPosition.z, Global.ConstGroundMaxHeight);
        myTransform.localPosition = targetPosition;
    }
}