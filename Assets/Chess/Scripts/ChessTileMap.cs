using Unity.Mathematics;
using UnityEngine;
using Utils;

namespace Chess
{
    public class ChessTileMap : MonoBehaviour
    {
        [SerializeField] private GameObject cube;
        private readonly int mapSize = 8; // map size 
        public int MapSize
        {
            get { return mapSize; }
        }
        
        [SerializeField] private Transform parents;

        [SerializeField] private Material black;
        [SerializeField] private Material white;
        [SerializeField] private Material selectedMat;

    
        private void Start()
        {

            ProduceTileMap();

        }

        public void ProduceTileMap()
        {
            bool isWhite = true;
            ChessCube cub = cube.GetComponent<ChessCube>();
            
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    if (isWhite)
                    {   
                        cub.SetMaterial(white);
                        isWhite = false;
                    }
                    else
                    {
                        cub.SetMaterial(black);
                        isWhite = true;
                    }
                    cub.SetPos(j, i);
                    Instantiate(cube, parents);
                }

                if (isWhite)
                    isWhite = false;
                else
                {
                    isWhite = true;
                }
            }
        }
        

        public void SelectedTile()
        {
            

        }
    



//     size = 1 
//     private readonly Vector3 _size = Vector3.one;
//     
//
// #if UNITY_EDITOR
//
//     private void OnDrawGizmos()
//     {
//         
//         for (int i = 0; i < mapSize; i++)
//         {
//             Vector3 pos = Vector3.zero;
//             pos.x += i;
//             Gizmos.DrawCube(pos, _size);
//
//             for (int j = 0; j < mapSize; j++)
//             {
//                 pos.z = 0;
//                 pos.z += j;
//                 Gizmos.DrawCube(pos, _size);
//             }
//         }
//     }
//     
// #endif

    }
}
