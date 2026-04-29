
using Unity.Cinemachine;
using UnityEngine;

public class ParallaxManager : MonoBehaviour
{
   [System.Serializable]  // what does this do im not sure.. 
   public class ParallaxLayer
   {
       public Transform layer;
       public float parallaxFactor;  //range 0-1. 
   }

   public ParallaxLayer[] layers;
   public Transform camTransform;
   private Vector3 cameraLastPosition;
    
   void Start()
   { 
       cameraLastPosition = camTransform.position;
   }

   void OnEnable() //used for sobscribing
   {
       CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated); //asking cinemachine to call OnCameraUpdated wheenver it updates its cameras coordinates.
   }
   void OnDisable()
   {
       CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated); //removing is neccessary otherwise Cinemachine Brain will try to call OnCameraUpdated which doesnt exist->ERRORS
   }
   void OnCameraUpdated(CinemachineBrain brain)
   {
 
       Vector3 camDelta = camTransform.position - cameraLastPosition;

       foreach (ParallaxLayer layer in layers  )
       {
           float moveX = camDelta.x * layer.parallaxFactor;
           float moveY = camDelta.y * layer.parallaxFactor;

           layer.layer.position += new Vector3(moveX, moveY, 0);

       }

       cameraLastPosition = camTransform.position;
   }
}
