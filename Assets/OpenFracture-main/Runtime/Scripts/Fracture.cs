using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Rigidbody))]
public class Fracture : MonoBehaviour
{
    public FractureOptions fractureOptions; // 부서지기 옵션 
    public CallbackOptions callbackOptions; // 부서지기 완료되면 부를 콜백함수 옵션
    private GameObject fragmentRoot; // 복제된 큐브 부모

    // 디버깅용
    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.O))
            this.ComputeFracture();

    }

    /// <summary>
    /// Compute the fracture and create the fragments
    /// </summary>
    /// <returns></returns>
    private void ComputeFracture()
    {
        var mesh = this.GetComponent<MeshFilter>().sharedMesh;

        if (mesh != null)
        {
            // If the fragment root object has not yet been created, create it now
            if (this.fragmentRoot == null)
            {
                // Create a game object to contain the fragments
                this.fragmentRoot = new GameObject($"{this.name}Fragments"); // 부술 오브젝트를 복사해서 새로 생성함
                this.fragmentRoot.transform.SetParent(this.transform.parent);

                // Each fragment will handle its own scale
                this.fragmentRoot.transform.position = this.transform.position;
                this.fragmentRoot.transform.rotation = this.transform.rotation;
                this.fragmentRoot.transform.localScale = Vector3.one;
            }

            var fragmentTemplate = CreateFragmentTemplate();

             if (fractureOptions.asynchronous)
             {
                 StartCoroutine(Fragmenter.FractureAsync(
                     this.gameObject,
                     this.fractureOptions,
                     fragmentTemplate,
                     this.fragmentRoot.transform,
                     () =>
                     {

                         GameObject.Destroy(fragmentTemplate);
                         this.gameObject.SetActive(false);
                         
                         if (callbackOptions.onCompleted != null)
                         {
                             callbackOptions.onCompleted.Invoke();
                         }
                         
                     }
                 ));
             }
             else
             {
                 Fragmenter.Fracture(this.gameObject,
                                     this.fractureOptions,
                                     fragmentTemplate,
                                     this.fragmentRoot.transform);
            
                 // Done with template, destroy it
                 GameObject.Destroy(fragmentTemplate);
            
                 // Deactivate the original object
                 this.gameObject.SetActive(false);
            
                 // Fire the completion callback
     
                 if (callbackOptions.onCompleted != null)
                 {
                    callbackOptions.onCompleted.Invoke();
                 }
             }
        }
    }

    /// <summary>
    /// Creates a template object which each fragment will derive from
    /// </summary>
    /// <param name="preFracture">True if this object is being pre-fractured. This will freeze all of the fragments.</param>
    /// <returns></returns>
    private GameObject CreateFragmentTemplate()
    {
        // If pre-fracturing, make the fragments children of this object so they can easily be unfrozen later.
        // Otherwise, parent to this object's parent
        GameObject obj = new GameObject();
        obj.name = "Fragment";
        obj.tag = this.tag;
        obj.layer = this.gameObject.layer; // 레이어 따라가게
        
        // Update mesh to the new sliced mesh
        obj.AddComponent<MeshFilter>(); 
        
        obj.AddComponent<RandomAddForce>(); // 조각들이 실행할 스크립트
        
        //머터리얼 추가. 일반 머터리얼은 슬롯 1에, 잘린 단면 머터리얼은 슬롯 2에
        var meshRenderer = obj.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = new Material[2] {
            this.GetComponent<MeshRenderer>().sharedMaterial,
            this.fractureOptions.insideMaterial
        };

        // collider 프로퍼티를 조각들에게 복사
        var thisCollider = this.GetComponent<Collider>();
        var fragmentCollider = obj.AddComponent<MeshCollider>();
        fragmentCollider.convex = true;
        fragmentCollider.sharedMaterial = thisCollider.sharedMaterial;
        fragmentCollider.isTrigger = thisCollider.isTrigger;

        // rigidbody 프로퍼티를 조각들한테 복사
        var thisRigidBody = this.GetComponent<Rigidbody>();
        var fragmentRigidBody = obj.AddComponent<Rigidbody>();
        fragmentRigidBody.velocity = thisRigidBody.velocity;  
        fragmentRigidBody.angularVelocity = thisRigidBody.angularVelocity;
        fragmentRigidBody.drag = thisRigidBody.drag;
        fragmentRigidBody.angularDrag = thisRigidBody.angularDrag;
        fragmentRigidBody.useGravity = true; // 중력 On
        
        return obj;
    }
}