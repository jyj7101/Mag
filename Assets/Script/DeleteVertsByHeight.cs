using System;
using UnityEngine;
 using System.Collections;
 using System.Collections.Generic;
 
 public class DeleteVertsByHeight : MonoBehaviour {
 
     public GameObject heightReferenceObject;
     public float heightReferenceFloat = 0;
     float heightCutOff;
     public float errorAdjustment=0;

     private Mesh mesh;
     private int[] triangles;
     private Vector3[] vertices;
     private Vector2[] uv;
     private Vector3[] normals;
     private List<Vector3> vertList;
     private List<Vector2> uvList;
     private List<Vector3> normalsList;
     private List<int> trianglesList;

     void Start() {
         mesh = GetComponent<MeshFilter>().mesh;
         triangles = mesh.triangles;
         vertices = mesh.vertices;
         uv = mesh.uv;
         normals = mesh.normals;
         vertList = new List<Vector3>();
         uvList = new List<Vector2>();
         normalsList = new List<Vector3>();
         trianglesList = new List<int>();
         if (heightReferenceObject != null) 
         {
             heightCutOff = heightReferenceObject.transform.position.y;
         }
         else
         {
             heightCutOff = heightReferenceFloat;
         }
 
         int i = 0;
         while (i < vertices.Length) {
             vertList.Add (vertices[i]); 
             uvList.Add (uv[i]);
             normalsList.Add (normals[i]);
             i++;
         }
         for (int triCount = 0; triCount < triangles.Length; triCount += 3) 
         {
             if ((transform.TransformPoint(vertices[triangles[triCount  ]]).y < heightCutOff+errorAdjustment)  &&
                 (transform.TransformPoint(vertices[triangles[triCount+1]]).y < heightCutOff+errorAdjustment)  &&
                 (transform.TransformPoint(vertices[triangles[triCount+2]]).y < heightCutOff+errorAdjustment)) 
             {
                 
                 trianglesList.Add (triangles[triCount]);
                 trianglesList.Add (triangles[triCount+1]);
                 trianglesList.Add (triangles[triCount+2]);
             }
         }
 
 
         triangles = trianglesList.ToArray ();
         vertices = vertList.ToArray ();
         uv = uvList.ToArray ();
         normals = normalsList.ToArray ();
         //mesh.Clear();
         mesh.triangles = triangles;
         mesh.vertices = vertices;
         mesh.uv = uv;
         mesh.normals = normals;
     }

 }