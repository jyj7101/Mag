using System;
using UnityEngine;

namespace Chess
{
    public class ChessCube : ChessTileMap
    {
        [SerializeField] private MeshRenderer render;
        [SerializeField] private Vector2 pos;

        public Vector2 Pos
        {
            get => pos;
            set
            {
                if (value.x < 0 || value.y < 0 || value.x > 8 || value.y > 8)
                {
                    Debug.Assert(false, "out of map range!");
                }
                else
                    pos = value;
            }
        }
        
        private void Start()
        {

            
        }


        public void SetPos(int x, int z)
        {
            pos = new Vector2(x, z);
            transform.position = new Vector3(pos.x, 1, pos.y);
        }

        public void SetMaterial(Material mat)
        {
            render.material = Instantiate(mat);
        }


    }
}
